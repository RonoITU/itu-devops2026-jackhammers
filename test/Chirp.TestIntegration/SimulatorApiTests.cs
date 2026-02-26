namespace Chirp.TestIntegration;

public class SimulatorApiTests : AbstractIntegration
{
    [Fact]
    public async Task CheckThisTestSetupWorks()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            await dbContext.Database.EnsureDeletedAsync(); // Clear previous data
            await dbContext.Database.EnsureCreatedAsync(); // Recreate the database
        }

        
        var response = await _client.GetAsync("/cheeps");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    
        var cheeps = await response.Content.ReadFromJsonAsync<List<Core.DTOs.CheepDTO>>();
        cheeps.Should().NotBeNull();
    
        // Check whether a cheep with author was returned
        cheeps.Should().BeEmpty();
    }
}
