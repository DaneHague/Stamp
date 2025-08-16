/* Temporarily disabled for Identity implementation
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using StampApi.Data;
using StampApi.Models;

namespace StampApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly StampDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(StampDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("google")]
    public async Task<ActionResult<object>> GoogleAuth([FromBody] GoogleAuthRequest request)
    {
        try
        {
            // In a real implementation, you would verify the Google token here
            // For now, we'll trust the client-side verification
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == request.GoogleId);
            
            if (user == null)
            {
                // Create new user
                user = new User
                {
                    GoogleId = request.GoogleId,
                    Email = request.Email,
                    Name = request.Name,
                    AvatarUrl = request.AvatarUrl,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"Created new user: {user.Email}");
            }
            else
            {
                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                user.Name = request.Name; // Update name in case it changed
                user.AvatarUrl = request.AvatarUrl; // Update avatar in case it changed
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"User logged in: {user.Email}");
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                Token = token,
                User = new
                {
                    user.Id,
                    user.Email,
                    user.Name,
                    user.AvatarUrl
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Auth error: {ex.Message}");
            return BadRequest("Authentication failed");
        }
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Authentication:Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim("GoogleId", user.GoogleId)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(int.Parse(jwtSettings["ExpireHours"]!)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class GoogleAuthRequest
{
    public string GoogleId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
*/