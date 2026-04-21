using Chirp.Core.DTOs;
using Chirp.Infrastructure.Services;
using Microsoft.AspNetCore.Http;

namespace Chirp.TestUnit;

/// <summary>
/// Unit tests for AuthorService. Each test spins up an isolated in-memory SQLite
/// database so tests are fully independent and require no external infrastructure.
///
/// AuthorService is a thin delegation layer over IAuthorRepository, so these tests
/// verify the end-to-end behaviour through the service interface by exercising the
/// real AuthorRepository against an in-memory database.
/// </summary>
public class AuthorServiceTests
{
    // Setup helpers

    private static async Task<(SqliteConnection connection, CheepDBContext context, AuthorService service)> SetupAsync()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CheepDBContext>()
            .UseSqlite(connection)
            .Options;

        var context    = new CheepDBContext(options);
        await context.Database.EnsureCreatedAsync();

        var authorRepo = new AuthorRepository(context);
        var service    = new AuthorService(authorRepo);

        return (connection, context, service);
    }

    private static Author MakeAuthor(int id, string name, string email = "test@test.com", List<string>? followed = null)
        => new Author
        {
            AuthorId        = id,
            Name            = name,
            Email           = email,
            Cheeps          = new List<Cheep>(),
            AuthorsFollowed = followed ?? new List<string>()
        };

    private static Cheep MakeCheep(int id, Author author, string text, DateTime? ts = null)
        => new Cheep
        {
            CheepId   = id,
            Author    = author,
            AuthorId  = author.AuthorId,
            Text      = text,
            TimeStamp = ts ?? DateTime.UtcNow
        };

    /// <summary>Generates a minimal valid 4×4 JPEG using ImageSharp.</summary>
    private static byte[] CreateMinimalJpeg()
    {
        using var ms  = new MemoryStream();
        using var img = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgb24>(4, 4);
        img.Save(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
        return ms.ToArray();
    }

    // FindAuthorByName

    [Fact]
    public async Task FindAuthorByName_ReturnsAuthorDTO_WhenAuthorExists()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            ctx.Authors.Add(MakeAuthor(1, "Alice", "alice@test.com"));
            await ctx.SaveChangesAsync();

            var result = await svc.FindAuthorByName("Alice");

            Assert.NotNull(result);
            Assert.Equal("Alice",          result.Name);
            Assert.Equal("alice@test.com", result.Email);
        }
    }

    [Fact]
    public async Task FindAuthorByName_ReturnsNull_WhenAuthorDoesNotExist()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var result = await svc.FindAuthorByName("Ghost");
            Assert.Null(result);
        }
    }

    // CreateAuthor

    [Fact]
    public async Task CreateAuthor_PersistsNewAuthor_WhenCalledWithoutProfilePicture()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            await svc.CreateAuthor("Alice", "alice@test.com", null);

            var author = ctx.Authors.SingleOrDefault(a => a.Name == "Alice");
            Assert.NotNull(author);
            Assert.Equal("alice@test.com", author.Email);
        }
    }

    [Fact]
    public async Task CreateAuthor_MultipleCalls_PersistAllAuthors()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            await svc.CreateAuthor("Alice", "alice@test.com", null);
            await svc.CreateAuthor("Bob",   "bob@test.com",   null);

            Assert.Equal(2, ctx.Authors.Count());
        }
    }

    // DeleteUser

    [Fact]
    public async Task DeleteUser_RemovesAuthor_WhenAuthorExists()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            ctx.Authors.Add(MakeAuthor(1, "Alice", "alice@test.com"));
            await ctx.SaveChangesAsync();

            await svc.DeleteUser(new AuthorDTO { Name = "Alice", Email = "alice@test.com", AuthorsFollowed = new List<string>() });

            Assert.Empty(ctx.Authors.ToList());
        }
    }

    [Fact]
    public async Task DeleteUser_IsNoOp_WhenAuthorDoesNotExist()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            ctx.Authors.Add(MakeAuthor(1, "Bob", "bob@test.com"));
            await ctx.SaveChangesAsync();

            var ex = await Record.ExceptionAsync(() => svc.DeleteUser(
                new AuthorDTO { Name = "Ghost", Email = "", AuthorsFollowed = new List<string>() }));

            Assert.Null(ex);
            Assert.Single(ctx.Authors.ToList());
        }
    }

    // FollowAuthor

    [Fact]
    public async Task FollowAuthor_AddsFollowedAuthorToList()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice");
            var bob   = MakeAuthor(2, "Bob");
            ctx.Authors.AddRange(alice, bob);
            await ctx.SaveChangesAsync();

            await svc.FollowAuthor("Alice", "Bob");

            Assert.Contains("Bob", alice.AuthorsFollowed);
        }
    }

    [Fact]
    public async Task FollowAuthor_DoesNotAddDuplicate_WhenAlreadyFollowing()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", followed: new List<string> { "Bob" });
            var bob   = MakeAuthor(2, "Bob");
            ctx.Authors.AddRange(alice, bob);
            await ctx.SaveChangesAsync();

            await svc.FollowAuthor("Alice", "Bob");

            Assert.Single(alice.AuthorsFollowed);
        }
    }

    // UnfollowAuthor

    [Fact]
    public async Task UnfollowAuthor_RemovesFollowedAuthorFromList()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", followed: new List<string> { "Bob" });
            var bob   = MakeAuthor(2, "Bob");
            ctx.Authors.AddRange(alice, bob);
            await ctx.SaveChangesAsync();

            await svc.UnfollowAuthor("Alice", "Bob");

            Assert.Empty(alice.AuthorsFollowed);
        }
    }

    [Fact]
    public async Task UnfollowAuthor_IsNoOp_WhenNotFollowing()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice");
            ctx.Authors.Add(alice);
            await ctx.SaveChangesAsync();

            var ex = await Record.ExceptionAsync(() => svc.UnfollowAuthor("Alice", "Bob"));

            Assert.Null(ex);
        }
    }

    // RemovedAuthorFromFollowingList 

    [Fact]
    public async Task RemovedAuthorFromFollowingList_RemovesFromAllFollowers()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", followed: new List<string> { "Bob" });
            var carol = MakeAuthor(2, "Carol", followed: new List<string> { "Bob" });
            var bob   = MakeAuthor(3, "Bob");
            ctx.Authors.AddRange(alice, carol, bob);
            await ctx.SaveChangesAsync();

            await svc.RemovedAuthorFromFollowingList("Bob");

            Assert.Empty(alice.AuthorsFollowed);
            Assert.Empty(carol.AuthorsFollowed);
        }
    }

    [Fact]
    public async Task RemovedAuthorFromFollowingList_IsNoOp_WhenNobodyFollowsTheAuthor()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            ctx.Authors.Add(MakeAuthor(1, "Alice"));
            await ctx.SaveChangesAsync();

            var ex = await Record.ExceptionAsync(() => svc.RemovedAuthorFromFollowingList("Ghost"));
            Assert.Null(ex);
        }
    }

    // GetFollowedAuthors

    [Fact]
    public async Task GetFollowedAuthors_ReturnsCorrectList_WhenAuthorFollowsSomeone()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            ctx.Authors.Add(MakeAuthor(1, "Alice", followed: new List<string> { "Bob", "Carol" }));
            await ctx.SaveChangesAsync();

            var result = await svc.GetFollowedAuthors("Alice");

            Assert.Equal(2, result.Count);
            Assert.Contains("Bob",   result);
            Assert.Contains("Carol", result);
        }
    }

    [Fact]
    public async Task GetFollowedAuthors_ReturnsEmpty_WhenAuthorFollowsNobody()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            ctx.Authors.Add(MakeAuthor(1, "Alice"));
            await ctx.SaveChangesAsync();

            var result = await svc.GetFollowedAuthors("Alice");

            Assert.Empty(result);
        }
    }

    // GetFollowingAuthors

    [Fact]
    public async Task GetFollowingAuthors_ReturnsNamesOfAuthorsWhoFollowUser()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", followed: new List<string> { "Bob" });
            var carol = MakeAuthor(2, "Carol", followed: new List<string> { "Bob" });
            var bob   = MakeAuthor(3, "Bob");
            ctx.Authors.AddRange(alice, carol, bob);
            await ctx.SaveChangesAsync();

            var result = await svc.GetFollowingAuthors("Bob");

            Assert.Equal(2, result.Count);
            Assert.Contains("Alice", result);
            Assert.Contains("Carol", result);
        }
    }

    [Fact]
    public async Task GetFollowingAuthors_ReturnsEmpty_WhenNobodyFollowsUser()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            ctx.Authors.Add(MakeAuthor(1, "Alice"));
            await ctx.SaveChangesAsync();

            var result = await svc.GetFollowingAuthors("Alice");

            Assert.Empty(result);
        }
    }

    // GetKarmaForAuthor

    [Fact]
    public async Task GetKarmaForAuthor_ReturnsZero_WhenAuthorDoesNotExist()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var result = await svc.GetKarmaForAuthor("Ghost");
            Assert.Equal(0, result);
        }
    }

    [Fact]
    public async Task GetKarmaForAuthor_ReturnsLikesMinusDislikes_ForExistingAuthor()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice    = MakeAuthor(1, "Alice");
            var liker    = MakeAuthor(2, "Liker");
            var disliker = MakeAuthor(3, "Disliker");
            ctx.Authors.AddRange(alice, liker, disliker);
            var cheep = MakeCheep(1, alice, "A cheep");
            ctx.Cheeps.Add(cheep);
            await ctx.SaveChangesAsync();

            var authorRepo = new AuthorRepository(ctx);
            var cheepRepo  = new CheepRepository(ctx, authorRepo);
            await cheepRepo.HandleLike("Liker",    cheep.CheepId);
            await cheepRepo.HandleLike("Liker",    cheep.CheepId); // toggle: remove
            await cheepRepo.HandleLike("Disliker", cheep.CheepId);
            await cheepRepo.HandleDislike("Disliker", cheep.CheepId); // switch to dislike

            var karma = await svc.GetKarmaForAuthor("Alice");

            // 0 likes, 1 dislike → karma = -1
            Assert.Equal(-1, karma);
        }
    }

    // UpdateProfilePicture

    [Fact]
    public async Task UpdateProfilePicture_SetsProfilePicture_WhenAuthorExists()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            ctx.Authors.Add(MakeAuthor(1, "Alice", "alice@test.com"));
            await ctx.SaveChangesAsync();

            var jpegBytes = CreateMinimalJpeg();
            using var stream = new MemoryStream(jpegBytes);
            var formFile = new FormFile(stream, 0, stream.Length, "image", "test.jpg")
            {
                Headers     = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };

            await svc.UpdateProfilePicture("Alice", formFile);

            var author = ctx.Authors.Single(a => a.Name == "Alice");
            Assert.NotNull(author.ProfilePicture);
        }
    }

    // ClearProfilePicture

    [Fact]
    public async Task ClearProfilePicture_SetsProfilePictureToNull()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            alice.ProfilePicture = "some-base64";
            ctx.Authors.Add(alice);
            await ctx.SaveChangesAsync();

            using var stream = new MemoryStream(Array.Empty<byte>());
            var formFile = new FormFile(stream, 0, 0, "image", "empty.jpg")
            {
                Headers     = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };

            await svc.ClearProfilePicture("Alice", formFile);

            Assert.Null(ctx.Authors.Single(a => a.Name == "Alice").ProfilePicture);
        }
    }

    [Fact]
    public async Task ClearProfilePicture_IsNoOp_WhenAuthorDoesNotExist()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            using var stream = new MemoryStream(Array.Empty<byte>());
            var formFile = new FormFile(stream, 0, 0, "image", "empty.jpg")
            {
                Headers     = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };

            var ex = await Record.ExceptionAsync(() => svc.ClearProfilePicture("Ghost", formFile));
            Assert.Null(ex);
        }
    }

    // TotalAuthorCount

    [Fact]
    public async Task TotalAuthorCount_ReturnsZero_WhenDatabaseIsEmpty()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var result = await svc.TotalAuthorCount();
            Assert.Equal(0L, result);
        }
    }

    [Fact]
    public async Task TotalAuthorCount_ReturnsCorrectCount_AfterCreatingAuthors()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            await svc.CreateAuthor("Alice", "alice@test.com", null);
            await svc.CreateAuthor("Bob",   "bob@test.com",   null);

            var result = await svc.TotalAuthorCount();

            Assert.Equal(2L, result);
        }
    }

    // GetActiveUsers

    [Fact]
    public async Task GetActiveUsers_ReturnsZero_WhenNoCheepsExist()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var result = await svc.GetActiveUsers();
            Assert.Equal(0, result);
        }
    }

    [Fact]
    public async Task GetActiveUsers_ReturnsOne_WhenOneAuthorPostedRecently()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice");
            var bob   = MakeAuthor(2, "Bob");
            ctx.Authors.AddRange(alice, bob);
            // Alice recent, Bob inactive
            ctx.Cheeps.Add(MakeCheep(1, alice, "Fresh cheep", DateTime.UtcNow.AddDays(-1)));
            ctx.Cheeps.Add(MakeCheep(2, bob,   "Old cheep",   DateTime.UtcNow.AddDays(-60)));
            await ctx.SaveChangesAsync();

            var result = await svc.GetActiveUsers();

            Assert.Equal(1, result);
        }
    }
}
