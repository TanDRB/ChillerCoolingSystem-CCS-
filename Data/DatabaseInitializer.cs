using Microsoft.EntityFrameworkCore;

namespace ChillerCoolingSystem_CCS_.Data
{
    public static class DatabaseInitializer
    {
        private const int MaxAttempts = 10;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

        // Chạy trước khi các worker khởi động, tránh tranh chấp khi DB mới đang được tạo.
        public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            var dbFactory = services.GetRequiredService<IDbContextFactory<HistoryDbContext>>();
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
                    await db.Database.MigrateAsync(cancellationToken);
                    return;
                }
                catch (Exception ex) when (attempt < MaxAttempts && ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Khởi tạo DB lần {Attempt}/{Max} thất bại, thử lại sau {Sec}s", attempt, MaxAttempts, RetryDelay.TotalSeconds);
                    await Task.Delay(RetryDelay, cancellationToken);
                }
            }
        }
    }
}
