using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Soundify_backend.Controllers;

[ApiController]
[Route("[controller]")]
public class SoundifyController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public SoundifyController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet(Name = "Login")]
    public IActionResult Login()
    {
        // TODO
        var jwt = GenerateJwtToken();
        return Ok(new { jwt });
    }

    private string GenerateJwtToken()
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                    new Claim(ClaimTypes.Name, "username"),
                }),
            Expires = DateTime.UtcNow.AddHours(Convert.ToInt32(jwtSettings["ExpirationHours"])),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
