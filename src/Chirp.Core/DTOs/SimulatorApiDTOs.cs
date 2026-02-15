namespace Chirp.Core.DTOs;

// Request DTOs
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Pwd { get; set; } = string.Empty;
}

public class MessageRequest
{
    public string Content { get; set; } = string.Empty;
}

public class FollowRequest
{
    public string? Follow { get; set; }
    public string? Unfollow { get; set; }
}

// Response DTOs
public class LatestResponse
{
    public int Latest { get; set; }
}

public class MessageResponse
{
    public string Content { get; set; } = string.Empty;
    public string Pub_date { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
}

public class FollowsResponse
{
    public List<string> Follows { get; set; } = new();
}

/// <summary>
/// Error response following the OpenAPI specification
/// </summary>
public class ErrorResponse
{
    public int Status { get; set; }
    public string ErrorMsg { get; set; } = string.Empty;
}