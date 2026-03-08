namespace Chirp.TestIntegration;

public class IntegrationFixture : IDisposable
{
    public HttpClient _client = null!;
    public WebApplicationFactory<Program> _factory = null!;
    private readonly PostgreSqlContainer _postgresContainer;

    public IntegrationFixture()
    {
        // Initialize the PostgreSQL container with desired configuration
        _postgresContainer = new PostgreSqlBuilder("postgres:15-alpine")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        // Start the PostgreSQL container (This order is important: start the db container before creating the factory)
        _postgresContainer.StartAsync().Wait();

        // Create the factory with the postgresql container's connection string
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // This commented code is not needed anymore, but in case something happens,
                // and we need it, I will keep it here for reference.
                //
                // Remove existing DbContext registration
                // var descriptor = services.SingleOrDefault(
                //     d => d.ServiceType == typeof(DbContextOptions<CheepDBContext>));
                //
                // if (descriptor != null)
                // {
                //     services.Remove(descriptor);
                // }

                // Re-register with test PostgreSQL
                services.AddDbContext<CheepDBContext>(options =>
                {
                    options.UseNpgsql(_postgresContainer.GetConnectionString());
                });
            });
        });

        _client = _factory.CreateClient();

        // Ensure database is created and migrated
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CheepDBContext>();
        dbContext.Database.MigrateAsync().Wait();

        // Option to seed the database with initial data if needed
        // DbInitializer.SeedDatabase(dbContext);
    }

    public async void Dispose()
    {
        GC.SuppressFinalize(this);
        // Stop and clean up the container
        _postgresContainer.StopAsync().Wait();
        _postgresContainer.DisposeAsync().AsTask().Wait();
    }

}