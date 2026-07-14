using EqualFutures.Web.Data;

namespace EqualFutures.Web.Services.Logging;

/// <summary>
/// Captures Warning-and-above log entries into the database so admins can review
/// application health and registration issues without server/console access.
/// </summary>
public class DatabaseLoggerProvider(IServiceScopeFactory scopeFactory) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new DatabaseLogger(categoryName, scopeFactory);

    public void Dispose() { }
}

public class DatabaseLogger(string categoryName, IServiceScopeFactory scopeFactory) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) =>
        logLevel >= LogLevel.Warning &&
        // Avoid feedback loops from EF Core's own logging while this logger writes to the database.
        !categoryName.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        string message = formatter(state, exception);

        // Never let logging block the caller or crash the app if the write fails.
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.AppLogEntries.Add(new AppLogEntry
                {
                    TimestampUtc = DateTime.UtcNow,
                    Level = logLevel.ToString(),
                    Category = categoryName,
                    Message = message,
                    Exception = exception?.ToString()
                });
                await db.SaveChangesAsync();
            }
            catch
            {
                // Swallow — logging must never be a source of failure itself.
            }
        });
    }
}
