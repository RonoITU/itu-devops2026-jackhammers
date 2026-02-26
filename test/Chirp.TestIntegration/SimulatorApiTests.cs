using Chirp.Core.DTOs;
using Chirp.Core.Interfaces;

namespace Chirp.TestIntegration;

public class SimulatorApiTests : IClassFixture<IntegrationFixture>
{
    private HttpClient _client = null!;
    private WebApplicationFactory<Program> _factory = null!;

    public SimulatorApiTests(IntegrationFixture fixture)
    {
        _client = fixture._client;
        _factory = fixture._factory;
    }

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
    public async Task Register_NormalRequest()
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
        await Register_NormalRequest();

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
    public async Task PostMessage_NormalRequest(string msg, int latest)
    {
        await Register_NormalRequest();

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
        await Register_NormalRequest();

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
        await Register_NormalRequest();

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
        await Register_NormalRequest();

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
        await Register_NormalRequest();

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
        content.Should().HaveCount(10);
        for (int i = 0, j = 10; i < 10; i++, j--)
        {
            content[i].Content.Should().Be($"I am msg no {j}.");
        }
    }

    [Fact]
    public async Task GetPublicMessages_NormalRequest_LimitedNo()
    {
        await GetPublicMessages_NormalRequest();

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
        
        var response = await _client.GetAsync("/api/msgs?latest=12&no=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<MessageResponse>>();
        content.Should().NotBeNull();
        content.Should().HaveCount(5);
        for (int i = 0, j = 10; i < 5; i++, j--)
        {
            content[i].Content.Should().Be($"I am msg no {j}.");
        }
    }

    [Fact]
    public async Task GetUserMessages_EmptyDatabase()
    {
        await Register_NormalRequest();

        var response = await _client.GetAsync("/api/msgs/Rono%20ITU?latest=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<MessageResponse>>();
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserMessages_NoneMatch()
    {
        await Register_NormalRequest();

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

    [Fact]
    public async Task GetUserMessages_SomeMatch_LimitedNo()
    {
        await GetUserMessages_SomeMatch();

        var response = await _client.GetAsync("/api/msgs/Rono%20ITU?latest=9&no=7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<MessageResponse>>();
        content.Should().NotBeNull();
        content.Count.Should().Be(7);
    }

    [Fact]
    public async Task FollowUser_NormalRequest_Follow()
    {
        await Register_NormalRequest();

        await _client.PostAsJsonAsync(
            "/api/register?latest=2", 
            new RegisterRequest
            {
                Username = "Rono ITU2",
                Email = "rono2@example.com",
                Pwd = "2a3b&4Cd",
            }
        );

        var response = await _client.PostAsJsonAsync(
            "/api/fllws/Rono%20ITU?latest=3",
            new FollowRequest
            {
                Follow = "Rono ITU2"
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task FollowUser_NormalRequest_Unfollow()
    {
        await FollowUser_NormalRequest_Follow();

        await _client.PostAsJsonAsync(
            "/api/register?latest=4", 
            new RegisterRequest
            {
                Username = "Rono ITU2",
                Email = "rono2@example.com",
                Pwd = "2a3b&4Cd",
            }
        );

        var response = await _client.PostAsJsonAsync(
            "/api/fllws/Rono%20ITU?latest=5",
            new FollowRequest
            {
                Unfollow = "Rono ITU2"
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task FollowUser_NotFound_Follower()
    {
        await Register_NormalRequest();

        await _client.PostAsJsonAsync(
            "/api/register?latest=2", 
            new RegisterRequest
            {
                Username = "Rono ITU2",
                Email = "rono2@example.com",
                Pwd = "2a3b&4Cd",
            }
        );

        var response = await _client.PostAsJsonAsync(
            "/api/fllws/Rono%20ITU3?latest=3",
            new FollowRequest
            {
                Follow = "Rono ITU2"
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FollowUser_NotFound_Followee()
    {
        await Register_NormalRequest();

        await _client.PostAsJsonAsync(
            "/api/register?latest=2", 
            new RegisterRequest
            {
                Username = "Rono ITU2",
                Email = "rono2@example.com",
                Pwd = "2a3b&4Cd",
            }
        );

        var response = await _client.PostAsJsonAsync(
            "/api/fllws/Rono%20ITU?latest=3",
            new FollowRequest
            {
                Follow = "Rono ITU3"
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FollowUser_CatchServerSideExceptions()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            await dbContext.Database.EnsureDeletedAsync();
            // Left in this state will raise an exception for most calls.
        }

        var response = await _client.PostAsJsonAsync(
            "/api/fllws/Rono%20ITU?latest=1",
            new FollowRequest
            {
                Follow = "Rono ITU2"
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content.Should().NotBeNull();
        content.Status.Should().Be(500);
        content.ErrorMsg.Should().Be("Internal server error");
    }

    [Fact]
    public async Task GetFollows_NormalRequest_OneFollower()
    {
        await FollowUser_NormalRequest_Follow();

        var response = await _client.GetAsync("/api/fllws/Rono%20ITU?latest=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<FollowsResponse>();
        content.Should().NotBeNull();
        content.Follows.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetFollows_NormalRequest_NoFollowers()
    {
        await FollowUser_NormalRequest_Follow();

        var response = await _client.GetAsync("/api/fllws/Rono%20ITU2?latest=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<FollowsResponse>();
        content.Should().NotBeNull();
        content.Follows.Should().BeEmpty();
    }

    [Fact(Skip = "Test Driving Development. To be implemented.")]
    public async Task GetFollows_AuthorNotFound()
    {
        await FollowUser_NormalRequest_Follow();

        var response = await _client.GetAsync("/api/fllws/Rono%20ITU3?latest=10");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Skip = "Test Driving Development. To be implemented.")]
    public async Task GetFollows_NormalRequest_LimitedNo()
    {
        await GetFollows_NormalRequest_OneFollower();

        await _client.PostAsJsonAsync(
            "/api/register?latest=11", 
            new RegisterRequest
            {
                Username = "Rono ITU3",
                Email = "rono3@example.com",
                Pwd = "2a3b&4Cd",
            }
        );

        await _client.PostAsJsonAsync(
            "/api/fllws/Rono%20ITU?latest=12",
            new FollowRequest
            {
                Follow = "Rono ITU3"
            }
        );

        var response = await _client.GetAsync("/api/fllws/Rono%20ITU?latest=13&no=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<FollowsResponse>();
        content.Should().NotBeNull();
        content.Follows.Should().HaveCount(1);
    }
}
