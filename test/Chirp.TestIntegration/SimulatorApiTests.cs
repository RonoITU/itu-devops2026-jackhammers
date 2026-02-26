using Chirp.Core.DTOs;
using Chirp.Core.Interfaces;

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
        var content = await response.Content.ReadFromJsonAsync<LatestResponse>();
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
        var content = await response.Content.ReadFromJsonAsync<LatestResponse>();
        content.Should().NotBeNull();
        content.Latest.Should().Be(testLatest);
    }

    [Theory]
    [InlineData("/api/latest")]
    [InlineData("/api/msgs?latest=1")]
    [InlineData("/api/register?latest=1")]
    [InlineData("/api/msgs/rono?latest=1")]
    [InlineData("/api/fllws/rono?latest=1")]
    public async Task AllGetEndpoints_CatchServerSideExceptions(string requestUri)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            await dbContext.Database.EnsureDeletedAsync();
            // Left in this state will raise an exception for most calls.
        }

        var response = await _client.GetAsync(requestUri);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content.Should().NotBeNull();
        content.Status.Should().Be(500);
        content.ErrorMsg.Should().Be("Internal server error");
    }

    [Fact]
    public async Task Register_CorrectRequest()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            await dbContext.Database.EnsureDeletedAsync(); // Clear previous data
            await dbContext.Database.EnsureCreatedAsync(); // Recreate the database
        }

        var response = await _client.PostAsJsonAsync(
            "/api/register?latest=1", 
            new RegisterRequest
            {
                Username = "Rono ITU",
                Email = "rono@example.com",
                Pwd = "2a3b&4Cd",
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var authorService = scope.ServiceProvider.GetRequiredService<IAuthorService>();
            var newAuthor = await authorService.FindAuthorByName("Rono ITU");
            newAuthor.Should().NotBeNull();
            newAuthor.Name.Should().Be("Rono ITU");
            newAuthor.Email.Should().Be("rono@example.com");
        }
    }

    [Fact]
    public async Task Register_UserAlreadyExists()
    {
        // Easy way to prepare for test.
        await Register_CorrectRequest();

        var response = await _client.PostAsJsonAsync(
            "/api/register?latest=2", 
            new RegisterRequest
            {
                Username = "Rono ITU",
                Email = "rono2@example.com",
                Pwd = "2a3b&4Cd",
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Existing user should still be found.
        using (var scope = _factory.Services.CreateScope())
        {
            var authorService = scope.ServiceProvider.GetRequiredService<IAuthorService>();
            var newAuthor = await authorService.FindAuthorByName("Rono ITU");
            newAuthor.Should().NotBeNull();
            newAuthor.Name.Should().Be("Rono ITU");
            newAuthor.Email.Should().Be("rono@example.com");
        }
    }

    [Theory]
    [InlineData("", "rono@example.com", "2a3b&4Cd")]
    [InlineData("Rono ITU", "", "2a3b&4Cd")]
    [InlineData("Rono ITU", "rono@example.com", "")]
    public async Task Register_BadRequest_EmptyFields(string userName, string email, string pwd)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            await dbContext.Database.EnsureDeletedAsync(); // Clear previous data
            await dbContext.Database.EnsureCreatedAsync(); // Recreate the database
        }

        var response = await _client.PostAsJsonAsync(
            "/api/register?latest=1", 
            new RegisterRequest
            {
                Username = userName,
                Email = email,
                Pwd = pwd,
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_CatchServerSideExceptions()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            await dbContext.Database.EnsureDeletedAsync();
            // Left in this state will raise an exception for most calls.
        }

        var response = await _client.PostAsJsonAsync(
            "/api/register?latest=1", 
            new RegisterRequest
            {
                Username = "Rono ITU",
                Email = "rono@example.com",
                Pwd = "2a3b&4Cd",
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content.Should().NotBeNull();
        content.Status.Should().Be(500);
        content.ErrorMsg.Should().Be("Internal server error");
    }

    [Theory]
    [InlineData("This better actually work.", 2)]
    public async Task PostMessage_CorrectRequest(string msg, int latest)
    {
        await Register_CorrectRequest();

        var response = await _client.PostAsJsonAsync(
            $"/api/msgs/Rono%20ITU?latest={latest}", 
            new MessageRequest
            {
                Content = msg,
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var cheepService = scope.ServiceProvider.GetRequiredService<ICheepService>();
            var cheeps = await cheepService.GetCheepsFromAuthor("Rono ITU", 1);
            cheeps.Should().NotBeEmpty();
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("I am a message of more than 160 characters! Reeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee!")]
    public async Task PostMessage_BadRequest_MessageContent(string msg)
    {
        await Register_CorrectRequest();

        var response = await _client.PostAsJsonAsync(
            $"/api/msgs/Rono%20ITU?latest=2", 
            new MessageRequest
            {
                Content = msg,
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostMessage_BadRequest_AuthorNotFound()
    {
        await Register_CorrectRequest();

        var response = await _client.PostAsJsonAsync(
            $"/api/msgs/Rono%20IT?latest=2", 
            new MessageRequest
            {
                Content = "I am an okay message.",
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostMessage_CatchServerSideExceptions()
    {
        await Register_CorrectRequest();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            await dbContext.Database.EnsureDeletedAsync();
            // Left in this state will raise an exception for most calls.
        }

        var response = await _client.PostAsJsonAsync(
            $"/api/msgs/Rono%20ITU?latest=2", 
            new MessageRequest
            {
                Content = "I am an okay message.",
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content.Should().NotBeNull();
        content.Status.Should().Be(500);
        content.ErrorMsg.Should().Be("Internal server error");
    }

    [Fact]
    public async Task GetPublicMessages_EmptyDatabase()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            await dbContext.Database.EnsureDeletedAsync(); // Clear previous data
            await dbContext.Database.EnsureCreatedAsync(); // Recreate the database
        }

        var response = await _client.GetAsync("/api/msgs?latest=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<MessageResponse>>();
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPublicMessages_NormalRequest()
    {
        await Register_CorrectRequest();

        for (int i = 1; i <= 10; i++)
        {
            await _client.PostAsJsonAsync(
                $"/api/msgs/Rono%20ITU?latest={i+1}", 
                new MessageRequest
                {
                    Content = $"I am msg no {i}."
                }
            );
        }
        
        var response = await _client.GetAsync("/api/msgs?latest=12");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<MessageResponse>>();
        content.Should().NotBeNull();
        for (int i = 0, j = 10; i < 10; i++, j--)
        {
            content[i].Content.Should().Be($"I am msg no {j}.");
        }
    }

    [Fact]
    public async Task GetUserMessages_EmptyDatabase()
    {
        await Register_CorrectRequest();

        var response = await _client.GetAsync("/api/msgs/Rono%20ITU?latest=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<MessageResponse>>();
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserMessages_NoneMatch()
    {
        await Register_CorrectRequest();

        var response = await _client.GetAsync("/api/msgs/Rono%20ITU2?latest=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<MessageResponse>>();
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserMessages_SomeMatch()
    {
        await GetPublicMessages_NormalRequest();

        await _client.PostAsJsonAsync(
            "/api/register?latest=2", 
            new RegisterRequest
            {
                Username = "Rono ITU2",
                Email = "rono2@example.com",
                Pwd = "2a3b&4Cd",
            }
        );

        for (int i = 1; i <= 5; i++)
        {
            await _client.PostAsJsonAsync(
                $"/api/msgs/Rono%20ITU2?latest={i+2}", 
                new MessageRequest
                {
                    Content = $"I am msg no {i}."
                }
            );
        }

        var response = await _client.GetAsync("/api/msgs/Rono%20ITU?latest=8");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<MessageResponse>>();
        content.Should().NotBeNull();
        content.Count.Should().Be(10);
    }
}
