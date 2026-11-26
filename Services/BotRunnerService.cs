using System;
using System.Diagnostics;
using System.IO;

namespace DiscordMultiTool.Services;

public sealed class BotRunnerService : IDisposable
{
    private readonly string _botName;
    private readonly string _tokenEnvVar;
    private Process? _process;
    private readonly object _sync = new();

    public event Action<string>? LogReceived;
    public event Action<bool, int?>? StateChanged;

    public bool IsRunning
    {
        get
        {
            lock (_sync)
            {
                return _process != null && !_process.HasExited;
            }
        }
    }

    public int? ProcessId
    {
        get
        {
            lock (_sync)
            {
                return _process != null && !_process.HasExited ? _process.Id : null;
            }
        }
    }

    public BotRunnerService(string botName, string tokenEnvVar)
    {
        _botName = botName;
        _tokenEnvVar = tokenEnvVar;
    }

    public int Start(string scriptPath, string token, string pythonExecutable = "python")
    {
        if (string.IsNullOrWhiteSpace(scriptPath))
            throw new ArgumentException("Script path is required.", nameof(scriptPath));

        scriptPath = scriptPath.Trim().Trim('"');
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException($"Script file not found: {scriptPath}", scriptPath);

        string pyExe = string.IsNullOrWhiteSpace(pythonExecutable) ? "python" : pythonExecutable.Trim();

        lock (_sync)
        {
            if (_process != null && !_process.HasExited)
                throw new InvalidOperationException($"{_botName} is already running.");

            var psi = new ProcessStartInfo
            {
                FileName = pyExe,
                Arguments = $"-u \"{scriptPath}\"",
                WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? Environment.CurrentDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            psi.Environment["PYTHONUNBUFFERED"] = "1";
            if (!string.IsNullOrWhiteSpace(token))
            {
                psi.Environment[_tokenEnvVar] = token.Trim();
                psi.Environment["BOT_TOKEN"] = token.Trim();
            }

            var proc = new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = true
            };

            proc.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    LogReceived?.Invoke($"[{DateTime.Now:HH:mm:ss}] {e.Data}");
            };

            proc.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    LogReceived?.Invoke($"[{DateTime.Now:HH:mm:ss}] [ERR] {e.Data}");
            };

            proc.Exited += (_, _) =>
            {
                int exitCode = -1;
                try { exitCode = proc.ExitCode; } catch { }

                lock (_sync)
                {
                    if (ReferenceEquals(_process, proc))
                    {
                        _process = null;
                    }
                }

                proc.Dispose();
                LogReceived?.Invoke($"[{DateTime.Now:HH:mm:ss}] Process exited with code {exitCode}.");
                StateChanged?.Invoke(false, null);
            };

            if (!proc.Start())
            {
                proc.Dispose();
                throw new InvalidOperationException($"Failed to start {pyExe}.");
            }

            _process = proc;
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            int pid = proc.Id;
            LogReceived?.Invoke($"[{DateTime.Now:HH:mm:ss}] Started {_botName} (PID: {pid}) -> {scriptPath}");
            StateChanged?.Invoke(true, pid);
            return pid;
        }
    }

    public void Stop()
    {
        Process? proc;
        lock (_sync)
        {
            proc = _process;
            _process = null;
        }

        if (proc == null)
            return;

        try
        {
            if (!proc.HasExited)
            {
                proc.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            LogReceived?.Invoke($"[{DateTime.Now:HH:mm:ss}] [ERR] Stop failed: {ex.Message}");
        }
        finally
        {
            proc.Dispose();
            StateChanged?.Invoke(false, null);
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
