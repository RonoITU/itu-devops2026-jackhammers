using System.Globalization;
using Chirp.Core.DTOs;
using Chirp.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Chirp.Web.Controllers;

/// <summary>
/// Controller for the Simulator API endpoints.
/// Handles all requests from the MiniTwit simulator.
/// These endpoints are separate from the regular user-facing UI.
/// </summary>
[ApiController]
[Route("")]
public class SimulatorController : ControllerBase
{
    private readonly LatestService _latestService;
    private readonly AuthorService _authorService;
    private readonly CheepService _cheepService;
    
    public SimulatorController(
        LatestService latestService,
        AuthorService authorService,
        CheepService cheepService)
    {
        _latestService = latestService;
        _authorService = authorService;
        _cheepService = cheepService;
    }
    
    /// <summary>
    /// GET /latest - Returns the latest processed command ID
    /// </summary>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(LatestResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> GetLatest()
    {
        try
        {
            var latest = await _latestService.GetLatestAsync();
            return Ok(new LatestResponse { Latest = latest });
        }
        catch (Exception)
        {
            return StatusCode(500, new ErrorResponse 
            { 
                Status = 500, 
                ErrorMsg = "Internal server error" 
            });
        }
    }
    
    /// <summary>
    /// POST /register - Register a new user
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        [FromQuery] int? latest)
    {
        await _latestService.UpdateLatestAsync(latest);
        
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Status = 400, 
                    ErrorMsg = "Username is required" 
                });
            }
            
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Status = 400, 
                    ErrorMsg = "Email is required" 
                });
            }
            
            if (string.IsNullOrWhiteSpace(request.Pwd))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Status = 400, 
                    ErrorMsg = "Password is required" 
                });
            }
            
            // Check if user already exists
            var existingAuthor = await _authorService.FindAuthorByName(request.Username);
            if (existingAuthor != null)
            {
                return BadRequest(new ErrorResponse 
                { 
                    Status = 400, 
                    ErrorMsg = "Username already taken" 
                });
            }
            
            // Create the author
            await _authorService.CreateAuthor(request.Username, request.Email);
            
            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(500, new ErrorResponse 
            { 
                Status = 500, 
                ErrorMsg = "Internal server error" 
            });
        }
    }
    
    /// <summary>
    /// POST /msgs/{username} - Post a new message
    /// </summary>
    [HttpPost("msgs/{username}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PostMessage(
        string username,
        [FromBody] MessageRequest request,
        [FromQuery] int? latest)
    {
        await _latestService.UpdateLatestAsync(latest);
        
        try
        {
            // Check if user exists
            var author = await _authorService.FindAuthorByName(username);
            if (author == null)
            {
                return NotFound();
            }
            
            // Validate message content
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Status = 400, 
                    ErrorMsg = "Message content is required" 
                });
            }
            
            if (request.Content.Length > 160)
            {
                return BadRequest(new ErrorResponse 
                { 
                    Status = 400, 
                    ErrorMsg = "Message content must be 160 characters or less" 
                });
            }
            
            // Create the cheep
            var cheepDto = new CheepDTO
            {
                Author = author,
                Text = request.Content,
                ImageReference = null,
                FormattedTimeStamp = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
            };
            
            await _cheepService.CreateCheep(cheepDto);
            
            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(500, new ErrorResponse 
            { 
                Status = 500, 
                ErrorMsg = "Internal server error" 
            });
        }
    }
    
    /// <summary>
    /// GET /msgs/{username} - Get messages from a specific user
    /// </summary>
    [HttpGet("msgs/{username}")]
    [ProducesResponseType(typeof(List<MessageResponse>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUserMessages(
        string username,
        [FromQuery] int? no,
        [FromQuery] int? latest)
    {
        await _latestService.UpdateLatestAsync(latest);
        
        try
        {
            int count = no ?? 100;
            
            var cheeps = await _cheepService.GetMessagesForSimulator(username, count);
            
            var messages = cheeps.Select(c => new MessageResponse
            {
                Content = c.Text,
                Pub_date = c.FormattedTimeStamp,
                User = c.Author.Name
            }).ToList();
            
            return Ok(messages);
        }
        catch (Exception)
        {
            return StatusCode(500, new ErrorResponse 
            { 
                Status = 500, 
                ErrorMsg = "Internal server error" 
            });
        }
    }
    
    /// <summary>
    /// GET /msgs - Get all public messages
    /// </summary>
    [HttpGet("msgs")]
    [ProducesResponseType(typeof(List<MessageResponse>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    public async Task<IActionResult> GetPublicMessages(
        [FromQuery] int? no,
        [FromQuery] int? latest)
    {
        await _latestService.UpdateLatestAsync(latest);
        
        try
        {
            int count = no ?? 100;
            
            var cheeps = await _cheepService.GetMessagesForSimulator(null, count);
            
            var messages = cheeps.Select(c => new MessageResponse
            {
                Content = c.Text,
                Pub_date = c.FormattedTimeStamp,
                User = c.Author.Name
            }).ToList();
            
            return Ok(messages);
        }
        catch (Exception)
        {
            return StatusCode(500, new ErrorResponse 
            { 
                Status = 500, 
                ErrorMsg = "Internal server error" 
            });
        }
    }
    
    /// <summary>
    /// POST /fllws/{username} - Follow or unfollow a user
    /// </summary>
    [HttpPost("fllws/{username}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> FollowUser(
        string username,
        [FromBody] FollowRequest request,
        [FromQuery] int? latest)
    {
        await _latestService.UpdateLatestAsync(latest);
        
        try
        {
            // Check if user exists
            var author = await _authorService.FindAuthorByName(username);
            if (author == null)
            {
                return NotFound();
            }
            
            // Handle follow
            if (!string.IsNullOrEmpty(request.Follow))
            {
                var followAuthor = await _authorService.FindAuthorByName(request.Follow);
                if (followAuthor == null)
                {
                    return NotFound();
                }
                
                await _authorService.FollowAuthor(username, request.Follow);
            }
            
            // Handle unfollow
            if (!string.IsNullOrEmpty(request.Unfollow))
            {
                await _authorService.UnfollowAuthor(username, request.Unfollow);
            }
            
            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(500, new ErrorResponse 
            { 
                Status = 500, 
                ErrorMsg = "Internal server error" 
            });
        }
    }
    
    /// <summary>
    /// GET /fllws/{username} - Get users that this user follows
    /// </summary>
    [HttpGet("fllws/{username}")]
    [ProducesResponseType(typeof(FollowsResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetFollows(
        string username,
        [FromQuery] int? no,
        [FromQuery] int? latest)
    {
        await _latestService.UpdateLatestAsync(latest);
        
        try
        {
            var followedAuthors = await _authorService.GetFollowedAuthors(username);
            
            return Ok(new FollowsResponse { Follows = followedAuthors });
        }
        catch (Exception)
        {
            return StatusCode(500, new ErrorResponse 
            { 
                Status = 500, 
                ErrorMsg = "Internal server error" 
            });
        }
    }
}
