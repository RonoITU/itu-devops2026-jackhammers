using Chirp.Core.DTOs;

namespace Chirp.TestUnit;

/// <summary>
/// Unit tests for CheepRepository. Each test uses an isolated in-memory SQLite
/// database so tests are fully independent and need no external infrastructure.
///
/// Tests that previously lived in UnitTests.cs have been migrated here and
/// renamed to follow the consistent Arrange/Act/Assert naming convention.
/// </summary>
public class CheepRepositoryTests
{
    // Shared setup helpers

    private static async Task<(SqliteConnection connection, CheepDBContext context, CheepRepository repo)> SetupAsync()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CheepDBContext>()
            .UseSqlite(connection)
            .Options;

        var context = new CheepDBContext(options);
        await context.Database.EnsureCreatedAsync();

        var authorRepo = new AuthorRepository(context);
        var repo       = new CheepRepository(context, authorRepo);

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

    private static CheepDTO MakeCheepDto(int id, string authorName, string authorEmail, string text)
        => new CheepDTO
        {
            CheepId            = id,
            Author             = new AuthorDTO { Name = authorName, Email = authorEmail, AuthorsFollowed = new List<string>() },
            Text               = text,
            FormattedTimeStamp = "2024-01-01 00:00:00"
        };

    // ReadCheepsFromAuthor

    [Theory]
    [InlineData("Helge", "Hello, BDSA students!", 1690892208)]
    public async Task ReadCheepsFromAuthor_ReturnsMatchingCheep_ForGivenAuthor(
        string authorName, string messageText, long unixTimestamp)
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var author = MakeAuthor(1, authorName, "mymail");
            var cheep  = MakeCheep(1, author, messageText,
                DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime);

            ctx.Authors.Add(author);
            ctx.Cheeps.Add(cheep);
            await ctx.SaveChangesAsync();

            var result    = await repo.ReadCheepsFromAuthor(authorName, 1);
            var first     = result.First();

