using Chirp.Core.Interfaces;

namespace Chirp.Web;

public class MetricsUpdater : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public MetricsUpdater(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var authorService = scope.ServiceProvider.GetRequiredService<IAuthorService>();

            var count = await authorService.TotalAuthorCount();
            MetricsRegistry.TotalUsers.Set(count);

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}