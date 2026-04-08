using System;

namespace DiscordMultiTool
{
    public static class Secrets
    {
        public static string GoogleClientId => Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? "YOUR_GOOGLE_CLIENT_ID";
        public static string GoogleClientSecret => Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? "YOUR_GOOGLE_CLIENT_SECRET";
        public static string FirebaseApiKey => Environment.GetEnvironmentVariable("FIREBASE_API_KEY") ?? "YOUR_FIREBASE_API_KEY";
    }
}
