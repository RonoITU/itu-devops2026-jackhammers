using Chirp.Core.Interfaces;
using Chirp.Infrastructure.Data;
using Chirp.Infrastructure.Repositories;
using Chirp.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Chirp.Web.Middleware;

namespace Chirp.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create the WebApplicationBuilder
            var builder = WebApplication.CreateBuilder(args);
            
            //CORS 
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    policy  =>
                    {
                        policy.WithOrigins("https://bdsagroup07chirprazor.azurewebsites.net/",
                            "http://localhost:");
                    });
            });
            
            // Add services to the container
            builder.Services.AddRazorPages();
            
            // Add Controllers (for API endpoints - primary for the simulator)
            builder.Services.AddControllers();
            
            // Once you are sure everything works, you might want to increase this value to up to 1 or 2 years
            builder.Services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(700));
            
            
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // Use PostgreSQL as the database provider
            builder.Services.AddDbContext<CheepDBContext>(options =>
                options.UseNpgsql(connectionString));
            
            // Then add Identity services
            builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
                    options.SignIn.RequireConfirmedAccount = true)
                .AddSignInManager<SignInManager<ApplicationUser>>()
                .AddEntityFrameworkStores<CheepDBContext>();
            
            // Add session support
            builder.Services.AddSession();
            
            // Register your repositories and services
            builder.Services.AddScoped<ICheepRepository, CheepRepository>();
            builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();
            builder.Services.AddScoped<ILatestRepository, LatestRepository>();
            builder.Services.AddScoped<ICheepService, CheepService>();
            builder.Services.AddScoped<IAuthorService, AuthorService>();
            builder.Services.AddScoped<ILatestService, LatestService>();

            // Build the application
            var app = builder.Build();

            // Seed the database after the application is built
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<CheepDBContext>();
                
                context.Database.Migrate();
                
                DbInitializer.SeedDatabase(context);
            }
            

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            
            app.Use(async (context, next) =>
            {
                // The Content-Security-Policy header helps to protect the webapp from XSS attacks.
                // Added connect-src to allow WebSocket connections
                context.Response.Headers.Append("Content-Security-Policy", 
                    "default-src 'self'; " +                            // Allow resources from the same origin
                    "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; " + // Allow styles from Font Awesome CDN
                    "style-src 'self' 'unsafe-inline'; " +               // Allow inline styles and styles from self
                    "img-src 'self' data:; " +  // Allow images from self and Base64-encoded images
                    "script-src-elem 'self' 'unsafe-inline'; " +         // Allow inline scripts in elements
                    "connect-src 'self' ws://localhost:53540/ wss://localhost:53539/ https://bdsagroup07chirprazor.azurewebsites.net/; " + // Allow WebSocket connections from localhost and Azure
                    "font-src 'self' https://cdnjs.cloudflare.com; " + // Allow fonts from Font Awesome CDN
                    "font-src 'self'; " +                                // Allow fonts from self
                    "frame-src 'self'; " +                               // Allow frames from self
                    "object-src 'none'; " +                              // Disallow object elements
                    "worker-src 'self';");                               // Allow workers from self
                await next();
            });

            
            

            //Use CORS
            app.UseCors();
            
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            
            // Custom middleware to handle authentication for simulator API endpoints
            app.UseMiddleware<SimulatorAuthMiddleware>(); 
            
            app.UseAuthentication();
            app.UseAuthorization();

            // Map Razor Pages
            app.MapRazorPages(); 
            
            // Map Controllers (Used for Simulator API endpoints)
            app.MapControllers();
            
            app.MapGet("/cheeps", async (ICheepService cheepService) =>
            {
                var cheeps = await cheepService.RetrieveAllCheeps();
                return Results.Ok(cheeps);
            });
            
            app.MapGet("/{userName}/follows", async (string userName, IAuthorService authorService) =>
            {
                var followedAuthors = await authorService.GetFollowedAuthors(userName);
                return Results.Ok(followedAuthors);
            });
            
            // Run the application
            app.Run();
        }
    }
}