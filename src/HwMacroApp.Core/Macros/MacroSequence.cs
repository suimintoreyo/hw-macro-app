namespace HwMacroApp.Core.Macros;

/// <summary>複数の IMacroAction を順番に実行する。</summary>
public sealed class MacroSequence : IMacroAction
{
    public required IReadOnlyList<IMacroAction> Actions { get; init; }

    public string DisplayName =>
        string.Join(" → ", Actions.Select(a => a.DisplayName));

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        foreach (var action in Actions)
        {
            ct.ThrowIfCancellationRequested();
            await action.ExecuteAsync(ct);
        }
    }
}
