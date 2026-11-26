using DiscordMultiTool.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordMultiTool.Services;

public static class ProcessService
{
    public static Task<bool> IsProcessRunningAsync(string processName)
    {
        return Task.Run(() =>
        {
            var procs = Process.GetProcessesByName(processName);
            bool running = procs.Length > 0;
            foreach (var p in procs)
            {
                p.Dispose();
            }
            return running;
        });
    }

    public static Task<List<ProcessInfoItem>> GetWindowedProcessesAsync()
    {
        return Task.Run(() =>
        {
            var result = new List<ProcessInfoItem>();
            var all = Process.GetProcesses();
            foreach (var p in all)
            {
                try
                {
                    if (p.MainWindowHandle != IntPtr.Zero)
                    {
                        double memMb = p.WorkingSet64 / (1024.0 * 1024.0);
                        string title = p.MainWindowTitle ?? "";
                        result.Add(new ProcessInfoItem(p.Id, p.ProcessName, memMb, title));
                    }
                }
                catch
                {
                    // Process exited or access denied
                }
                finally
                {
                    p.Dispose();
                }
            }

            return result
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(p => p.Pid)
                .ToList();
        });
    }

    public static string KillProcess(int pid)
    {
        using var process = Process.GetProcessById(pid);
        string name = process.ProcessName;
        process.Kill();
        return name;
    }
}
