using Chirp.Core.DTOs;
using Chirp.Web.Pages;
using Xunit.Abstractions;

namespace Chirp.TestUnit;

public class UnitTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("Helge", "Hello, BDSA students!", 1690892208)]
    //[InlineData("Adrian", "Hej, velkommen til kurset.", 1690895308)]
    public async Task TestReadForAuthor (string authorName, string messageData, long unixTimestamp) 
    {
        // Arrange
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();                              
        var builder = new DbContextOptionsBuilder<CheepDBContext>().UseSqlite(connection);

        await using var context = new CheepDBContext(builder.Options);   
        await context.Database.EnsureCreatedAsync();               
        
        // Seed the database with a test entry
        var author = new Author() { AuthorId = 1, Cheeps = new List<Cheep>(), Email = "mymail", Name = authorName, AuthorsFollowed = new List<string>()};
        
        var cheep = new Cheep
        {
            CheepId = 1,
            Author = author,
            AuthorId = author.AuthorId,
            Text = messageData,
            TimeStamp = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime // Ensure this matches the format in your model
        };

        context.Authors.Add(author);
        context.Cheeps.Add(cheep);
        await context.SaveChangesAsync();  // Save the seed data to the in-memory database
        
        ICheepRepository repository = new CheepRepository(context, new AuthorRepository(context));
        
        // Act
        var cheepList = await repository.ReadCheepsFromAuthor(authorName, 1);
        var firstCheep = cheepList.First();
     
        
        // Assert
        Assert.Equal(firstCheep.Author.Name, authorName);
        Assert.Equal(firstCheep.Text, messageData);
        Assert.Equal(firstCheep.FormattedTimeStamp, UnixTimeStampToDateTimeString(unixTimestamp));
    }
    
     [Fact]
     public async Task TestReadallcheeps()
     {
         // Arrange
         await using var connection = new SqliteConnection("Filename=:memory:");
         await connection.OpenAsync();                              
         var builder = new DbContextOptionsBuilder<CheepDBContext>().UseSqlite(connection);

         await using var context = new CheepDBContext(builder.Options);   
         await context.Database.EnsureCreatedAsync();               
        
         // Seed the database with a test entry
         var author1 = new Author() { AuthorId = 1, Cheeps = new List<Cheep>(), Email = "helge@hotmail", Name = "Helge", AuthorsFollowed = new List<string>()};
         var author2 = new Author() { AuthorId = 2, Cheeps = new List<Cheep>(), Email = "", Name = "Adrian", AuthorsFollowed = new List<string>()};
        
         var cheep1 = new Cheep
         {
             CheepId = 1,
             Author = author1,
             AuthorId = author1.AuthorId,
             Text = "Sådan!",
             TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime // Ensure this matches the format in your model
         };
         var cheep2 = new Cheep
         {
             CheepId = 2,
             Author = author2,
             AuthorId = author2.AuthorId,
             Text = "Dependency Injections are very important.",
             TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1690992208).UtcDateTime // Ensure this matches the format in your model
         };
         var cheep3 = new Cheep
         {
             CheepId = 3,
             Author = author1,
             AuthorId = author1.AuthorId,
             Text = "I like my cold brewed tee very much.",
             TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1691992208).UtcDateTime // Ensure this matches the format in your model
         };
         var cheep4 = new Cheep
         {
             CheepId = 4,
             Author = author2,
             AuthorId = author2.AuthorId,
             Text = "EF Core is goated!!.",
             TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1692992208).UtcDateTime // Ensure this matches the format in your model
         };
         
         context.Authors.Add(author1);
         context.Authors.Add(author2);
         context.Cheeps.Add(cheep1);
         context.Cheeps.Add(cheep2);
         context.Cheeps.Add(cheep3);
         context.Cheeps.Add(cheep4);
         await context.SaveChangesAsync();  // Save the seed data to the in-memory database

        
         ICheepRepository repository = new CheepRepository(context, new AuthorRepository(context));
        
         // Act
         var cheepList = await repository.ReadAllCheeps(0);
         
     
        
         // Assert
         Assert.True(cheepList.Count() == 4);
     }

     [Fact]
     public async Task DeleteCheepsByAuthor()
     {
         // Arrange
         await using var connection = new SqliteConnection("Filename=:memory:");
         await connection.OpenAsync();                              
         var builder = new DbContextOptionsBuilder<CheepDBContext>().UseSqlite(connection);

         await using var context = new CheepDBContext(builder.Options);   
         await context.Database.EnsureCreatedAsync();               
        
         // Seed the database with a test entry
         var author1 = new Author() { AuthorId = 1, Cheeps = new List<Cheep>(), Email = "helge@hotmail", Name = "Helge", AuthorsFollowed = new List<string>()};
         var author2 = new Author() { AuthorId = 2, Cheeps = new List<Cheep>(), Email = "", Name = "Adrian", AuthorsFollowed = new List<string>()};
         
         var cheep1 = new Cheep
         {
             CheepId = 1,
             Author = author1,
             AuthorId = author1.AuthorId,
             Text = "Sådan!",
             TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime // Ensure this matches the format in your model
         };
         var cheep2 = new Cheep
         {
             CheepId = 2,
             Author = author2,
             AuthorId = author2.AuthorId,
             Text = "Dependency Injections are very important.",
             TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1690992208).UtcDateTime // Ensure this matches the format in your model
         };
         var cheep3 = new Cheep
         {
             CheepId = 3,
             Author = author1,
             AuthorId = author1.AuthorId,
             Text = "I like my cold brewed tee very much.",
             TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1691992208).UtcDateTime // Ensure this matches the format in your model
         };
         var cheep4 = new Cheep
         {
             CheepId = 4,
             Author = author2,
             AuthorId = author2.AuthorId,
             Text = "EF Core is goated!!.",
             TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1692992208).UtcDateTime // Ensure this matches the format in your model
         };
         
         context.Authors.Add(author1);
         context.Authors.Add(author2);
         context.Cheeps.Add(cheep1);
         context.Cheeps.Add(cheep2);
         context.Cheeps.Add(cheep3);
         context.Cheeps.Add(cheep4);
         await context.SaveChangesAsync();  // Save the seed data to the in-memory database

        
         ICheepRepository repository = new CheepRepository(context, new AuthorRepository(context));
         
            // Act
            await repository.DeleteUserCheeps(new AuthorDTO { Name = "Helge", Email = "helge@hotmail", AuthorsFollowed = new List<string>()});
            await repository.DeleteUserCheeps(new AuthorDTO { Name = "Adrian", Email = "", AuthorsFollowed = new List<string>() });
            
            var cheepList = await repository.ReadAllCheeps(0);
         
     
        
            // Assert
            Assert.True(!cheepList.Any());
     }

     [Fact]
     public async Task DeleteCheepsById()
     {
         // Arrange
         await using var connection = new SqliteConnection("Filename=:memory:");
         await connection.OpenAsync();
         var builder = new DbContextOptionsBuilder<CheepDBContext>().UseSqlite(connection);

         await using var context = new CheepDBContext(builder.Options);
         await context.Database.EnsureCreatedAsync();

         // Seed the database with a test entry
         var author1 = new Author() { AuthorId = 1, Cheeps = new List<Cheep>(), Email = "helge@hotmail", Name = "Helge", AuthorsFollowed = new List<string>()};
         
         var cheep1 = new Cheep
         {
             CheepId = 1,
             Author = author1,
             AuthorId = author1.AuthorId,
             Text = "Sådan!",
             TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1690892208)
                 .UtcDateTime // Ensure this matches the format in your model
         };
         context.Authors.Add(author1);
         context.Cheeps.Add(cheep1);
         ICheepRepository repository = new CheepRepository(context, new AuthorRepository(context));
         
         await repository.DeleteCheep(1);
         var cheepList = await repository.ReadAllCheeps(0);
         
         
         // Assert
         Assert.True(!cheepList.Any());
     }
     
     
     [Fact]
     public async Task DeleteCheepsByAuthorWith2Authors()
     {
         // Arrange
         await using var connection = new SqliteConnection("Filename=:memory:");
         await connection.OpenAsync();                              
         var builder = new DbContextOptionsBuilder<CheepDBContext>().UseSqlite(connection);

         await using var context = new CheepDBContext(builder.Options);   
         await context.Database.EnsureCreatedAsync();               
        
         // Seed the database with a test entry
         var author1 = new Author() { AuthorId = 1, Cheeps = new List<Cheep>(), Email = "helge@hotmail", Name = "Helge", AuthorsFollowed = new List<string>() };
         var author2 = new Author() { AuthorId = 2, Cheeps = new List<Cheep>(), Email = "", Name = "Adrian", AuthorsFollowed = new List<string>() };
         
         var cheep1 = new Cheep
         {
             CheepId = 1,
             Author = author1,
             AuthorId = author1.AuthorId,
             Text = "Sådan!",
             TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime // Ensure this matches the format in your model
         };
         var cheep2 = new Cheep
         {
             CheepId = 2,
             Author = author2,
             AuthorId = author2.AuthorId,
             Text = "Dependency Injections are very important.",
             TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1690992208).UtcDateTime // Ensure this matches the format in your model
         };
         var cheep3 = new Cheep
         {
             CheepId = 3,
             Author = author1,
             AuthorId = author1.AuthorId,
             Text = "I like my cold brewed tee very much.",
             TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1691992208).UtcDateTime // Ensure this matches the format in your model
         };
         var cheep4 = new Cheep
         {
             CheepId = 4,
             Author = author2,
             AuthorId = author2.AuthorId,
             Text = "EF Core is goated!!.",
             TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1692992208).UtcDateTime // Ensure this matches the format in your model
         };
         
         context.Authors.Add(author1);
         context.Authors.Add(author2);
         context.Cheeps.Add(cheep1);
         context.Cheeps.Add(cheep2);
         context.Cheeps.Add(cheep3);
         context.Cheeps.Add(cheep4);
         await context.SaveChangesAsync();  // Save the seed data to the in-memory database

        
         ICheepRepository repository = new CheepRepository(context, new AuthorRepository(context));
         
            // Act
            await repository.DeleteUserCheeps(new AuthorDTO { Name = "Helge", Email = "helge@hotmail", AuthorsFollowed = new List<string>() });
            
            
            var cheepList = await repository.ReadAllCheeps(0);
         
     
        
            // Assert
            Assert.True(cheepList.Count() == 2);
            // chek if the cheep with author helge is deleted
     }
      [Fact]
     public async Task DeleteCheepsByIdMultipleCheepsr()
     {
         // Arrange
         await using var connection = new SqliteConnection("Filename=:memory:");
         await connection.OpenAsync();                              
         var builder = new DbContextOptionsBuilder<CheepDBContext>().UseSqlite(connection);

         await using var context = new CheepDBContext(builder.Options);   
         await context.Database.EnsureCreatedAsync();               
        
         // Seed the database with a test entry
         var author1 = new Author() { AuthorId = 1, Cheeps = new List<Cheep>(), Email = "helge@hotmail", Name = "Helge", AuthorsFollowed = new List<string>()};
         var author2 = new Author() { AuthorId = 2, Cheeps = new List<Cheep>(), Email = "", Name = "Adrian", AuthorsFollowed = new List<string>() };
         
         var cheep1 = new Cheep
         {
             CheepId = 1,
             Author = author1,
             AuthorId = author1.AuthorId,
             Text = "Sådan!",
             TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime // Ensure this matches the format in your model
         };
         var cheep2 = new Cheep
         {
             CheepId = 2,
             Author = author2,
             AuthorId = author2.AuthorId,
             Text = "Dependency Injections are very important.",
             TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1690992208).UtcDateTime // Ensure this matches the format in your model
         };
         var cheep3 = new Cheep
         {
             CheepId = 3,
             Author = author1,
             AuthorId = author1.AuthorId,
             Text = "I like my cold brewed tee very much.",
             TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1691992208).UtcDateTime // Ensure this matches the format in your model
         };
         var cheep4 = new Cheep
         {
             CheepId = 4,
             Author = author2,
             AuthorId = author2.AuthorId,
             Text = "EF Core is goated!!.",
             TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1692992208).UtcDateTime // Ensure this matches the format in your model
         };
         
         context.Authors.Add(author1);
         context.Authors.Add(author2);
         context.Cheeps.Add(cheep1);
         context.Cheeps.Add(cheep2);
         context.Cheeps.Add(cheep3);
         context.Cheeps.Add(cheep4);
         await context.SaveChangesAsync();  // Save the seed data to the in-memory database

        
         ICheepRepository repository = new CheepRepository(context, new AuthorRepository(context));
         
            // Act
            await repository.DeleteCheep(1);
            
            var cheepList = await repository.ReadAllCheeps(0);
         
            
        
            // Assert
            Assert.True(cheepList.Count() == 3);
     }
     
     private static String UnixTimeStampToDateTimeString(double unixTimeStamp)
    {
        // Unix timestamp is seconds past epoch
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp);
        return dateTime.ToString("yyyy-MM-dd H:mm:ss");
    }

    [Fact]
    public async Task AddFollowersToList()
    {
        // Arrange
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();                              
        var builder = new DbContextOptionsBuilder<CheepDBContext>().UseSqlite(connection);

        await using var context = new CheepDBContext(builder.Options);   
        await context.Database.EnsureCreatedAsync();   
        
        // Seed the database with a test entry
        var author1 = new Author() { AuthorId = 1, Cheeps = new List<Cheep>(), Email = "helge@hotmail", Name = "Helge", AuthorsFollowed = new List<string>()};
        var author2 = new Author() { AuthorId = 2, Cheeps = new List<Cheep>(), Email = "", Name = "Adrian", AuthorsFollowed = new List<string>() };
        
        var cheep1 = new Cheep
        {
            CheepId = 1,
            Author = author1,
            AuthorId = author1.AuthorId,
            Text = "Sådan!",
            TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime // Ensure this matches the format in your model
        };
        
        context.Authors.Add(author1);
        context.Authors.Add(author2);
        context.Cheeps.Add(cheep1);
        
        await context.SaveChangesAsync();  // Save the seed data to the in-memory database
        
        IAuthorRepository repository = new AuthorRepository(context);
        
        await repository.FollowAuthor(author2.Name, cheep1.Author.Name);
        
        testOutputHelper.WriteLine(author2.AuthorsFollowed.ToString());

        Assert.True(author2.AuthorsFollowed.Count != 0);
        Assert.True(author2.AuthorsFollowed.Contains(author1.Name));
    }

    [Fact]
    public async Task DoesAuthorLike()
    {
        // Arrange
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();                              
        var builder = new DbContextOptionsBuilder<CheepDBContext>().UseSqlite(connection);

        await using var context = new CheepDBContext(builder.Options);   
        await context.Database.EnsureCreatedAsync();  
        
        var author1 = new Author() { AuthorId = 1, Cheeps = new List<Cheep>(), Email = "helge@hotmail", Name = "Helge", AuthorsFollowed = new List<string>()};
        var author2 = new Author() { AuthorId = 2, Cheeps = new List<Cheep>(), Email = "", Name = "Adrian", AuthorsFollowed = new List<string>() };
        
        context.Authors.Add(author1);
        context.Authors.Add(author2);
        
        var cheep1 = new Cheep
        {
            CheepId = 1,
            Author = author1,
            AuthorId = author1.AuthorId,
            Text = "Sådan!",
            TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime // Ensure this matches the format in your model
        };
        context.Cheeps.Add(cheep1);
        
        await context.SaveChangesAsync();  // Save the seed data to the in-memory database
        
        AuthorRepository authorRepository = new AuthorRepository(context);
        CheepRepository repository = new CheepRepository(context,authorRepository);

        await repository.HandleLike(author2.Name, cheep1.CheepId);
        
            
        // Assert
        Assert.True(cheep1.Likes.Count == 1);
    }

    [Fact]
    public async Task DoesAuthorUnlike()
    {
        // Arrange
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();                              
        var builder = new DbContextOptionsBuilder<CheepDBContext>().UseSqlite(connection);

        await using var context = new CheepDBContext(builder.Options);   
        await context.Database.EnsureCreatedAsync();  
        
        var author1 = new Author() { AuthorId = 1, Cheeps = new List<Cheep>(), Email = "helge@hotmail", Name = "Helge", AuthorsFollowed = new List<string>()};
        var author2 = new Author() { AuthorId = 2, Cheeps = new List<Cheep>(), Email = "", Name = "Adrian", AuthorsFollowed = new List<string>() };
        
        context.Authors.Add(author1);
        context.Authors.Add(author2);
        
        var cheep1 = new Cheep
        {
            CheepId = 1,
            Author = author1,
            AuthorId = author1.AuthorId,
            Text = "Sådan!",
            TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime // Ensure this matches the format in your model
        };
        context.Cheeps.Add(cheep1);
        
        await context.SaveChangesAsync();  // Save the seed data to the in-memory database
        
        AuthorRepository authorRepository = new AuthorRepository(context);
        CheepRepository repository = new CheepRepository(context,authorRepository);

        await repository.HandleLike(author2.Name, cheep1.CheepId); // Add like
        await repository.HandleLike(author2.Name, cheep1.CheepId); // Remove like
            
        // Assert
        Assert.True(cheep1.Likes.Count == 0);
    }

    [Fact]
    public async Task DoesAuthorDislike()
    {
        // Arrange
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();                              
        var builder = new DbContextOptionsBuilder<CheepDBContext>().UseSqlite(connection);

        await using var context = new CheepDBContext(builder.Options);   
        await context.Database.EnsureCreatedAsync();  
        
        var author1 = new Author() { AuthorId = 1, Cheeps = new List<Cheep>(), Email = "helge@hotmail", Name = "Helge", AuthorsFollowed = new List<string>()};
        var author2 = new Author() { AuthorId = 2, Cheeps = new List<Cheep>(), Email = "", Name = "Adrian", AuthorsFollowed = new List<string>() };
        
        context.Authors.Add(author1);
        context.Authors.Add(author2);
        
        var cheep1 = new Cheep
        {
            CheepId = 1,
            Author = author1,
            AuthorId = author1.AuthorId,
            Text = "Sådan!",
            TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime // Ensure this matches the format in your model
        };
        context.Cheeps.Add(cheep1);
        
        await context.SaveChangesAsync();  // Save the seed data to the in-memory database
        
        AuthorRepository authorRepository = new AuthorRepository(context);
        CheepRepository repository = new CheepRepository(context,authorRepository);

        await repository.HandleDislike(author2.Name, cheep1.CheepId);
        
            
        // Assert
        Assert.True(cheep1.Dislikes.Count == 1);
    }

    [Fact]
    public async Task DoesAuthorUndislike()
    {
        // Arrange
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();                              
        var builder = new DbContextOptionsBuilder<CheepDBContext>().UseSqlite(connection);

        await using var context = new CheepDBContext(builder.Options);   
        await context.Database.EnsureCreatedAsync();  
        
        var author1 = new Author() { AuthorId = 1, Cheeps = new List<Cheep>(), Email = "helge@hotmail", Name = "Helge", AuthorsFollowed = new List<string>()};
        var author2 = new Author() { AuthorId = 2, Cheeps = new List<Cheep>(), Email = "", Name = "Adrian", AuthorsFollowed = new List<string>() };
        
        context.Authors.Add(author1);
        context.Authors.Add(author2);
        
        var cheep1 = new Cheep
        {
            CheepId = 1,
            Author = author1,
            AuthorId = author1.AuthorId,
            Text = "Sådan!",
            TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime // Ensure this matches the format in your model
        };
        context.Cheeps.Add(cheep1);
        
        await context.SaveChangesAsync();  // Save the seed data to the in-memory database
        
        AuthorRepository authorRepository = new AuthorRepository(context);
        CheepRepository repository = new CheepRepository(context,authorRepository);

        await repository.HandleDislike(author2.Name, cheep1.CheepId); // Add like
        await repository.HandleDislike(author2.Name, cheep1.CheepId); // Remove like
            
        // Assert
        Assert.True(cheep1.Dislikes.Count == 0);
    }

    [Theory]
    [InlineData("Check this site http://example.com",
                "Check this site <a href=\"http://example.com\" target=\"_blank\">http://example.com</a>")]
    [InlineData("Visit example.com for more info",
                "Visit <a href=\"example.com\" target=\"_blank\">example.com</a> for more info")]
    [InlineData("Multiple links: example.com and https://google.com",
                "Multiple links: <a href=\"example.com\" target=\"_blank\">example.com</a> and <a href=\"https://google.com\" target=\"_blank\">https://google.com</a>")]
    public async Task ConvertLinksToAnchors_ReplacesUrlsWithAnchorTags(string input, string expected)
    {
        // Act
        var result = Shared.ConvertLinksToAnchors(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ConvertLinksToAnchors_EmptyOrNullString()
    {
        Assert.Null(Shared.ConvertLinksToAnchors(null!));

        var es = "";
        Assert.Same(es, Shared.ConvertLinksToAnchors(es));
    }

    /*public static TheoryData<string, string> GetFormattedTimeStampData =>
    [
        ["Tuesaday, 07 April 2026 12:20:21", "Invalid date"],
        [DateTime.UtcNow.ToString("F"), "just now"],
        [(DateTime.UtcNow - TimeSpan.FromMinutes(1)).ToString("F"), "1 minute ago"],
        [(DateTime.UtcNow - TimeSpan.FromMinutes(2)).ToString("F"), "2 minutes ago"],
        [(DateTime.UtcNow - TimeSpan.FromMinutes(59)).ToString("F"), "59 minutes ago"],
        [(DateTime.UtcNow - TimeSpan.FromHours(1)).ToString("F"), "1 hour ago"],
        [(DateTime.UtcNow - TimeSpan.FromHours(2)).ToString("F"), "2 hours ago"],
        [(DateTime.UtcNow - TimeSpan.FromHours(23)).ToString("F"), "23 hours ago"],
        [(DateTime.UtcNow - TimeSpan.FromDays(1)).ToString("F"), "1 day ago"],
        [(DateTime.UtcNow - TimeSpan.FromDays(2)).ToString("F"), "2 days ago"],
        [(DateTime.UtcNow - TimeSpan.FromDays(29)).ToString("F"), "29 days ago"],
        [(DateTime.UtcNow - TimeSpan.FromDays(30)).ToString("F"), "1 month ago"],
        [(DateTime.UtcNow - TimeSpan.FromDays(59)).ToString("F"), "1 month ago"],
        [(DateTime.UtcNow - TimeSpan.FromDays(60)).ToString("F"), "2 months ago"],
        [(DateTime.UtcNow - TimeSpan.FromDays(359)).ToString("F"), "11 months ago"],
        [(DateTime.UtcNow - TimeSpan.FromDays(360)).ToString("F"), "12 months ago"],
        [(DateTime.UtcNow - TimeSpan.FromDays(364)).ToString("F"), "12 months ago"],
        [(DateTime.UtcNow - TimeSpan.FromDays(365)).ToString("F"), "1 year ago"],
        [(DateTime.UtcNow - TimeSpan.FromDays(365*2 - 1)).ToString("F"), "1 year ago"],
        [(DateTime.UtcNow - TimeSpan.FromDays(365*2)).ToString("F"), "2 years ago"]
    ];
    */

    private class GetFormattedTimeStamp_TestFormats_Data : TheoryData<string, string>
    {
        public GetFormattedTimeStamp_TestFormats_Data()
        {
            var now = DateTime.UtcNow;

            Add("Tuesaday, 07 April 2026 12:20:21", "Invalid date");
            Add(now.ToString("F"), "just now");
            Add((now - TimeSpan.FromMinutes(1)).ToString("F"), "1 minute ago");
            Add((now - TimeSpan.FromMinutes(2)).ToString("F"), "2 minutes ago");
            Add((now - TimeSpan.FromMinutes(59)).ToString("F"), "59 minutes ago");
            Add((now - TimeSpan.FromHours(1)).ToString("F"), "1 hour ago");
            Add((now - TimeSpan.FromHours(2)).ToString("F"), "2 hours ago");
            Add((now - TimeSpan.FromHours(23)).ToString("F"), "23 hours ago");
            Add((now - TimeSpan.FromDays(1)).ToString("F"), "1 day ago");
            Add((now - TimeSpan.FromDays(2)).ToString("F"), "2 days ago");
            Add((now - TimeSpan.FromDays(29)).ToString("F"), "29 days ago");
            Add((now - TimeSpan.FromDays(30)).ToString("F"), "1 month ago");
            Add((now - TimeSpan.FromDays(59)).ToString("F"), "1 month ago");
            Add((now - TimeSpan.FromDays(60)).ToString("F"), "2 months ago");
            Add((now - TimeSpan.FromDays(359)).ToString("F"), "11 months ago");
            Add((now - TimeSpan.FromDays(360)).ToString("F"), "12 months ago");
            Add((now - TimeSpan.FromDays(364)).ToString("F"), "12 months ago");
            Add((now - TimeSpan.FromDays(365)).ToString("F"), "1 year ago");
            Add((now - TimeSpan.FromDays(365*2 - 1)).ToString("F"), "1 year ago");
            Add((now - TimeSpan.FromDays(365*2)).ToString("F"), "2 years ago");
        }
    }    

    [Theory]
    [ClassData(typeof(GetFormattedTimeStamp_TestFormats_Data))]
    public async Task GetFormattedTimeStamp_TestFormats(string timeStamp, string expected)
    {
        Assert.Equal(expected, Shared.GetFormattedTimeStamp(timeStamp));
    }
}
