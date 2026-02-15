using System.Text;

namespace Chirp.Web.Middleware;

/// <summary>
/// Middleware to authenticate simulator requests using Basic Authentication.
/// Only applies to /api/simulator/* endpoints.
/// Regular user endpoints are unaffected.
/// </summary>
public class SimulatorAuthMiddleware
{
    private readonly RequestDelegate _next;
    private const string SimulatorUsername = "simulator";
    private const string SimulatorPassword = "super_safe!";
    
    public SimulatorAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Only check auth for simulator API endpoints
        // This leaves regular user endpoints unaffected
        if (context.Request.Path.StartsWithSegments("/register") ||
            context.Request.Path.StartsWithSegments("/msgs") ||
            context.Request.Path.StartsWithSegments("/fllws") ||
            context.Request.Path.StartsWithSegments("/latest"))
        {
            if (!IsAuthorized(context.Request))
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new 
                { 
                    status = 403, 
                    error_msg = "You are not authorized to use this resource!" 
                });
                return;
            }
        }
        
        await _next(context);
    }
    
    private bool IsAuthorized(HttpRequest request)
    {
        // Check for Authorization header
        if (!request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return false;
        }
        
        var authHeaderValue = authHeader.ToString();
        
        // Check if it's Basic authentication
        if (!authHeaderValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        
        // Extract and decode credentials
        var encodedCredentials = authHeaderValue.Substring("Basic ".Length).Trim();
        
        try
        {
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes);
            var parts = credentials.Split(':', 2);
            
            if (parts.Length != 2)
            {
                return false;
            }
            
            var username = parts[0];
            var password = parts[1];
            
            // Validate credentials
            return username == SimulatorUsername && password == SimulatorPassword;
        }
        catch
        {
            return false;
        }
    }
}
