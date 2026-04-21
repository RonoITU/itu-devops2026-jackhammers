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
    
    // DeleteComment

    [Fact]
    public async Task DeleteComment_RemovesCommentById()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            ctx.Authors.Add(alice);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            ctx.Comment.Add(new Comment
            {
                CommentId = 1, CheepId = 1, Author = alice, AuthorId = 1,
                Text = "A comment", TimeStamp = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            await svc.DeleteComment(1);

            Assert.Empty(ctx.Comment.ToList());
        }
    }

    [Fact]
    public async Task DeleteComment_LeavesOtherCommentsIntact()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            ctx.Authors.Add(alice);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            ctx.Comment.Add(new Comment
            {
                CommentId = 1, CheepId = 1, Author = alice, AuthorId = 1,
                Text = "Keep me", TimeStamp = DateTime.UtcNow
            });
            ctx.Comment.Add(new Comment
            {
                CommentId = 2, CheepId = 1, Author = alice, AuthorId = 1,
                Text = "Delete me", TimeStamp = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            await svc.DeleteComment(2);

            var remaining = ctx.Comment.ToList();
            Assert.Single(remaining);
            Assert.Equal("Keep me", remaining[0].Text);
        }
    }

    [Fact]
    public async Task DeleteComment_IsIdempotent_WhenCommentDoesNotExist()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            // Deleting a non-existent comment should not throw
            var ex = await Record.ExceptionAsync(() => svc.DeleteComment(999));
            Assert.Null(ex);
        }
    }
    
    // HandleLike

    [Fact]
    public async Task HandleLike_AddsLike_WhenAuthorHasNotLikedBefore()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            await ctx.SaveChangesAsync();

            await svc.HandleLike("Bob", 1, null);

            Assert.Single(ctx.Likes.ToList());
        }
    }

    [Fact]
    public async Task HandleLike_RemovesLike_WhenAuthorAlreadyLiked()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            await ctx.SaveChangesAsync();

            await svc.HandleLike("Bob", 1, null); // like
            await svc.HandleLike("Bob", 1, null); // unlike (toggle)

            Assert.Empty(ctx.Likes.ToList());
        }
    }

    [Fact]
    public async Task HandleLike_SwitchesFromDislikeToLike()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            await ctx.SaveChangesAsync();

            await svc.HandleDislike("Bob", 1, null);
            Assert.Single(ctx.Dislikes.ToList());

            await svc.HandleLike("Bob", 1, null);

            Assert.Empty(ctx.Dislikes.ToList());
            Assert.Single(ctx.Likes.ToList());
        }
    }

    [Fact]
    public async Task HandleLike_AddsEmojiReaction_WhenEmojiProvided()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            await ctx.SaveChangesAsync();

            await svc.HandleLike("Bob", 1, "😊");

            var reactions = ctx.Reaction.ToList();
            Assert.Single(reactions);
            Assert.Equal("😊", reactions[0].Emoji);
        }
    }
    
    // HandleDislike

    [Fact]
    public async Task HandleDislike_AddsDislike_WhenAuthorHasNotDislikedBefore()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            await ctx.SaveChangesAsync();

            await svc.HandleDislike("Bob", 1, null);

            Assert.Single(ctx.Dislikes.ToList());
        }
    }

    [Fact]
    public async Task HandleDislike_RemovesDislike_WhenAuthorAlreadyDisliked()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            await ctx.SaveChangesAsync();

            await svc.HandleDislike("Bob", 1, null); // dislike
            await svc.HandleDislike("Bob", 1, null); // un-dislike (toggle)

            Assert.Empty(ctx.Dislikes.ToList());
        }
    }

    [Fact]
    public async Task HandleDislike_SwitchesFromLikeToDislike()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            await ctx.SaveChangesAsync();

            await svc.HandleLike("Bob", 1, null);
            Assert.Single(ctx.Likes.ToList());

            await svc.HandleDislike("Bob", 1, null);

            Assert.Empty(ctx.Likes.ToList());
            Assert.Single(ctx.Dislikes.ToList());
        }
    }

    [Fact]
    public async Task HandleDislike_AddsEmojiReaction_WhenEmojiProvided()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            await ctx.SaveChangesAsync();

            await svc.HandleDislike("Bob", 1, "😢");

            var reactions = ctx.Reaction.ToList();
            Assert.Single(reactions);
            Assert.Equal("😢", reactions[0].Emoji);
        }
    }
    
    // GetPopularCheeps

    [Fact]
    public async Task GetPopularCheeps_ReturnsOnlyCheepsWithAtLeastOneLike()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "Liked cheep"));
            ctx.Cheeps.Add(MakeCheep(2, alice, "Unliked cheep")); // no likes
            await ctx.SaveChangesAsync();

            await svc.HandleLike("Bob", 1, null);

            var result = await svc.GetPopularCheeps(1);

            Assert.Single(result);
            Assert.Equal(1, result[0].CheepId);
        }
    }

    [Fact]
    public async Task GetPopularCheeps_ReturnsCheepsSortedByLikeCountDescending()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var author1 = MakeAuthor(1, "Author1", "a1@test.com");
            var author2 = MakeAuthor(2, "Author2", "a2@test.com");
            var author3 = MakeAuthor(3, "Author3", "a3@test.com");
            ctx.Authors.AddRange(author1, author2, author3);
            ctx.Cheeps.Add(MakeCheep(1, author1, "One like"));
            ctx.Cheeps.Add(MakeCheep(2, author1, "Two likes"));
            await ctx.SaveChangesAsync();

            await svc.HandleLike("Author2", 1, null);           // cheep 1 → 1 like
            await svc.HandleLike("Author2", 2, null);           // cheep 2 → 2 likes
            await svc.HandleLike("Author3", 2, null);

            var result = await svc.GetPopularCheeps(1);

            Assert.Equal(2, result.Count);
            Assert.Equal(2, result[0].CheepId); // most liked first
            Assert.Equal(1, result[1].CheepId);
        }
    }

    [Fact]
    public async Task GetPopularCheeps_ReturnsEmpty_WhenNoCheepsHaveLikes()
    {
        var (conn, ctx, svc) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            ctx.Authors.Add(alice);
            ctx.Cheeps.Add(MakeCheep(1, alice, "An unloved cheep"));
            await ctx.SaveChangesAsync();

            var result = await svc.GetPopularCheeps(1);

            Assert.Empty(result);
        }
    }

}
