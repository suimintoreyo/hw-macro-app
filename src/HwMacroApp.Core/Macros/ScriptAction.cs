using System.Diagnostics;

namespace HwMacroApp.Core.Macros;

public sealed class ScriptAction : IMacroAction
{
    public required string ScriptPath { get; init; }
    public string Shell { get; init; } = "powershell.exe";
    public string Arguments { get; init; } = "-ExecutionPolicy Bypass -File";

    public string DisplayName => $"Script: {Path.GetFileName(ScriptPath)}";

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = Shell,
            Arguments = $"{Arguments} \"{ScriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
        });

        if (process != null)
            await process.WaitForExitAsync(ct);
    }
}
