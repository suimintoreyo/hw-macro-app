namespace HwMacroApp.Core.Macros;

public interface IMacroAction
{
    string DisplayName { get; }
    Task ExecuteAsync(CancellationToken ct = default);
}
