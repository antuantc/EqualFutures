namespace EqualFutures.Web.Data;

/// <summary>
/// A captured application log entry, surfaced in the admin portal for diagnostics.
/// Only ever holds operational/log data — never a user's financial plan data.
/// </summary>
public class AppLogEntry
{
    public int Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
}
