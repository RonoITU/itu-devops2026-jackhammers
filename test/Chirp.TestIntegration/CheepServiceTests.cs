using Chirp.Core.Interfaces;
using Chirp.Infrastructure.Services;

namespace Chirp.TestIntegration;

public class CheepServiceTests : IClassFixture<IntegrationFixture>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CheepServiceTests(IntegrationFixture fixture)
    {
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

    [Fact]
    public async Task GetCheeps_NormalCalls()
    {
        // Arrange
        await PrepareDatabase();

        using var scope = _factory.Services.CreateScope();
        var cheepService = scope.ServiceProvider.GetRequiredService<ICheepService>();
    
        // Get first page.
        var response1 = await cheepService.GetCheeps(1);
        Assert.Equal(32, response1.Count);

        // Get last page.
        var response2 = await cheepService.GetCheeps(19);
        Assert.Equal(26, response2.Count);

        // Get non-existent page.
        var response3 = await cheepService.GetCheeps(20);
        Assert.Empty(response3);

        // TODO: Consider how page 0 and -1 are to be handled.
    }

    [Fact]
    public async Task DeleteCheep_NormalCall()
    {
        // Arrange
        await PrepareDatabase();

        using var scope = _factory.Services.CreateScope();
        var cheepService = scope.ServiceProvider.GetRequiredService<ICheepService>();

        var allCheeps = await cheepService.RetrieveAllCheepsFromAnAuthor("Tony Stark");
        var someCheep = allCheeps[5];

        // Act
        await cheepService.DeleteCheep(someCheep.CheepId);

        // Assert
        var updatedCheeps = await cheepService.RetrieveAllCheepsFromAnAuthor("Tony Stark");
        Assert.DoesNotContain(someCheep, updatedCheeps);
    }
}
