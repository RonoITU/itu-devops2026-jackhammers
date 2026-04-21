using Chirp.Core.DTOs;
using Microsoft.AspNetCore.Http;

namespace Chirp.TestUnit;

/// <summary>
/// Unit tests for AuthorRepository. Each test uses an isolated in-memory SQLite
/// database so tests are fully independent and need no external infrastructure.
///
/// The single AuthorRepository test that previously lived in UnitTests.cs has been
/// migrated here and the suite has been extended to reach full method coverage.
/// </summary>
public class AuthorRepositoryTests
{
    // Setup helpers

    private static async Task<(SqliteConnection connection, CheepDBContext context, AuthorRepository repo)> SetupAsync()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CheepDBContext>()
            .UseSqlite(connection)
            .Options;

        var context = new CheepDBContext(options);
        await context.Database.EnsureCreatedAsync();

        var repo = new AuthorRepository(context);
        return (connection, context, repo);
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

    // FindAuthorByNameDto

    [Fact]
    public async Task FindAuthorByNameDto_ReturnsAuthorDTO_WhenAuthorExists()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            ctx.Authors.Add(MakeAuthor(1, "Alice", "alice@test.com"));
            await ctx.SaveChangesAsync();

            var result = await repo.FindAuthorByNameDto("Alice");

