using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CXA.Backend.Services;

namespace CXA.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuth0Service _auth0Service;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuth0Service auth0Service, ILogger<AuthController> logger)
    {
        _auth0Service = auth0Service;
        _logger = logger;
    }

    [HttpGet("callback")]
    public IActionResult Callback([FromQuery] string code, [FromQuery] string state)
    {
        // Handle OAuth 2.0 callback
        _logger.LogInformation("OAuth callback received");
        return Ok(new { message = "Callback received" });
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> RefreshToken()
    {
        try
        {
            var newToken = await _auth0Service.RefreshTokenAsync();
            return Ok(new { accessToken = newToken });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return BadRequest(new { error = "Failed to refresh token" });
        }
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var profile = await _auth0Service.GetUserProfileAsync(userId);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile");
            return BadRequest(new { error = "Failed to get profile" });
        }
    }
}






