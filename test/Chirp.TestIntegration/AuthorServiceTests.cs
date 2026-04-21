using Chirp.Core.Interfaces;
using Chirp.Infrastructure.Services;

namespace Chirp.TestIntegration;

public class AuthorServiceTests : IClassFixture<IntegrationFixture>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthorServiceTests(IntegrationFixture fixture)
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
    public async Task GetMostFollowed_NormalRequest()
    {
        await PrepareDatabase();

        using var scope = _factory.Services.CreateScope();
        var authorService = scope.ServiceProvider.GetRequiredService<IAuthorService>();

        var response = await authorService.GetMostFollowed();
        Assert.Equal(2, response.Count);
        Assert.Equal("James Bond", response[0].Author);
        Assert.Equal(2, response[0].Followers);
        Assert.Equal("Jack Sparrow", response[1].Author);
        Assert.Equal(1, response[1].Followers);
    }

    [Fact]
    public async Task GetFollowerStats_NormalRequest()
    {
        await PrepareDatabase();

        using var scope = _factory.Services.CreateScope();
        var authorService = scope.ServiceProvider.GetRequiredService<IAuthorService>();

        var response = await authorService.GetFollowerStats();
        Assert.Equal(1.5, response.Average);
        Assert.Equal(1.5, response.Median);
    }
}