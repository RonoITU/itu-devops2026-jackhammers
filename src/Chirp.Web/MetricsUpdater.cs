using Chirp.Core.Interfaces;

namespace Chirp.Web;

public class MetricsUpdater : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MetricsUpdater> _logger;

    public MetricsUpdater(IServiceScopeFactory scopeFactory, ILogger<MetricsUpdater> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    private HashSet<string> _previousAuthors = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var authorService = scope.ServiceProvider.GetRequiredService<IAuthorService>();
                var cheepService = scope.ServiceProvider.GetRequiredService<ICheepService>();

                MetricsRegistry.TotalUsers.Set(await authorService.TotalAuthorCount());

                MetricsRegistry.TotalCheepsPosted.Set(await cheepService.TotalCheepsPosted());

                MetricsRegistry.ActiveUsers.Set(await authorService.GetActiveUsers());

                var (average, median) = await authorService.GetFollowerStats();
                MetricsRegistry.AverageFollowers.Set(average);
                MetricsRegistry.MedianFollowers.Set(median);

                var mostFollowed = await authorService.GetMostFollowed();
                var currentAuthors = mostFollowed.Select(x => x.Author).ToHashSet();
                foreach (var oldAuthor in _previousAuthors.Except(currentAuthors))
                {
                    MetricsRegistry.MostFollowed.RemoveLabelled(oldAuthor);
                }
                foreach (var (author, followers) in mostFollowed)
                {
                    MetricsRegistry.MostFollowed
                        .WithLabels(author)
                        .Set(followers);
                }
                _previousAuthors = currentAuthors;

                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error while updating metrics. Will retry after a 10 second delay.");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (OperationCanceledException) {} // Ignore cancellation during this delay. 
            }
        }
    }
}
