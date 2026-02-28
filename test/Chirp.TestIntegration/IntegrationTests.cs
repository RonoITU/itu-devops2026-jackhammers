namespace Chirp.TestIntegration;

public class IntegrationTests : IClassFixture<IntegrationFixture>
{
    private HttpClient _client = null!;
    private WebApplicationFactory<Program> _factory = null!;

    public IntegrationTests(IntegrationFixture fixture)
    {
        _client = fixture._client;
        _factory = fixture._factory;
    }

    [Fact]
    public async Task GetCheepsReturnsCheeps()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            await dbContext.Database.EnsureDeletedAsync(); // Clear previous data
            await dbContext.Database.EnsureCreatedAsync(); // Recreate the database

            var author = new Author() { AuthorId = 1, Cheeps = new List<Cheep>(), Email = "mymail", Name = "testPerson", AuthorsFollowed = new List<string>()};
        
            var cheep = new Cheep
            {
                CheepId = 1,
                Author = author,
                AuthorId = author.AuthorId,
                Text = "Hello! I hope this goes through",
                TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1728497189).UtcDateTime
            };

            dbContext.Authors.Add(author);
            await dbContext.SaveChangesAsync();

            dbContext.Cheeps.Add(cheep);
            await dbContext.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/cheeps");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cheeps = await response.Content.ReadFromJsonAsync<List<Core.DTOs.CheepDTO>>();
        cheeps.Should().NotBeNull();
        cheeps.Should().ContainSingle(c => c.Text == "Hello! I hope this goes through");
    }
    
    [Fact]
    public async Task GetCheepsReturnsCheepsForSpecificAuthor()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            await dbContext.Database.EnsureDeletedAsync(); // Clear previous data
            await dbContext.Database.EnsureCreatedAsync(); // Recreate the database

            var author = new Author() { AuthorId = 1, Cheeps = new List<Cheep>(), Email = "mymail", Name = "testPerson", AuthorsFollowed = new List<string>() };
    
            var cheep = new Cheep
            {
                CheepId = 1,
                Author = author,
                AuthorId = author.AuthorId,
                Text = "Test Message",
                TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1728497189).UtcDateTime
            };

            dbContext.Authors.Add(author);
            await dbContext.SaveChangesAsync();

            dbContext.Cheeps.Add(cheep);
            await dbContext.SaveChangesAsync();
        }

        // Fetch all cheeps and see if author is amongst them
        
        var response = await _client.GetAsync("/cheeps");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    
        var cheeps = await response.Content.ReadFromJsonAsync<List<Core.DTOs.CheepDTO>>();
        cheeps.Should().NotBeNull();
    
        // Check whether a cheep with author was returned
        cheeps.Should().ContainSingle(c =>  c.Author.Name == "testPerson");
    }
    
    [Fact]
    public async Task CheckGetCheepsWhenEmpty()
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

    [Fact]
    public async Task CheckGetFollowsWhenEmpty()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            await dbContext.Database.EnsureDeletedAsync(); // Clear previous data
            await dbContext.Database.EnsureCreatedAsync(); // Recreate the database
        }
        
        var author = new Author() { AuthorId = 1, Cheeps = new List<Cheep>(), Email = "mymail", Name = "testPerson", AuthorsFollowed = new List<string>() };
        
        var response = await _client.GetAsync("/" + author.Name +"/follows");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    
        var followedAuthors = await response.Content.ReadFromJsonAsync<List<string>>();
        followedAuthors.Should().NotBeNull();
    
        // Check whether a cheep with author was returned
        followedAuthors.Should().BeEmpty();
    }
    
    [Fact]
    public async Task CheckGetFollowsWhenFollowing()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            await dbContext.Database.EnsureDeletedAsync(); // Clear previous data
            await dbContext.Database.EnsureCreatedAsync(); // Recreate the database

            var author1 = new Author() { AuthorId = 1, Cheeps = new List<Cheep>(), Email = "mymail", Name = "testPerson1", AuthorsFollowed = new List<string>() };
            var author2 = new Author() { AuthorId = 2, Cheeps = new List<Cheep>(), Email = "my2mail", Name = "testPerson2", AuthorsFollowed = new List<string>() };

            dbContext.Authors.Add(author1);
            dbContext.Authors.Add(author2);
            await dbContext.SaveChangesAsync();

            // Use the repository to follow the author
            var authorRepository = new AuthorRepository(dbContext);
            await authorRepository.FollowAuthor(author1.Name, author2.Name);
        }

        var response = await _client.GetAsync("/testPerson1/follows");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var followedAuthors = await response.Content.ReadFromJsonAsync<List<string>>();
        followedAuthors.Should().NotBeNull();

        // Check whether author2 is in the list of followed authors
        followedAuthors.Should().Contain("testPerson2");
    }
    
    [Fact]
    public async Task CheckGetFollowsAfterUnfollowing()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
            await dbContext.Database.EnsureDeletedAsync(); // Clear previous data
            await dbContext.Database.EnsureCreatedAsync(); // Recreate the database

            var author1 = new Author() { AuthorId = 1, Cheeps = new List<Cheep>(), Email = "mymail", Name = "testPerson1", AuthorsFollowed = new List<string>() };
            var author2 = new Author() { AuthorId = 2, Cheeps = new List<Cheep>(), Email = "my2mail", Name = "testPerson2", AuthorsFollowed = new List<string>() };

            dbContext.Authors.Add(author1);
            dbContext.Authors.Add(author2);
            await dbContext.SaveChangesAsync();

            // Use the repository to follow the author
            var authorRepository = new AuthorRepository(dbContext);
            await authorRepository.FollowAuthor(author1.Name, author2.Name);
            await authorRepository.UnfollowAuthor(author1.Name, author2.Name);
        }

        var response = await _client.GetAsync("/testPerson1/follows");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var followedAuthors = await response.Content.ReadFromJsonAsync<List<string>>();
        followedAuthors.Should().NotBeNull();

        // Check if the list is empty after unfollowing
        followedAuthors.Should().BeEmpty();
    }
}