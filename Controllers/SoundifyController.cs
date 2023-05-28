using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Soundify_backend.Models;
using Soundify_backend.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Soundify_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SoundifyController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly SoundifyDbContext _dbContext;

    public SoundifyController(IConfiguration configuration, SoundifyDbContext dbContext)
    {
        _configuration = configuration;
        _dbContext = dbContext;
    }

    [HttpPost]
    [Route("user/LoginUser")]
    public IActionResult LoginUser([FromBody] LoginModel login)
    {
        if (login == null)
            return BadRequest();

        var user = _dbContext.Users.FirstOrDefault(p => p.Email == login.Email && p.Password == login.Password);

        if (user == null)
        {
            return Unauthorized("This email and/or this password are wrong");
        }

        var jwt = GenerateJwtToken();
        return Ok(new { jwt });
    }

    [HttpPost]
    [Route("user/CreateUser")]
    public IActionResult CreateUser([FromBody] UserModel user)
    {
        if (user == null)
            return BadRequest();

        var existingUser = _dbContext.Users.FirstOrDefault(p => p.Email == user.Email);

        if (existingUser != null)
        {
            return BadRequest("This email is already used");
        }

        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();
        return Ok();
    }

    [HttpPost]
    [Route("user/UploadProfilePicture")]
    public async Task<IActionResult> UploadProfilePicture([FromForm] UserPictureModel userPicture)
    {
        string[] allowedImageTypes = { "image/jpeg", "image/png", "image/jpg" };

        if (userPicture.Image == null || userPicture.Image.Length == 0 || !allowedImageTypes.Contains(userPicture.Image.ContentType))
            return BadRequest("Invalid image file");

        var user = _dbContext.Users.FirstOrDefault(user => user.Id == userPicture.UserId);
        if (user == null)
            return BadRequest("User doesn't exists");

        var fileName = $"{Guid.NewGuid().ToString()}_{userPicture.UserId}{Path.GetExtension(userPicture.Image.FileName).ToLowerInvariant()}";
        var path = Path.Combine("wwwroot/images/users", fileName);

        using (var stream = new FileStream(path, FileMode.Create))
            await userPicture.Image.CopyToAsync(stream);

        if (user.ProfilePictureFileName != "default.jpg")
            System.IO.File.Delete($"wwwroot/images/users/{user.ProfilePictureFileName}");

        user.ProfilePictureFileName = fileName;

        _dbContext.SaveChanges();

        return Ok();
    }

    [HttpGet]
    [Route("user/GetUser")]
    public IActionResult GetUser([FromQuery] UserModel user)
    {
        if (user == null)
            return BadRequest();

        UserModel? foundUser = null;
        if (user.Id > 0)
            foundUser = _dbContext.Users.FirstOrDefault(u => u.Id == user.Id);
        else if (!string.IsNullOrEmpty(user.Email))
            foundUser = _dbContext.Users.FirstOrDefault(u => u.Email == user.Email);
        else if (!string.IsNullOrEmpty(user.Username))
            foundUser = _dbContext.Users.FirstOrDefault(u => u.Username == user.Username);

        if (foundUser == null)
            return NotFound();

        return Ok(new { foundUser });
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
