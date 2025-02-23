namespace WorkApiAuth.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;

public class LoginRequest
{
    public string User { get; set; } = default!;
}

public class LoginResponse
{
    public string Token { get; set; } = default!;
}

public class ExecuteResponse
{
    public string Message { get; set; } = default!;
}

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    [HttpPost]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, request.User),
            new(ClaimTypes.Role, "Administrator")
        };
        var identify = new ClaimsIdentity(claims, "custom");

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = identify,
            Expires = DateTime.UtcNow.AddDays(AuthenticationSettings.ExpireDays),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AuthenticationSettings.SecretKey)), SecurityAlgorithms.HmacSha256Signature),
            Audience = AuthenticationSettings.Audience,
            Issuer = AuthenticationSettings.Issuer
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Ok(new LoginResponse
        {
            Token = tokenHandler.WriteToken(token)
        });
    }

    [HttpGet]
    [Authorize]
    public IActionResult Execute()
    {
        return Ok(new ExecuteResponse
        {
            Message = $"ok: {User.Identity?.Name}"
        });
    }
}
