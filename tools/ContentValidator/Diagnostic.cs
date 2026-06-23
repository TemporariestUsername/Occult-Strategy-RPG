namespace PaleCommunion.Tools.ContentValidator;

public enum Severity
{
    Error,
    Warning,
}

/// <summary>A single validation finding, tied to a file and (where known) an event card id.</summary>
public sealed record Diagnostic(Severity Severity, string File, string CardId, string Message)
{
    public override string ToString()
    {
        string sev = Severity == Severity.Error ? "ERROR" : "WARN ";
        string where = string.IsNullOrEmpty(CardId) ? File : $"{File} [{CardId}]";
        return $"{sev}  {where}: {Message}";
    }
}
