using Chirp.Core.DTOs;
using Chirp.Infrastructure.Services;
using Microsoft.AspNetCore.Http;

namespace Chirp.TestUnit;

/// <summary>
/// Unit tests for CheepService. Each test spins up an isolated in-memory SQLite
/// database so tests are fully independent and require no external infrastructure.
/// </summary>
public class CheepServiceTests
{
    // Shared setup helpers

    /// <summary>
    /// Creates a fresh in-memory SQLite database, wires up the real repositories,
    /// and returns a CheepService ready for testing.
    /// </summary>
    private static async Task<(SqliteConnection connection, CheepDBContext context, CheepService service)> SetupAsync()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CheepDBContext>()
            .UseSqlite(connection)
            .Options;

        var context = new CheepDBContext(options);
        await context.Database.EnsureCreatedAsync();

        var authorRepo = new AuthorRepository(context);
        var cheepRepo  = new CheepRepository(context, authorRepo);
        var service    = new CheepService(cheepRepo);

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

    private static CheepDTO MakeCheepDto(int id, string authorName, string authorEmail, string text)
        => new CheepDTO
        {
            CheepId            = id,
            Author             = new AuthorDTO { Name = authorName, Email = authorEmail, AuthorsFollowed = new List<string>() },
            Text               = text,
            FormattedTimeStamp = "2024-01-01 00:00:00"
        };

    // Image helpers

    /// <summary>
    /// Generates a minimal valid 1×1 JPEG byte array using ImageSharp so that
    /// HandleImageUpload has real image data to compress.
    /// </summary>
    private static byte[] CreateMinimalJpeg()
    {
        using var ms  = new MemoryStream();
        using var img = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgb24>(4, 4);
        img.Save(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
        return ms.ToArray();
    }

    /// <summary>
    /// Generates a minimal valid 4×4 PNG byte array using ImageSharp.
    /// </summary>
    private static byte[] CreateMinimalPng()
    {
        using var ms  = new MemoryStream();
        using var img = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgb24>(4, 4);
        img.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
        return ms.ToArray();
    }
    
    // GetPrivateCheeps

    [Fact]
    public async Task GetPrivateCheeps_ReturnsOwnCheepsAndFollowedAuthorsCheeps()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            // Alice follows Bob but not Carol
            var alice = MakeAuthor(1, "Alice", "alice@test.com", followed: new List<string> { "Bob" });
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            var carol = MakeAuthor(3, "Carol", "carol@test.com");

            ctx.Authors.AddRange(alice, bob, carol);
            ctx.Cheeps.Add(MakeCheep(1, alice, "Alice cheep"));
            ctx.Cheeps.Add(MakeCheep(2, bob,   "Bob cheep"));
            ctx.Cheeps.Add(MakeCheep(3, carol, "Carol cheep")); // should NOT appear
            await ctx.SaveChangesAsync();

            var result = await svc.GetPrivateCheeps(1, "Alice");

            Assert.Equal(2, result.Count);
            Assert.Contains(result, c => c.Author.Name == "Alice");
            Assert.Contains(result, c => c.Author.Name == "Bob");
            Assert.DoesNotContain(result, c => c.Author.Name == "Carol");
        }
    }

    [Fact]
    public async Task GetPrivateCheeps_ReturnsOnlyOwnCheeps_WhenFollowingNobody()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com"); // follows nobody
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");

            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "Alice cheep"));
            ctx.Cheeps.Add(MakeCheep(2, bob,   "Bob cheep"));
            await ctx.SaveChangesAsync();

            var result = await svc.GetPrivateCheeps(1, "Alice");

            Assert.Single(result);
            Assert.Equal("Alice", result[0].Author.Name);
        }
    }

    [Fact]
    public async Task GetPrivateCheeps_ReturnsEmptyList_WhenUserNotFound()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var result = await svc.GetPrivateCheeps(1, "GhostUser");
            Assert.Empty(result);
        }
    }
    
    // DeleteUserCheeps

    [Fact]
    public async Task DeleteUserCheeps_RemovesAllCheepsFromSpecifiedAuthor()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "Alice cheep 1"));
            ctx.Cheeps.Add(MakeCheep(2, alice, "Alice cheep 2"));
            ctx.Cheeps.Add(MakeCheep(3, bob,   "Bob cheep"));    // must survive
            await ctx.SaveChangesAsync();

            await svc.DeleteUserCheeps(new AuthorDTO
            {
                Name            = "Alice",
                Email           = "alice@test.com",
                AuthorsFollowed = new List<string>()
            });

            var remaining = ctx.Cheeps.ToList();
            Assert.Single(remaining);
            Assert.Equal("Bob cheep", remaining[0].Text);
        }
    }

    [Fact]
    public async Task DeleteUserCheeps_LeavesOtherCheepsUntouched_WhenAuthorHasNoCheeps()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var bob = MakeAuthor(1, "Bob", "bob@test.com");
            ctx.Authors.Add(bob);
            ctx.Cheeps.Add(MakeCheep(1, bob, "Bob cheep"));
            await ctx.SaveChangesAsync();

            // Delete an author who owns no cheeps
            await svc.DeleteUserCheeps(new AuthorDTO
            {
                Name            = "Alice",
                Email           = "alice@test.com",
                AuthorsFollowed = new List<string>()
            });

            Assert.Single(ctx.Cheeps.ToList());
        }
    }
}