            Assert.NotNull(result);
            Assert.Equal("Alice",          result.Name);
            Assert.Equal("alice@test.com", result.Email);
        }
    }

    [Fact]
    public async Task FindAuthorByNameDto_ReturnsNull_WhenAuthorDoesNotExist()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var result = await repo.FindAuthorByNameDto("Ghost");
            Assert.Null(result);
        }
    }

    // FindAuthorByName

    [Fact]
    public async Task FindAuthorByName_ReturnsAuthorEntity_WhenAuthorExists()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            ctx.Authors.Add(MakeAuthor(1, "Bob", "bob@test.com"));
            await ctx.SaveChangesAsync();

            var result = await repo.FindAuthorByName("Bob");

            Assert.NotNull(result);
            Assert.Equal("Bob", result.Name);
        }
    }

    [Fact]
    public async Task FindAuthorByName_ReturnsNull_WhenAuthorDoesNotExist()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var result = await repo.FindAuthorByName("Ghost");
            Assert.Null(result);
        }
    }

    // CreateAuthor

    [Fact]
    public async Task CreateAuthor_PersistsNewAuthor_WithoutProfilePicture()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            await repo.CreateAuthor("Alice", "alice@test.com", null);

            var author = ctx.Authors.SingleOrDefault(a => a.Name == "Alice");
            Assert.NotNull(author);
            Assert.Equal("alice@test.com", author.Email);
            Assert.Null(author.ProfilePicture);
        }
    }

    [Fact]
    public async Task CreateAuthor_InitialisesEmptyCheepsAndFollowingLists()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            await repo.CreateAuthor("Alice", "alice@test.com", null);

            var author = ctx.Authors.Single(a => a.Name == "Alice");
            Assert.Empty(author.Cheeps);
            Assert.Empty(author.AuthorsFollowed);
        }
    }

    // DeleteUser

    [Fact]
    public async Task DeleteUser_RemovesAuthor_WhenAuthorExists()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            ctx.Authors.Add(MakeAuthor(1, "Alice", "alice@test.com"));
            await ctx.SaveChangesAsync();

            await repo.DeleteUser(new AuthorDTO { Name = "Alice", Email = "alice@test.com", AuthorsFollowed = new List<string>() });

            Assert.Empty(ctx.Authors.ToList());
        }
    }

    [Fact]
    public async Task DeleteUser_IsNoOp_WhenAuthorDoesNotExist()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            ctx.Authors.Add(MakeAuthor(1, "Bob", "bob@test.com"));
            await ctx.SaveChangesAsync();

            // Deleting a non-existent author should not throw and should leave Bob intact
            var ex = await Record.ExceptionAsync(() => repo.DeleteUser(
                new AuthorDTO { Name = "Ghost", Email = "", AuthorsFollowed = new List<string>() }));

            Assert.Null(ex);
            Assert.Single(ctx.Authors.ToList());
        }
    }

    // FollowAuthor

    [Fact]
    public async Task FollowAuthor_AddsFollowedAuthor_ToFollowingList()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            await ctx.SaveChangesAsync();

            await repo.FollowAuthor("Alice", "Bob");

            Assert.Contains("Bob", alice.AuthorsFollowed);
        }
    }

    [Fact]
    public async Task FollowAuthor_DoesNotAddDuplicate_WhenAlreadyFollowing()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com", followed: new List<string> { "Bob" });
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            await ctx.SaveChangesAsync();

            await repo.FollowAuthor("Alice", "Bob"); // already following

            Assert.Single(alice.AuthorsFollowed); // still just one entry
        }
    }

    [Fact]
    public async Task FollowAuthor_IsNoOp_WhenUserAuthorDoesNotExist()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var ex = await Record.ExceptionAsync(() => repo.FollowAuthor("Ghost", "Bob"));
            Assert.Null(ex);
        }
    }

    // UnfollowAuthor

    [Fact]
    public async Task UnfollowAuthor_RemovesFollowedAuthor_FromFollowingList()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com", followed: new List<string> { "Bob" });
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            await ctx.SaveChangesAsync();

            await repo.UnfollowAuthor("Alice", "Bob");

            Assert.Empty(alice.AuthorsFollowed);
        }
    }

    [Fact]
    public async Task UnfollowAuthor_IsNoOp_WhenNotFollowing()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com"); // follows nobody
            ctx.Authors.Add(alice);
            await ctx.SaveChangesAsync();

            var ex = await Record.ExceptionAsync(() => repo.UnfollowAuthor("Alice", "Bob"));

            Assert.Null(ex);
            Assert.Empty(alice.AuthorsFollowed);
        }
    }

    // RemovedAuthorFromFollowingList

    [Fact]
    public async Task RemovedAuthorFromFollowingList_RemovesAuthorFromAllFollowersLists()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com", followed: new List<string> { "Bob" });
            var carol = MakeAuthor(2, "Carol", "carol@test.com", followed: new List<string> { "Bob" });
            var bob   = MakeAuthor(3, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, carol, bob);
            await ctx.SaveChangesAsync();

            await repo.RemovedAuthorFromFollowingList("Bob");

            Assert.Empty(alice.AuthorsFollowed);
            Assert.Empty(carol.AuthorsFollowed);
        }
    }

    [Fact]
    public async Task RemovedAuthorFromFollowingList_IsNoOp_WhenNoOnefollowsTheAuthor()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            ctx.Authors.Add(alice);
            await ctx.SaveChangesAsync();

            var ex = await Record.ExceptionAsync(() => repo.RemovedAuthorFromFollowingList("Ghost"));
            Assert.Null(ex);
        }
    }

    // GetFollowedAuthors

    [Fact]
    public async Task GetFollowedAuthors_ReturnsFollowedList_WhenAuthorExists()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com", followed: new List<string> { "Bob", "Carol" });
            ctx.Authors.Add(alice);
            await ctx.SaveChangesAsync();

            var result = await repo.GetFollowedAuthors("Alice");

            Assert.Equal(2, result.Count);
            Assert.Contains("Bob",   result);
            Assert.Contains("Carol", result);
        }
    }

    [Fact]
    public async Task GetFollowedAuthors_ReturnsEmpty_WhenAuthorDoesNotExist()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var result = await repo.GetFollowedAuthors("Ghost");
            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task GetFollowedAuthors_ReturnsEmpty_WhenAuthorFollowsNobody()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            ctx.Authors.Add(MakeAuthor(1, "Alice", "alice@test.com"));
            await ctx.SaveChangesAsync();

            var result = await repo.GetFollowedAuthors("Alice");

            Assert.Empty(result);
        }
    }

    // GetFollowingAuthors

    [Fact]
    public async Task GetFollowingAuthors_ReturnsNamesOfAuthorsWhoFollowUser()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com", followed: new List<string> { "Bob" });
            var carol = MakeAuthor(2, "Carol", "carol@test.com", followed: new List<string> { "Bob" });
            var bob   = MakeAuthor(3, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, carol, bob);
            await ctx.SaveChangesAsync();

            var result = await repo.GetFollowingAuthors("Bob");

            Assert.Equal(2, result.Count);
            Assert.Contains("Alice", result);
            Assert.Contains("Carol", result);
        }
    }

    [Fact]
    public async Task GetFollowingAuthors_ReturnsEmpty_WhenNobodyFollowsUser()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            ctx.Authors.Add(MakeAuthor(1, "Alice", "alice@test.com"));
            await ctx.SaveChangesAsync();

            var result = await repo.GetFollowingAuthors("Alice");

            Assert.Empty(result);
        }
    }

    // GetKarmaForAuthor

    [Fact]
    public async Task GetKarmaForAuthor_ReturnsZero_WhenAuthorDoesNotExist()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var result = await repo.GetKarmaForAuthor("Ghost");
            Assert.Equal(0, result);
        }
    }

    [Fact]
    public async Task GetKarmaForAuthor_ReturnsZero_WhenAuthorHasNoCheeps()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            ctx.Authors.Add(MakeAuthor(1, "Alice", "alice@test.com"));
            await ctx.SaveChangesAsync();

            var result = await repo.GetKarmaForAuthor("Alice");

            Assert.Equal(0, result);
        }
    }

    [Fact]
    public async Task GetKarmaForAuthor_ReturnsLikesMinusDislikes()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice    = MakeAuthor(1, "Alice",    "alice@test.com");
            var liker1   = MakeAuthor(2, "Liker1",   "l1@test.com");
            var liker2   = MakeAuthor(3, "Liker2",   "l2@test.com");
            var disliker = MakeAuthor(4, "Disliker", "d@test.com");
            ctx.Authors.AddRange(alice, liker1, liker2, disliker);
            var cheep = MakeCheep(1, alice, "A great cheep");
            ctx.Cheeps.Add(cheep);
            await ctx.SaveChangesAsync();

            // Use CheepRepository to add likes/dislikes
            var cheepRepo = new CheepRepository(ctx, repo);
            await cheepRepo.HandleLike("Liker1",  cheep.CheepId);
            await cheepRepo.HandleLike("Liker2",  cheep.CheepId);
            await cheepRepo.HandleDislike("Disliker", cheep.CheepId);

            var karma = await repo.GetKarmaForAuthor("Alice");

            Assert.Equal(1, karma); // 2 likes − 1 dislike
        }
    }

    // UpdateProfilePicture

    [Fact]
    public async Task UpdateProfilePicture_SetsBase64ProfilePicture_WhenAuthorExists()
    {
        var (conn, ctx, repo) = await SetupAsync();
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

            await repo.UpdateProfilePicture("Alice", formFile);

            var author = ctx.Authors.Single(a => a.Name == "Alice");
            Assert.NotNull(author.ProfilePicture);
            Assert.NotEmpty(author.ProfilePicture);
        }
    }

    [Fact]
    public async Task UpdateProfilePicture_IsNoOp_WhenAuthorDoesNotExist()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var jpegBytes = CreateMinimalJpeg();
            using var stream = new MemoryStream(jpegBytes);
            var formFile = new FormFile(stream, 0, stream.Length, "image", "test.jpg")
            {
                Headers     = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };

            var ex = await Record.ExceptionAsync(() => repo.UpdateProfilePicture("Ghost", formFile));
            Assert.Null(ex);
        }
    }

    // ClearProfilePicture

    [Fact]
    public async Task ClearProfilePicture_SetsProfilePictureToNull_WhenAuthorExists()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            alice.ProfilePicture = "some-base64-string";
            ctx.Authors.Add(alice);
            await ctx.SaveChangesAsync();

            // ClearProfilePicture expects an IFormFile parameter (unused internally)
            using var stream   = new MemoryStream(Array.Empty<byte>());
            var formFile = new FormFile(stream, 0, 0, "image", "empty.jpg")
            {
                Headers     = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };

            await repo.ClearProfilePicture("Alice", formFile);

            var author = ctx.Authors.Single(a => a.Name == "Alice");
            Assert.Null(author.ProfilePicture);
        }
    }

    [Fact]
    public async Task ClearProfilePicture_IsNoOp_WhenAuthorDoesNotExist()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            using var stream = new MemoryStream(Array.Empty<byte>());
            var formFile = new FormFile(stream, 0, 0, "image", "empty.jpg")
            {
                Headers     = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };

            var ex = await Record.ExceptionAsync(() => repo.ClearProfilePicture("Ghost", formFile));
            Assert.Null(ex);
        }
    }

    // TotalAuthorCount

    [Fact]
    public async Task TotalAuthorCount_ReturnsZero_WhenDatabaseIsEmpty()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var result = await repo.TotalAuthorCount();
            Assert.Equal(0L, result);
        }
    }

    [Fact]
    public async Task TotalAuthorCount_ReturnsCorrectCount_AfterAddingAuthors()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            ctx.Authors.AddRange(MakeAuthor(1, "Alice"), MakeAuthor(2, "Bob"), MakeAuthor(3, "Carol"));
            await ctx.SaveChangesAsync();

            var result = await repo.TotalAuthorCount();

            Assert.Equal(3L, result);
        }
    }

    // GetActiveUsers

    [Fact]
    public async Task GetActiveUsers_ReturnsZero_WhenNoCheepsExist()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var result = await repo.GetActiveUsers();
            Assert.Equal(0, result);
        }
    }

    [Fact]
    public async Task GetActiveUsers_CountsOnlyAuthorsWithRecentCheeps()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice");
            var bob   = MakeAuthor(2, "Bob");
            ctx.Authors.AddRange(alice, bob);
            // Alice posted recently; Bob posted 60 days ago (inactive)
            ctx.Cheeps.Add(MakeCheep(1, alice, "Fresh cheep",   DateTime.UtcNow.AddDays(-1)));
            ctx.Cheeps.Add(MakeCheep(2, bob,   "Old cheep",     DateTime.UtcNow.AddDays(-60)));
            await ctx.SaveChangesAsync();

            var result = await repo.GetActiveUsers();

            Assert.Equal(1, result);
        }
    }

    [Fact]
    public async Task GetActiveUsers_CountsDistinctAuthors_NotCheepCount()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice");
            ctx.Authors.Add(alice);
            // Two recent cheeps from the same author — should still count as 1
            ctx.Cheeps.Add(MakeCheep(1, alice, "Cheep 1", DateTime.UtcNow.AddDays(-1)));
            ctx.Cheeps.Add(MakeCheep(2, alice, "Cheep 2", DateTime.UtcNow.AddDays(-2)));
            await ctx.SaveChangesAsync();

            var result = await repo.GetActiveUsers();

            Assert.Equal(1, result);
        }
    }

    // CompressImage

    [Fact]
    public async Task CompressImage_ReturnsEmptyArray_WhenFileIsEmpty()
    {
        using var stream = new MemoryStream(Array.Empty<byte>());
        var formFile = new FormFile(stream, 0, 0, "image", "empty.jpg")
        {
            Headers     = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        var result = await AuthorRepository.CompressImage(formFile);

        Assert.Empty(result);
    }

    [Fact]
    public async Task CompressImage_ReturnsNonEmptyBytes_ForValidJpeg()
    {
        var jpegBytes = CreateMinimalJpeg();
        using var stream = new MemoryStream(jpegBytes);
        var formFile = new FormFile(stream, 0, stream.Length, "image", "test.jpg")
        {
            Headers     = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        var result = await AuthorRepository.CompressImage(formFile);

        Assert.NotEmpty(result);
    }

    // DownloadAndConvertToBase64Async

    [Fact]
    public async Task DownloadAndConvertToBase64Async_Throws_WhenUrlIsInvalid()
    {
        await Assert.ThrowsAnyAsync<Exception>(
            () => AuthorRepository.DownloadAndConvertToBase64Async("http://localhost:0/invalid-url"));
    }
}
