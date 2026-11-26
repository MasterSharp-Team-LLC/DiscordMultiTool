namespace DiscordMultiTool.Models;

public sealed record ProcessInfoItem(int Pid, string Name, double MemoryMb, string WindowTitle)
{
    public string SubtitleText => string.IsNullOrWhiteSpace(WindowTitle)
        ? "Desktop Window"
        : WindowTitle;

    public string PidText => $"PID {Pid}";
    public string MemoryText => $"{MemoryMb:F1} MB";

    public string DisplayText => string.IsNullOrWhiteSpace(WindowTitle)
        ? $"{Name}  (PID: {Pid} • {MemoryMb:F1} MB)"
        : $"{Name} — {WindowTitle}  (PID: {Pid} • {MemoryMb:F1} MB)";

    public override string ToString() => DisplayText;
}