            Assert.Equal(authorName, first.Author.Name);
            Assert.Equal(messageText, first.Text);
            Assert.Equal(
                DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                first.FormattedTimeStamp);
        }
    }

    [Fact]
    public async Task ReadCheepsFromAuthor_ReturnsEmpty_WhenAuthorHasNoCheeps()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var author = MakeAuthor(1, "Helge", "helge@test.com");
            ctx.Authors.Add(author);
            await ctx.SaveChangesAsync();

            var result = await repo.ReadCheepsFromAuthor("Helge", 1);

            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task ReadCheepsFromAuthor_ReturnsOnlySpecifiedAuthors_Cheeps()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var helge  = MakeAuthor(1, "Helge",  "helge@test.com");
            var adrian = MakeAuthor(2, "Adrian", "adrian@test.com");
            ctx.Authors.AddRange(helge, adrian);
            ctx.Cheeps.Add(MakeCheep(1, helge,  "Helge cheep"));
            ctx.Cheeps.Add(MakeCheep(2, adrian, "Adrian cheep"));
            await ctx.SaveChangesAsync();

            var result = await repo.ReadCheepsFromAuthor("Helge", 1);

            Assert.Single(result);
            Assert.Equal("Helge", result[0].Author.Name);
        }
    }

    // ReadAllCheeps

    [Fact]
    public async Task ReadAllCheeps_ReturnsAllCheeps_WithCorrectCount()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var helge  = MakeAuthor(1, "Helge",  "helge@hotmail");
            var adrian = MakeAuthor(2, "Adrian", "");
            ctx.Authors.AddRange(helge, adrian);
            ctx.Cheeps.Add(MakeCheep(1, helge,  "Sådan!",
                DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime));
            ctx.Cheeps.Add(MakeCheep(2, adrian, "Dependency Injections are very important.",
                DateTimeOffset.FromUnixTimeSeconds(1690992208).UtcDateTime));
            ctx.Cheeps.Add(MakeCheep(3, helge,  "I like my cold brewed tee very much.",
                DateTimeOffset.FromUnixTimeSeconds(1691992208).UtcDateTime));
            ctx.Cheeps.Add(MakeCheep(4, adrian, "EF Core is goated!!.",
                DateTimeOffset.FromUnixTimeSeconds(1692992208).UtcDateTime));
            await ctx.SaveChangesAsync();

            var result = await repo.ReadAllCheeps(0);

            Assert.Equal(4, result.Count);
        }
    }

    [Fact]
    public async Task ReadAllCheeps_ReturnsEmpty_WhenNoCheepsExist()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var result = await repo.ReadAllCheeps(1);
            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task ReadAllCheeps_ReturnsCheepsInDescendingTimeOrder()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var author = MakeAuthor(1, "Alice", "alice@test.com");
            ctx.Authors.Add(author);
            var older  = MakeCheep(1, author, "Older cheep",  DateTime.UtcNow.AddHours(-2));
            var newer  = MakeCheep(2, author, "Newer cheep",  DateTime.UtcNow);
            ctx.Cheeps.AddRange(older, newer);
            await ctx.SaveChangesAsync();

            var result = await repo.ReadAllCheeps(1);

            Assert.Equal("Newer cheep", result[0].Text);
            Assert.Equal("Older cheep", result[1].Text);
        }
    }

    // ReadPrivateCheeps

    [Fact]
    public async Task ReadPrivateCheeps_ReturnsOwnAndFollowedCheeps()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com", followed: new List<string> { "Bob" });
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            var carol = MakeAuthor(3, "Carol", "carol@test.com");
            ctx.Authors.AddRange(alice, bob, carol);
            ctx.Cheeps.Add(MakeCheep(1, alice, "Alice cheep"));
            ctx.Cheeps.Add(MakeCheep(2, bob,   "Bob cheep"));
            ctx.Cheeps.Add(MakeCheep(3, carol, "Carol cheep")); // must NOT appear
            await ctx.SaveChangesAsync();

            var result = await repo.ReadPrivateCheeps(1, "Alice");

            Assert.Equal(2, result.Count);
            Assert.Contains(result, c => c.Author.Name == "Alice");
            Assert.Contains(result, c => c.Author.Name == "Bob");
            Assert.DoesNotContain(result, c => c.Author.Name == "Carol");
        }
    }

    [Fact]
    public async Task ReadPrivateCheeps_ReturnsOnlyOwnCheeps_WhenFollowingNobody()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com"); // follows nobody
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "Alice cheep"));
            ctx.Cheeps.Add(MakeCheep(2, bob,   "Bob cheep"));
            await ctx.SaveChangesAsync();

            var result = await repo.ReadPrivateCheeps(1, "Alice");

            Assert.Single(result);
            Assert.Equal("Alice", result[0].Author.Name);
        }
    }

    [Fact]
    public async Task ReadPrivateCheeps_ReturnsEmptyList_WhenUserNotFound()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var result = await repo.ReadPrivateCheeps(1, "GhostUser");
            Assert.Empty(result);
        }
    }

    // CreateCheep

    [Fact]
    public async Task CreateCheep_PersistsCheepToDatabase()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var author = MakeAuthor(1, "Alice", "alice@test.com");
            ctx.Authors.Add(author);
            await ctx.SaveChangesAsync();

            await repo.CreateCheep(MakeCheepDto(0, "Alice", "alice@test.com", "My first cheep!"));

            Assert.Single(ctx.Cheeps.ToList());
            Assert.Equal("My first cheep!", ctx.Cheeps.First().Text);
        }
    }

    [Fact]
    public async Task CreateCheep_DoesNotCreateCheep_WhenAuthorNotFound()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            // Author doesn't exist in the DB
            await repo.CreateCheep(MakeCheepDto(0, "Ghost", "ghost@test.com", "Invisible cheep"));

            Assert.Empty(ctx.Cheeps.ToList());
        }
    }

    [Fact]
    public async Task CreateCheep_AssignsCorrectAuthor()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            await ctx.SaveChangesAsync();

            await repo.CreateCheep(MakeCheepDto(0, "Bob", "bob@test.com", "Bob's cheep"));

            var cheep = ctx.Cheeps.Include(c => c.Author).Single();
            Assert.Equal("Bob", cheep.Author.Name);
        }
    }

    // GetTotalPages

    [Fact]
    public async Task GetTotalPages_ReturnsZero_WhenNoCheepsExist()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var result = await repo.GetTotalPages("");
            Assert.Equal(0, result);
        }
    }

    [Fact]
    public async Task GetTotalPages_ReturnsOne_WhenFewerThan32CheepsExist()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var author = MakeAuthor(1, "Alice", "alice@test.com");
            ctx.Authors.Add(author);
            ctx.Cheeps.Add(MakeCheep(1, author, "A cheep"));
            await ctx.SaveChangesAsync();

            var result = await repo.GetTotalPages("");

            Assert.Equal(1, result);
        }
    }

    [Fact]
    public async Task GetTotalPages_FiltersCorrectly_WhenAuthorNameProvided()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            // Alice has 1 cheep, Bob has 2 – GetTotalPages("Alice") should count only Alice's
            ctx.Cheeps.Add(MakeCheep(1, alice, "Alice cheep"));
            ctx.Cheeps.Add(MakeCheep(2, bob,   "Bob cheep 1"));
            ctx.Cheeps.Add(MakeCheep(3, bob,   "Bob cheep 2"));
            await ctx.SaveChangesAsync();

            var allPages   = await repo.GetTotalPages("");
            var alicePages = await repo.GetTotalPages("Alice");

            Assert.Equal(1, allPages);   // ceil(3/32) = 1
            Assert.Equal(1, alicePages); // ceil(1/32) = 1, but only 1 cheep counted
        }
    }

    // DeleteUserCheeps

    [Fact]
    public async Task DeleteUserCheeps_RemovesAllCheepsFromBothAuthors()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var helge  = MakeAuthor(1, "Helge",  "helge@hotmail");
            var adrian = MakeAuthor(2, "Adrian", "");
            ctx.Authors.AddRange(helge, adrian);
            ctx.Cheeps.Add(MakeCheep(1, helge,  "Sådan!",
                DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime));
            ctx.Cheeps.Add(MakeCheep(2, adrian, "Dependency Injections are very important.",
                DateTimeOffset.FromUnixTimeSeconds(1690992208).UtcDateTime));
            ctx.Cheeps.Add(MakeCheep(3, helge,  "I like my cold brewed tee very much.",
                DateTimeOffset.FromUnixTimeSeconds(1691992208).UtcDateTime));
            ctx.Cheeps.Add(MakeCheep(4, adrian, "EF Core is goated!!.",
                DateTimeOffset.FromUnixTimeSeconds(1692992208).UtcDateTime));
            await ctx.SaveChangesAsync();

            await repo.DeleteUserCheeps(new AuthorDTO { Name = "Helge",  Email = "helge@hotmail", AuthorsFollowed = new List<string>() });
            await repo.DeleteUserCheeps(new AuthorDTO { Name = "Adrian", Email = "",              AuthorsFollowed = new List<string>() });

            Assert.Empty((await repo.ReadAllCheeps(0)));
        }
    }

    [Fact]
    public async Task DeleteUserCheeps_RemovesOnlyTargetAuthors_CheepsFromMultiple()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var helge  = MakeAuthor(1, "Helge",  "helge@hotmail");
            var adrian = MakeAuthor(2, "Adrian", "");
            ctx.Authors.AddRange(helge, adrian);
            ctx.Cheeps.Add(MakeCheep(1, helge,  "Sådan!",
                DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime));
            ctx.Cheeps.Add(MakeCheep(2, adrian, "Dependency Injections are very important.",
                DateTimeOffset.FromUnixTimeSeconds(1690992208).UtcDateTime));
            ctx.Cheeps.Add(MakeCheep(3, helge,  "I like my cold brewed tee very much.",
                DateTimeOffset.FromUnixTimeSeconds(1691992208).UtcDateTime));
            ctx.Cheeps.Add(MakeCheep(4, adrian, "EF Core is goated!!.",
                DateTimeOffset.FromUnixTimeSeconds(1692992208).UtcDateTime));
            await ctx.SaveChangesAsync();

            await repo.DeleteUserCheeps(new AuthorDTO { Name = "Helge", Email = "helge@hotmail", AuthorsFollowed = new List<string>() });

            var remaining = await repo.ReadAllCheeps(0);
            Assert.Equal(2, remaining.Count);
            Assert.All(remaining, c => Assert.Equal("Adrian", c.Author.Name));
        }
    }

    // DeleteCheep

    [Fact]
    public async Task DeleteCheep_RemovesSingleCheep_WhenOnlyOneCheepExists()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var helge = MakeAuthor(1, "Helge", "helge@hotmail");
            ctx.Authors.Add(helge);
            ctx.Cheeps.Add(MakeCheep(1, helge, "Sådan!",
                DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime));
            await ctx.SaveChangesAsync();

            await repo.DeleteCheep(1);

            Assert.Empty(await repo.ReadAllCheeps(0));
        }
    }

    [Fact]
    public async Task DeleteCheep_RemovesOnlyTargetCheep_FromMultiple()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var helge  = MakeAuthor(1, "Helge",  "helge@hotmail");
            var adrian = MakeAuthor(2, "Adrian", "");
            ctx.Authors.AddRange(helge, adrian);
            ctx.Cheeps.Add(MakeCheep(1, helge,  "Sådan!",
                DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime));
            ctx.Cheeps.Add(MakeCheep(2, adrian, "Dependency Injections are very important.",
                DateTimeOffset.FromUnixTimeSeconds(1690992208).UtcDateTime));
            ctx.Cheeps.Add(MakeCheep(3, helge,  "I like my cold brewed tee very much.",
                DateTimeOffset.FromUnixTimeSeconds(1691992208).UtcDateTime));
            ctx.Cheeps.Add(MakeCheep(4, adrian, "EF Core is goated!!.",
                DateTimeOffset.FromUnixTimeSeconds(1692992208).UtcDateTime));
            await ctx.SaveChangesAsync();

            await repo.DeleteCheep(1);

            Assert.Equal(3, (await repo.ReadAllCheeps(0)).Count);
        }
    }

    [Fact]
    public async Task DeleteCheep_IsIdempotent_WhenCheepDoesNotExist()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var ex = await Record.ExceptionAsync(() => repo.DeleteCheep(999));
            Assert.Null(ex);
        }
    }

    // DeleteComment

    [Fact]
    public async Task DeleteComment_RemovesCommentById()
    {
        var (conn, ctx, repo) = await SetupAsync();
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

            await repo.DeleteComment(1);

            Assert.Empty(ctx.Comment.ToList());
        }
    }

    [Fact]
    public async Task DeleteComment_LeavesOtherCommentsIntact()
    {
        var (conn, ctx, repo) = await SetupAsync();
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

            await repo.DeleteComment(2);

            var remaining = ctx.Comment.ToList();
            Assert.Single(remaining);
            Assert.Equal("Keep me", remaining[0].Text);
        }
    }

    [Fact]
    public async Task DeleteComment_IsIdempotent_WhenCommentDoesNotExist()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var ex = await Record.ExceptionAsync(() => repo.DeleteComment(999));
            Assert.Null(ex);
        }
    }

    // HandleLike

    [Fact]
    public async Task HandleLike_AddsLike_WhenAuthorHasNotLiked()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var helge  = MakeAuthor(1, "Helge",  "helge@hotmail");
            var adrian = MakeAuthor(2, "Adrian", "");
            ctx.Authors.AddRange(helge, adrian);
            var cheep = MakeCheep(1, helge, "Sådan!",
                DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime);
            ctx.Cheeps.Add(cheep);
            await ctx.SaveChangesAsync();

            await repo.HandleLike(adrian.Name, cheep.CheepId);

            Assert.Single(cheep.Likes);
        }
    }

    [Fact]
    public async Task HandleLike_RemovesLike_WhenAuthorAlreadyLiked()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var helge  = MakeAuthor(1, "Helge",  "helge@hotmail");
            var adrian = MakeAuthor(2, "Adrian", "");
            ctx.Authors.AddRange(helge, adrian);
            var cheep = MakeCheep(1, helge, "Sådan!",
                DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime);
            ctx.Cheeps.Add(cheep);
            await ctx.SaveChangesAsync();

            await repo.HandleLike(adrian.Name, cheep.CheepId); // like
            await repo.HandleLike(adrian.Name, cheep.CheepId); // unlike

            Assert.Empty(cheep.Likes);
        }
    }

    [Fact]
    public async Task HandleLike_SwitchesFromDislikeToLike_WhenAuthorHadDisliked()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            await ctx.SaveChangesAsync();

            await repo.HandleDislike("Bob", 1);
            Assert.Single(ctx.Dislikes.ToList());

            await repo.HandleLike("Bob", 1);

            Assert.Empty(ctx.Dislikes.ToList());
            Assert.Single(ctx.Likes.ToList());
        }
    }

    [Fact]
    public async Task HandleLike_AddsEmojiReaction_WhenEmojiProvided()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            await ctx.SaveChangesAsync();

            await repo.HandleLike("Bob", 1, "😊");

            var reactions = ctx.Reaction.ToList();
            Assert.Single(reactions);
            Assert.Equal("😊", reactions[0].Emoji);
        }
    }

    // HandleDislike

    [Fact]
    public async Task HandleDislike_AddsDislike_WhenAuthorHasNotDisliked()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var helge  = MakeAuthor(1, "Helge",  "helge@hotmail");
            var adrian = MakeAuthor(2, "Adrian", "");
            ctx.Authors.AddRange(helge, adrian);
            var cheep = MakeCheep(1, helge, "Sådan!",
                DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime);
            ctx.Cheeps.Add(cheep);
            await ctx.SaveChangesAsync();

            await repo.HandleDislike(adrian.Name, cheep.CheepId);

            Assert.Single(cheep.Dislikes);
        }
    }

    [Fact]
    public async Task HandleDislike_RemovesDislike_WhenAuthorAlreadyDisliked()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var helge  = MakeAuthor(1, "Helge",  "helge@hotmail");
            var adrian = MakeAuthor(2, "Adrian", "");
            ctx.Authors.AddRange(helge, adrian);
            var cheep = MakeCheep(1, helge, "Sådan!",
                DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime);
            ctx.Cheeps.Add(cheep);
            await ctx.SaveChangesAsync();

            await repo.HandleDislike(adrian.Name, cheep.CheepId); // dislike
            await repo.HandleDislike(adrian.Name, cheep.CheepId); // un-dislike

            Assert.Empty(cheep.Dislikes);
        }
    }

    [Fact]
    public async Task HandleDislike_SwitchesFromLikeToDislike_WhenAuthorHadLiked()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            await ctx.SaveChangesAsync();

            await repo.HandleLike("Bob", 1);
            Assert.Single(ctx.Likes.ToList());

            await repo.HandleDislike("Bob", 1);

            Assert.Empty(ctx.Likes.ToList());
            Assert.Single(ctx.Dislikes.ToList());
        }
    }

    [Fact]
    public async Task HandleDislike_AddsEmojiReaction_WhenEmojiProvided()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            await ctx.SaveChangesAsync();

            await repo.HandleDislike("Bob", 1, "😢");

            var reactions = ctx.Reaction.ToList();
            Assert.Single(reactions);
            Assert.Equal("😢", reactions[0].Emoji);
        }
    }

    // GetPopularCheeps

    [Fact]
    public async Task GetPopularCheeps_ReturnsOnlyCheepsWithAtLeastOneLike()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "Liked cheep"));
            ctx.Cheeps.Add(MakeCheep(2, alice, "Unliked cheep")); // no likes → should not appear
            await ctx.SaveChangesAsync();

            await repo.HandleLike("Bob", 1);

            var result = await repo.GetPopularCheeps(1);

            Assert.Single(result);
            Assert.Equal(1, result[0].CheepId);
        }
    }

    [Fact]
    public async Task GetPopularCheeps_ReturnsCheepsSortedByLikeCountDescending()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var author1 = MakeAuthor(1, "Author1", "a1@test.com");
            var author2 = MakeAuthor(2, "Author2", "a2@test.com");
            var author3 = MakeAuthor(3, "Author3", "a3@test.com");
            ctx.Authors.AddRange(author1, author2, author3);
            ctx.Cheeps.Add(MakeCheep(1, author1, "One like"));
            ctx.Cheeps.Add(MakeCheep(2, author1, "Two likes"));
            await ctx.SaveChangesAsync();

            await repo.HandleLike("Author2", 1); // cheep 1: 1 like
            await repo.HandleLike("Author2", 2); // cheep 2: 2 likes
            await repo.HandleLike("Author3", 2);

            var result = await repo.GetPopularCheeps(1);

            Assert.Equal(2, result.Count);
            Assert.Equal(2, result[0].CheepId); // most liked first
            Assert.Equal(1, result[1].CheepId);
        }
    }

    [Fact]
    public async Task GetPopularCheeps_ReturnsEmpty_WhenNoCheepsHaveLikes()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            ctx.Authors.Add(alice);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A loveless cheep"));
            await ctx.SaveChangesAsync();

            var result = await repo.GetPopularCheeps(1);

            Assert.Empty(result);
        }
    }

    // GetTotalPageNumberForPopular

    [Fact]
    public async Task GetTotalPageNumberForPopular_ReturnsZero_WhenNoLikedCheepsExist()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var result = await repo.GetTotalPageNumberForPopular();
            Assert.Equal(0, result);
        }
    }

    [Fact]
    public async Task GetTotalPageNumberForPopular_ReturnsOne_WhenFewerThan32LikedCheepsExist()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A liked cheep"));
            await ctx.SaveChangesAsync();

            await repo.HandleLike("Bob", 1);

            var result = await repo.GetTotalPageNumberForPopular();
            Assert.Equal(1, result);
        }
    }

    [Fact]
    public async Task GetTotalPageNumberForPopular_ExcludesCheepsWithNoLikes_FromCount()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "Liked cheep"));
            ctx.Cheeps.Add(MakeCheep(2, alice, "Unliked cheep"));
            await ctx.SaveChangesAsync();

            await repo.HandleLike("Bob", 1); // only 1 liked cheep

            var result = await repo.GetTotalPageNumberForPopular();

            Assert.Equal(1, result); // ceil(1/32) = 1
        }
    }

    // RetrieveAllCheepsForEndPoint

    [Fact]
    public async Task RetrieveAllCheepsForEndPoint_ReturnsAllCheeps_WithoutPagination()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            ctx.Authors.Add(alice);
            // Add more than one page's worth would be (32), but just a few here
            for (int i = 1; i <= 5; i++)
                ctx.Cheeps.Add(MakeCheep(i, alice, $"Cheep {i}"));
            await ctx.SaveChangesAsync();

            var result = await repo.RetrieveAllCheepsForEndPoint();

            Assert.Equal(5, result.Count);
        }
    }

    [Fact]
    public async Task RetrieveAllCheepsForEndPoint_ReturnsEmpty_WhenNoCheepsExist()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var result = await repo.RetrieveAllCheepsForEndPoint();
            Assert.Empty(result);
        }
    }

    // RetrieveAllCheepsFromAnAuthor

    [Fact]
    public async Task RetrieveAllCheepsFromAnAuthor_ReturnsAllCheepsFromAuthor()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "Alice cheep 1"));
            ctx.Cheeps.Add(MakeCheep(2, alice, "Alice cheep 2"));
            ctx.Cheeps.Add(MakeCheep(3, bob,   "Bob cheep"));
            await ctx.SaveChangesAsync();

            var result = await repo.RetrieveAllCheepsFromAnAuthor("Alice");

            Assert.Equal(2, result.Count);
            Assert.All(result, c => Assert.Equal("Alice", c.Author.Name));
        }
    }

    [Fact]
    public async Task RetrieveAllCheepsFromAnAuthor_ReturnsEmpty_WhenAuthorHasNoCheeps()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var result = await repo.RetrieveAllCheepsFromAnAuthor("Ghost");
            Assert.Empty(result);
        }
    }

    // GetCommentsByCheepId

    [Fact]
    public async Task GetCommentsByCheepId_ReturnsOnlyCommentsForSpecifiedCheep()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            ctx.Authors.Add(alice);
            ctx.Cheeps.Add(MakeCheep(1, alice, "Cheep 1"));
            ctx.Cheeps.Add(MakeCheep(2, alice, "Cheep 2"));
            ctx.Comment.Add(new Comment
            {
                CommentId = 1, CheepId = 1, Author = alice, AuthorId = 1,
                Text = "On cheep 1", TimeStamp = DateTime.UtcNow
            });
            ctx.Comment.Add(new Comment
            {
                CommentId = 2, CheepId = 2, Author = alice, AuthorId = 1,
                Text = "On cheep 2", TimeStamp = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var result = await repo.GetCommentsByCheepId(1);

            Assert.Single(result);
            Assert.Equal("On cheep 1", result[0].Text);
        }
    }

    [Fact]
    public async Task GetCommentsByCheepId_ReturnsEmpty_WhenCheepHasNoComments()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            ctx.Authors.Add(alice);
            ctx.Cheeps.Add(MakeCheep(1, alice, "Silent cheep"));
            await ctx.SaveChangesAsync();

            var result = await repo.GetCommentsByCheepId(1);

            Assert.Empty(result);
        }
    }

    // AddCommentToCheep

    [Fact]
    public async Task AddCommentToCheep_PersistsCommentToDatabase()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            ctx.Authors.Add(alice);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            await ctx.SaveChangesAsync();

            await repo.AddCommentToCheep(
                MakeCheepDto(1, "Alice", "alice@test.com", "A cheep"),
                "Nice cheep!", "Alice");

            Assert.Single(ctx.Comment.ToList());
            Assert.Equal("Nice cheep!", ctx.Comment.First().Text);
        }
    }

    [Fact]
    public async Task AddCommentToCheep_AssignsCorrectCheepIdAndAuthor()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            await ctx.SaveChangesAsync();

            await repo.AddCommentToCheep(
                MakeCheepDto(1, "Alice", "alice@test.com", "A cheep"),
                "Bob's take", "Bob");

            var comment = ctx.Comment.Include(c => c.Author).Single();
            Assert.Equal(1,     comment.CheepId);
            Assert.Equal("Bob", comment.Author.Name);
        }
    }

    // GetCheepFromId

    [Fact]
    public async Task GetCheepFromId_ReturnsCorrectCheep_WhenExists()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            ctx.Authors.Add(alice);
            ctx.Cheeps.Add(MakeCheep(1, alice, "The target cheep"));
            ctx.Cheeps.Add(MakeCheep(2, alice, "Another cheep"));
            await ctx.SaveChangesAsync();

            var result = await repo.GetCheepFromId(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.CheepId);
            Assert.Equal("The target cheep", result.Text);
            Assert.Equal("Alice", result.Author.Name);
        }
    }

    [Fact]
    public async Task GetCheepFromId_ThrowsInvalidOperationException_WhenNotFound()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => repo.GetCheepFromId(999));
        }
    }

    // GetTopReactions

    [Fact]
    public async Task GetTopReactions_ReturnsEmpty_WhenCheepHasNoReactions()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            ctx.Authors.Add(alice);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            await ctx.SaveChangesAsync();

            var result = await repo.GetTopReactions(1);

            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task GetTopReactions_ReturnsMostFrequentEmojiFirst()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var author1 = MakeAuthor(1, "Author1", "a1@test.com");
            var author2 = MakeAuthor(2, "Author2", "a2@test.com");
            var author3 = MakeAuthor(3, "Author3", "a3@test.com");
            var author4 = MakeAuthor(4, "Author4", "a4@test.com");
            ctx.Authors.AddRange(author1, author2, author3, author4);
            ctx.Cheeps.Add(MakeCheep(1, author1, "A cheep"));
            await ctx.SaveChangesAsync();

            // 😊 × 2, 😢 × 1 → 😊 should be first
            await repo.HandleLike("Author2", 1, "😊");
            await repo.HandleLike("Author3", 1, "😊");
            await repo.HandleLike("Author4", 1, "😢");

            var result = await repo.GetTopReactions(1);

            Assert.NotEmpty(result);
            Assert.Equal("😊", result[0]);
        }
    }

    [Fact]
    public async Task GetTopReactions_ReturnsAtMostTopN_WhenMoreReactionsExist()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var authors = Enumerable.Range(1, 6)
                .Select(i => MakeAuthor(i, $"Author{i}", $"a{i}@test.com"))
                .ToList();
            ctx.Authors.AddRange(authors);
            ctx.Cheeps.Add(MakeCheep(1, authors[0], "A popular cheep"));
            await ctx.SaveChangesAsync();

            // 5 distinct emojis → GetTopReactions should cap at topN=3
            await repo.HandleLike("Author2", 1, "😊");
            await repo.HandleLike("Author3", 1, "😢");
            await repo.HandleLike("Author4", 1, "🔥");
            await repo.HandleLike("Author5", 1, "💯");
            await repo.HandleLike("Author6", 1, "❤️");

            var result = await repo.GetTopReactions(1); // default topN = 3

            Assert.True(result.Count <= 3);
        }
    }

    // RetriveAllCommentsFromAnAuthor

    [Fact]
    public async Task RetriveAllCommentsFromAnAuthor_ReturnsAllCommentsByAuthor()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            await ctx.SaveChangesAsync();

            var dto = MakeCheepDto(1, "Alice", "alice@test.com", "A cheep");
            await repo.AddCommentToCheep(dto, "Alice comment 1", "Alice");
            await repo.AddCommentToCheep(dto, "Alice comment 2", "Alice");
            await repo.AddCommentToCheep(dto, "Bob comment",     "Bob");

            var result = await repo.RetriveAllCommentsFromAnAuthor("Alice");

            Assert.Equal(2, result.Count);
            Assert.All(result, c => Assert.Equal("Alice", c.Author.Name));
        }
    }

    [Fact]
    public async Task RetriveAllCommentsFromAnAuthor_ReturnsEmpty_WhenAuthorHasNoComments()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var result = await repo.RetriveAllCommentsFromAnAuthor("Ghost");
            Assert.Empty(result);
        }
    }

    // TotalCheepsPosted

    [Fact]
    public async Task TotalCheepsPosted_ReturnsZero_WhenDatabaseIsEmpty()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var result = await repo.TotalCheepsPosted();
            Assert.Equal(0L, result);
        }
    }

    [Fact]
    public async Task TotalCheepsPosted_ReturnsCorrectCount_AfterAddingCheeps()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            ctx.Authors.Add(alice);
            ctx.Cheeps.Add(MakeCheep(1, alice, "Cheep 1"));
            ctx.Cheeps.Add(MakeCheep(2, alice, "Cheep 2"));
            ctx.Cheeps.Add(MakeCheep(3, alice, "Cheep 3"));
            await ctx.SaveChangesAsync();

            var result = await repo.TotalCheepsPosted();

            Assert.Equal(3L, result);
        }
    }

    // GetMessagesForSimulator

    [Fact]
    public async Task GetMessagesForSimulator_ReturnsAllCheeps_WhenNoUsernameFilter()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "Alice cheep"));
            ctx.Cheeps.Add(MakeCheep(2, bob,   "Bob cheep"));
            await ctx.SaveChangesAsync();

            var result = await repo.GetMessagesForSimulator();

            Assert.Equal(2, result.Count);
        }
    }

    [Fact]
    public async Task GetMessagesForSimulator_ReturnsOnlyUserCheeps_WhenUsernameProvided()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            var bob   = MakeAuthor(2, "Bob",   "bob@test.com");
            ctx.Authors.AddRange(alice, bob);
            ctx.Cheeps.Add(MakeCheep(1, alice, "Alice cheep"));
            ctx.Cheeps.Add(MakeCheep(2, bob,   "Bob cheep"));
            await ctx.SaveChangesAsync();

            var result = await repo.GetMessagesForSimulator("Alice");

            Assert.Single(result);
            Assert.Equal("Alice", result[0].Author.Name);
        }
    }

    [Fact]
    public async Task GetMessagesForSimulator_RespectsCountLimit()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            ctx.Authors.Add(alice);
            for (int i = 1; i <= 10; i++)
                ctx.Cheeps.Add(MakeCheep(i, alice, $"Cheep {i}"));
            await ctx.SaveChangesAsync();

            var result = await repo.GetMessagesForSimulator(count: 3);

            Assert.Equal(3, result.Count);
        }
    }

    [Fact]
    public async Task GetMessagesForSimulator_ReturnsEmpty_WhenNoMatchingUser()
    {
        var (conn, ctx, repo) = await SetupAsync();
        await using (conn)
        {
            var alice = MakeAuthor(1, "Alice", "alice@test.com");
            ctx.Authors.Add(alice);
            ctx.Cheeps.Add(MakeCheep(1, alice, "A cheep"));
            await ctx.SaveChangesAsync();

            var result = await repo.GetMessagesForSimulator("Ghost");

            Assert.Empty(result);
        }
    }
}
