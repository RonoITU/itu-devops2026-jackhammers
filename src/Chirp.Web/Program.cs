using System.Security.Claims;
using Chirp.Infrastructure.Data;
using Chirp.Infrastructure.Repositories;
using Chirp.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Chirp.Core.DTOs;
using Microsoft.AspNetCore.OpenApi;

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

            // Retrieve ClientId and ClientSecret from configuration
            //string? clientId = builder.Configuration["AUTHENTICATION_GITHUB_CLIENTID"];
            //string? clientSecret = builder.Configuration["AUTHENTICATION_GITHUB_CLIENTSECRET"];

            // if (string.IsNullOrEmpty(clientId) && string.IsNullOrEmpty(clientSecret))
            // {
            //     throw new ApplicationException("Failed to retrieve both the Github Client ID and Secret. Make sure that the values are set on the machine.");
            // }
            // if (string.IsNullOrEmpty(clientId))
            // {
            //     throw new ApplicationException("Failed to retrieve the Github Client ID. Make sure that the github value is set on the machine.");
            // }
            // if (string.IsNullOrEmpty(clientSecret))
            // {
            //     throw new ApplicationException("Failed to retrieve the Github Secret. Make sure that the github value is set on the machine.");
            // }
            
            // Add GitHub Services
            // builder.Services.AddAuthentication()
            //     .AddGitHub(options =>
            //     {
            //         options.ClientId = clientId;
            //         options.ClientSecret = clientSecret;
            //         options.CallbackPath = new PathString("/signin-github");
            //         options.Scope.Add("user:email");
            //         options.ClaimActions.MapJsonKey("urn:github:avatar_url", "avatar_url");
            //
            //
            //         options.Events.OnCreatingTicket = context =>
            //         {
            //             // Retrieve user details from claims
            //             var userName = context.Identity?.FindFirst(c => c.Type == ClaimTypes.Name)?.Value;
            //             var email = context.Identity?.FindFirst(c => c.Type == ClaimTypes.Email)?.Value;
            //
            //             // You can use these values as needed in your application
            //             Console.WriteLine($"GitHub Username: {userName}");
            //             Console.WriteLine($"GitHub Email: {email}");
            //
            //             return Task.CompletedTask;
            //         };
            //     }); 
            
            builder.Services.AddSession();
            
            // Register your repositories and services
            builder.Services.AddScoped<CheepRepository>();
            builder.Services.AddScoped<AuthorRepository>();
            builder.Services.AddScoped<LatestRepository>();
            builder.Services.AddScoped<CheepService>();
            builder.Services.AddScoped<AuthorService>();
            builder.Services.AddScoped<LatestService>();

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
            app.UseAuthentication();
            app.UseAuthorization();

            // Map Razor Pages
            app.MapRazorPages();    
            
            app.MapGet("/cheeps", async (CheepService cheepService) =>
            {
                var cheeps = await cheepService.RetrieveAllCheeps();
                return Results.Ok(cheeps);
            });
            
            app.MapGet("/{userName}/follows", async (string userName, AuthorService authorService) =>
            {
                var followedAuthors = await authorService.GetFollowedAuthors(userName);
                return Results.Ok(followedAuthors);
            });
            
            // Simulator API endpoints
            var simulatorApi = app.MapGroup("/")
                .WithOpenApi();

            // GET /latest - Now async
            simulatorApi.MapGet("/latest", async (LatestService latestService) =>
            {
                var latest = await latestService.GetLatestAsync();
                return Results.Ok(new LatestResponse { Latest = latest });
            });

            // POST /register - Update to use async
            simulatorApi.MapPost("/register", async (
                [FromBody] RegisterRequest request,
                [FromQuery] int? latest,
                AuthorService authorService,
                LatestService latestService) =>
            {
                await latestService.UpdateLatestAsync(latest);
                
                try
                {
                    var existingAuthor = await authorService.FindAuthorByName(request.Username);
                    if (existingAuthor != null)
                    {
                        return Results.BadRequest(new { error = "User already exists" });
                    }
                    
                    if (string.IsNullOrWhiteSpace(request.Username))
                    {
                        return Results.BadRequest(new { error = "Username is required" });
                    }
                    
                    if (string.IsNullOrWhiteSpace(request.Email))
                    {
                        return Results.BadRequest(new { error = "Email is required" });
                    }
                    
                    await authorService.CreateAuthor(request.Username, request.Email, null);
                    
                    return Results.NoContent();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            // POST /msgs/{username} - Update to use async
            simulatorApi.MapPost("/msgs/{username}", async (
                string username,
                [FromBody] MessageRequest request,
                [FromQuery] int? latest,
                AuthorService authorService,
                CheepService cheepService,
                LatestService latestService) =>
            {
                await latestService.UpdateLatestAsync(latest);
                
                try
                {
                    var author = await authorService.FindAuthorByName(username);
                    if (author == null)
                    {
                        return Results.NotFound(new { error = "User not found" });
                    }
                    
                    if (string.IsNullOrWhiteSpace(request.Content))
                    {
                        return Results.BadRequest(new { error = "Message content is required" });
                    }
                    
                    if (request.Content.Length > 160)
                    {
                        return Results.BadRequest(new { error = "Message content must be 160 characters or less" });
                    }
                    
                    var cheepDto = new CheepDTO
                    {
                        Author = author,
                        Text = request.Content,
                        ImageReference = null,
                        FormattedTimeStamp = DateTime.UtcNow.ToString()
                    };
                    
                    await cheepService.CreateCheep(cheepDto);
                    
                    return Results.NoContent();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            // GET /msgs/{username} - Update to use async
            simulatorApi.MapGet("/msgs/{username}", async (
                string username,
                [FromQuery] int? no,
                [FromQuery] int? latest,
                CheepService cheepService,
                LatestService latestService) =>
            {
                await latestService.UpdateLatestAsync(latest);
                
                try
                {
                    int count = no ?? 100;
                    
                    var cheeps = await cheepService.GetMessagesForSimulator(username, count);
                    
                    var messages = cheeps.Select(c => new MessageResponse
                    {
                        Content = c.Text,
                        Pub_date = c.FormattedTimeStamp,
                        User = c.Author.Name
                    }).ToList();
                    
                    return Results.Ok(messages);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            // GET /msgs - Update to use async
            simulatorApi.MapGet("/msgs", async (
                [FromQuery] int? no,
                [FromQuery] int? latest,
                CheepService cheepService,
                LatestService latestService) =>
            {
                await latestService.UpdateLatestAsync(latest);
                
                try
                {
                    int count = no ?? 100;
                    
                    var cheeps = await cheepService.GetMessagesForSimulator(null, count);
                    
                    var messages = cheeps.Select(c => new MessageResponse
                    {
                        Content = c.Text,
                        Pub_date = c.FormattedTimeStamp,
                        User = c.Author.Name
                    }).ToList();
                    
                    return Results.Ok(messages);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            // POST /fllws/{username} - Update to use async
            simulatorApi.MapPost("/fllws/{username}", async (
                string username,
                [FromBody] FollowRequest request,
                [FromQuery] int? latest,
                AuthorService authorService,
                LatestService latestService) =>
            {
                await latestService.UpdateLatestAsync(latest);
                
                try
                {
                    var author = await authorService.FindAuthorByName(username);
                    if (author == null)
                    {
                        return Results.NotFound(new { error = "User not found" });
                    }
                    
                    if (!string.IsNullOrEmpty(request.Follow))
                    {
                        var followAuthor = await authorService.FindAuthorByName(request.Follow);
                        if (followAuthor == null)
                        {
                            return Results.NotFound(new { error = "User to follow not found" });
                        }
                        
                        await authorService.FollowAuthor(username, request.Follow);
                    }
                    
                    if (!string.IsNullOrEmpty(request.Unfollow))
                    {
                        await authorService.UnfollowAuthor(username, request.Unfollow);
                    }
                    
                    return Results.NoContent();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            // GET /fllws/{username} - Update to use async
            simulatorApi.MapGet("/fllws/{username}", async (
                string username,
                [FromQuery] int? no,
                [FromQuery] int? latest,
                AuthorService authorService,
                LatestService latestService) =>
            {
                await latestService.UpdateLatestAsync(latest);
                
                try
                {
                    var followedAuthors = await authorService.GetFollowedAuthors(username);
                    
                    return Results.Ok(new FollowsResponse { Follows = followedAuthors });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            
            // Run the application
            app.Run();
        }
    }
}