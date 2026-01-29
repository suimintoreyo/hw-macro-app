using System.Diagnostics;

namespace HwMacroApp.Core.Macros;

public sealed class LaunchAppAction : IMacroAction
{
    public required string Path { get; init; }
    public string Arguments { get; init; } = "";

    public string DisplayName => System.IO.Path.GetFileName(Path);

    public Task ExecuteAsync(CancellationToken ct = default)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Path,
            Arguments = Arguments,
            UseShellExecute = true,
        });
        return Task.CompletedTask;
    }
}
