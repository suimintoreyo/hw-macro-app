namespace HwMacroApp.Core.Macros;

public sealed class DelayAction : IMacroAction
{
    public required int Milliseconds { get; init; }

    public string DisplayName => $"Delay {Milliseconds}ms";

    public Task ExecuteAsync(CancellationToken ct = default) =>
        Task.Delay(Milliseconds, ct);
}
