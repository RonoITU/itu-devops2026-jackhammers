using Chirp.Web.Pages;
using Xunit.Abstractions;

namespace Chirp.TestUnit;

/// <summary>
/// General unit tests that do not belong to a specific repository class.
/// Covers utility helpers (Shared page methods) and the AuthorRepository.
///
/// CheepRepository tests live in CheepRepositoryTests.cs.
/// CheepService tests live in CheepServiceTests.cs.
/// </summary>
public class UnitTests(ITestOutputHelper testOutputHelper)
{
    // ── AuthorRepository ────────────────────────────────────────────────────────

    [Fact]
    public async Task AddFollowersToList()
    {
        // Arrange
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var builder = new DbContextOptionsBuilder<CheepDBContext>().UseSqlite(connection);

        await using var context = new CheepDBContext(builder.Options);
        await context.Database.EnsureCreatedAsync();

        var author1 = new Author() { AuthorId = 1, Cheeps = new List<Cheep>(), Email = "helge@hotmail", Name = "Helge", AuthorsFollowed = new List<string>() };
        var author2 = new Author() { AuthorId = 2, Cheeps = new List<Cheep>(), Email = "",              Name = "Adrian", AuthorsFollowed = new List<string>() };

        var cheep1 = new Cheep
        {
            CheepId   = 1,
            Author    = author1,
            AuthorId  = author1.AuthorId,
            Text      = "Sådan!",
            TimeStamp = DateTimeOffset.FromUnixTimeSeconds(1690892208).UtcDateTime
        };

        context.Authors.Add(author1);
        context.Authors.Add(author2);
        context.Cheeps.Add(cheep1);

        await context.SaveChangesAsync();

        IAuthorRepository repository = new AuthorRepository(context);

        // Act
        await repository.FollowAuthor(author2.Name, cheep1.Author.Name);

        testOutputHelper.WriteLine(author2.AuthorsFollowed.ToString());

        // Assert
        Assert.True(author2.AuthorsFollowed.Count != 0);
        Assert.True(author2.AuthorsFollowed.Contains(author1.Name));
    }

    // ── Shared page helpers ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("Check this site http://example.com",
                "Check this site <a href=\"http://example.com\" target=\"_blank\">http://example.com</a>")]
    [InlineData("Visit example.com for more info",
                "Visit <a href=\"example.com\" target=\"_blank\">example.com</a> for more info")]
    [InlineData("Multiple links: example.com and https://google.com",
                "Multiple links: <a href=\"example.com\" target=\"_blank\">example.com</a> and <a href=\"https://google.com\" target=\"_blank\">https://google.com</a>")]
    public async Task ConvertLinksToAnchors_ReplacesUrlsWithAnchorTags(string input, string expected)
    {
        var result = Shared.ConvertLinksToAnchors(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ConvertLinksToAnchors_EmptyOrNullString()
    {
        Assert.Null(Shared.ConvertLinksToAnchors(null!));

        var es = "";
        Assert.Same(es, Shared.ConvertLinksToAnchors(es));
    }

    private class GetFormattedTimeStamp_TestFormats_Data : TheoryData<string, string>
    {
        public GetFormattedTimeStamp_TestFormats_Data()
        {
            var now = DateTime.UtcNow;

            Add("Tuesaday, 07 April 2026 12:20:21", "Invalid date");
            Add(now.ToString("F"),                                          "just now");
            Add((now - TimeSpan.FromMinutes(1)).ToString("F"),             "1 minute ago");
            Add((now - TimeSpan.FromMinutes(2)).ToString("F"),             "2 minutes ago");
            Add((now - TimeSpan.FromMinutes(59)).ToString("F"),            "59 minutes ago");
            Add((now - TimeSpan.FromHours(1)).ToString("F"),               "1 hour ago");
            Add((now - TimeSpan.FromHours(2)).ToString("F"),               "2 hours ago");
            Add((now - TimeSpan.FromHours(23)).ToString("F"),              "23 hours ago");
            Add((now - TimeSpan.FromDays(1)).ToString("F"),                "1 day ago");
            Add((now - TimeSpan.FromDays(2)).ToString("F"),                "2 days ago");
            Add((now - TimeSpan.FromDays(29)).ToString("F"),               "29 days ago");
            Add((now - TimeSpan.FromDays(30)).ToString("F"),               "1 month ago");
            Add((now - TimeSpan.FromDays(59)).ToString("F"),               "1 month ago");
            Add((now - TimeSpan.FromDays(60)).ToString("F"),               "2 months ago");
            Add((now - TimeSpan.FromDays(359)).ToString("F"),              "11 months ago");
            Add((now - TimeSpan.FromDays(360)).ToString("F"),              "12 months ago");
            Add((now - TimeSpan.FromDays(364)).ToString("F"),              "12 months ago");
            Add((now - TimeSpan.FromDays(365)).ToString("F"),              "1 year ago");
            Add((now - TimeSpan.FromDays(365 * 2 - 1)).ToString("F"),     "1 year ago");
            Add((now - TimeSpan.FromDays(365 * 2)).ToString("F"),         "2 years ago");
        }
    }

    [Theory]
    [ClassData(typeof(GetFormattedTimeStamp_TestFormats_Data))]
    public async Task GetFormattedTimeStamp_TestFormats(string timeStamp, string expected)
    {
        Assert.Equal(expected, Shared.GetFormattedTimeStamp(timeStamp));
    }
}
