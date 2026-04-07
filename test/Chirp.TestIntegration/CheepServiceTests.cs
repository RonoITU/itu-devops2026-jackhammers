using Chirp.Core.Interfaces;
using Chirp.Infrastructure.Services;

namespace Chirp.TestIntegration;

public class CheepServiceTests : IClassFixture<IntegrationFixture>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public CheepServiceTests(IntegrationFixture fixture)
    {
        _client = fixture._client;
        _factory = fixture._factory;
    }

    private async Task PrepareDatabase()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            await dbContext.Database.EnsureDeletedAsync(); // Clear previous data
            await dbContext.Database.EnsureCreatedAsync(); // Recreate the database
            DbInitializer.SeedDatabase(dbContext);
        }
    }

    [Fact]
    public async Task GetTotalPageNumber_NormalCalls()
    {
        await PrepareDatabase();

        using var scope = _factory.Services.CreateScope();

        var cheepService = scope.ServiceProvider.GetRequiredService<ICheepService>();
        
        // Filter for an author.
        var response1 = await cheepService.GetTotalPageNumber("Tony Stark");
        Assert.Equal(2, response1);

        // Total pages. 
        var response2 = await cheepService.GetTotalPageNumber("");
        Assert.Equal(19, response2);
    }
}
