using Firebase.Auth;
using Firebase.Auth.Providers;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordMultiTool.Services;

public sealed record AuthResult(bool Success, string? Email, string? ErrorMessage);

public sealed class FirebaseAuthService
{
    private const string AuthDomain = "discordmultitool.firebaseapp.com";
    private static readonly string[] DatabaseBaseUrls =
    {
        "https://discordmultitool-default-rtdb.europe-west1.firebasedatabase.app",
        "https://discordmultitool-default-rtdb.firebaseio.com"
    };

    private string? _idToken;
    private string? _userId;
    private string? _email;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_idToken) && !string.IsNullOrEmpty(_userId);
    public string? Email => _email;

    private static bool HasFirebaseKey()
    {
        string key = Secrets.FirebaseApiKey;
        return !string.IsNullOrWhiteSpace(key) && key != "YOUR_FIREBASE_API_KEY";
    }

    private FirebaseAuthClient CreateClient()
    {
        var config = new FirebaseAuthConfig
        {
            ApiKey = Secrets.FirebaseApiKey,
            AuthDomain = AuthDomain,
            Providers = new FirebaseAuthProvider[]
            {
                new EmailProvider()
            }
        };
        return new FirebaseAuthClient(config);
    }

    public async Task<AuthResult> SignInWithEmailAsync(string email, string password)
    {
        if (!HasFirebaseKey())
            return new AuthResult(false, null, "Firebase API Key not configured. Add FirebaseApiKey in secrets.json.");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return new AuthResult(false, null, "Email and password are required.");

        try
        {
            var client = CreateClient();
            var credential = await client.SignInWithEmailAndPasswordAsync(email.Trim(), password);
            _idToken = await credential.User.GetIdTokenAsync();
            _userId = credential.User.Uid;
            _email = credential.User.Info.Email;
            return new AuthResult(true, _email, null);
        }
        catch (Exception ex)
        {
            return new AuthResult(false, null, ex.Message);
        }
    }

    public async Task<AuthResult> RegisterWithEmailAsync(string email, string password)
    {
        if (!HasFirebaseKey())
            return new AuthResult(false, null, "Firebase API Key not configured. Add FirebaseApiKey in secrets.json.");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return new AuthResult(false, null, "Email and password are required.");

        try
        {
            var client = CreateClient();
            var credential = await client.CreateUserWithEmailAndPasswordAsync(email.Trim(), password);
            _idToken = await credential.User.GetIdTokenAsync();
            _userId = credential.User.Uid;
            _email = credential.User.Info.Email;
            return new AuthResult(true, _email, null);
        }
        catch (Exception ex)
        {
            return new AuthResult(false, null, ex.Message);
        }
    }

    public async Task<AuthResult> SignInWithGoogleAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!HasFirebaseKey())
                return new AuthResult(false, null, "Firebase API Key not configured. Add FirebaseApiKey in secrets.json.");

            string clientId = Secrets.GoogleClientId;
            string clientSecret = Secrets.GoogleClientSecret;

            if (string.IsNullOrWhiteSpace(clientId) ||
                string.IsNullOrWhiteSpace(clientSecret) ||
                clientId == "YOUR_GOOGLE_CLIENT_ID" ||
                clientSecret == "YOUR_GOOGLE_CLIENT_SECRET")
            {
                return new AuthResult(false, null,
                    "Google Client ID or Secret not configured. Set GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET in secrets.json or environment variables.");
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMinutes(2));

            // NullDataStore avoids reusing an expired cached IdToken from %AppData%\Google.Apis.Auth
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
                new[] { "email", "profile", "openid" },
                "user",
                timeoutCts.Token,
                new NullDataStore());

            if (string.IsNullOrEmpty(credential.Token?.IdToken))
                return new AuthResult(false, null, "Google did not return an ID token.");

            var requestPayload = new
            {
                postBody = $"id_token={Uri.EscapeDataString(credential.Token.IdToken)}&providerId=google.com",
                requestUri = "http://localhost",
                returnIdpCredential = true,
                returnSecureToken = true
            };

            string jsonPayload = JsonSerializer.Serialize(requestPayload);
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var response = await HttpClientProvider.Shared.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={Secrets.FirebaseApiKey}",
                content,
                timeoutCts.Token);

            string responseString = await response.Content.ReadAsStringAsync(timeoutCts.Token);

            if (response.IsSuccessStatusCode)
            {
                using var jsonDoc = JsonDocument.Parse(responseString);
                var root = jsonDoc.RootElement;
                _email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : "Google User";
                _idToken = root.GetProperty("idToken").GetString();
                _userId = root.GetProperty("localId").GetString();
                return new AuthResult(true, _email, null);
            }

            string firebaseError = ExtractFirebaseError(responseString);
            return new AuthResult(false, null, $"Firebase authentication failed: {firebaseError}");
        }
        catch (OperationCanceledException)
        {
            return new AuthResult(false, null, "Google login cancelled.");
        }
        catch (Exception ex)
        {
            return new AuthResult(false, null, ex.Message);
        }
    }

    public void SignOut()
    {
        _idToken = null;
        _userId = null;
        _email = null;
    }

    public async Task<AppConfig?> LoadCloudConfigAsync()
    {
        if (!IsAuthenticated) return null;

        foreach (string baseUrl in DatabaseBaseUrls)
        {
            try
            {
                string dbUrl = $"{baseUrl}/users/{_userId}/config.json?auth={_idToken}";
                using var response = await HttpClientProvider.Shared.GetAsync(dbUrl);
                if (!response.IsSuccessStatusCode)
                    continue;

                string json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json) || json == "null")
                    return null;

                return JsonSerializer.Deserialize<AppConfig>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading cloud config from {baseUrl}: {ex.Message}");
            }
        }

        return null;
    }

    public async Task<bool> SaveCloudConfigAsync(AppConfig config)
    {
        if (!IsAuthenticated) return false;

        string json = JsonSerializer.Serialize(config);

        foreach (string baseUrl in DatabaseBaseUrls)
        {
            try
            {
                string dbUrl = $"{baseUrl}/users/{_userId}/config.json?auth={_idToken}";
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await HttpClientProvider.Shared.PutAsync(dbUrl, content);
                if (response.IsSuccessStatusCode)
                    return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving cloud config to {baseUrl}: {ex.Message}");
            }
        }

        return false;
    }

    private static string ExtractFirebaseError(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var err) &&
                err.TryGetProperty("message", out var msg))
            {
                return msg.GetString() ?? "Unknown error";
            }
        }
        catch { }
        return "Unknown error";
    }
}
