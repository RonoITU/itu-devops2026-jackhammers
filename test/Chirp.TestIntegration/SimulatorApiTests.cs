namespace Chirp.TestIntegration;

public class SimulatorApiTests : AbstractIntegration
{
    [Fact]
    public async Task GetLatest_DefaultValue()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            await dbContext.Database.EnsureDeletedAsync(); // Clear previous data
            await dbContext.Database.EnsureCreatedAsync(); // Recreate the database
        }

        var response = await _client.GetAsync("/api/latest");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<Core.DTOs.LatestResponse>();
        content.Should().NotBeNull();
        content.Latest.Should().Be(-1);
    }

    [Fact]
    public async Task GetLatest_WithPreviousLatest()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            await dbContext.Database.EnsureDeletedAsync(); // Clear previous data
            await dbContext.Database.EnsureCreatedAsync(); // Recreate the database
        }

        int testLatest = new Random().Next();

        await _client.GetAsync($"/api/msgs?latest={testLatest}");
        var response = await _client.GetAsync("/api/latest");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<Core.DTOs.LatestResponse>();
        content.Should().NotBeNull();
        content.Latest.Should().Be(testLatest);
    }

    [Theory]
    [InlineData("/api/latest")]
    [InlineData("/api/msgs?latest=1")]
    [InlineData("/api/register?latest=1")]
    [InlineData("/api/msgs/rono?latest=1")]
    [InlineData("/api/fllws/rono?latest=1")]
    public async Task GetEndpoints_CatchServerSideExceptions(string requestUri)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            await dbContext.Database.EnsureDeletedAsync();
            // Left in this state will raise an exception for most calls.
        }

        var response = await _client.GetAsync(requestUri);
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadFromJsonAsync<Core.DTOs.ErrorResponse>();
        content.Should().NotBeNull();
        content.Status.Should().Be(500);
        content.ErrorMsg.Should().Be("Internal server error");
    }
}
