using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Soundify_backend.Models;
using Soundify_backend.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Soundify_backend.Controllers;

[ApiController]
[Route("Api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IPasswordService _passwordService;
    private readonly SoundifyDbContext _dbContext;

    public UserController(IConfiguration configuration, IPasswordService passwordService, SoundifyDbContext dbContext)
    {
        _configuration = configuration;
        _passwordService = passwordService;
        _dbContext = dbContext;
    }

    [HttpGet]
    [Route("GetUser")]
    public IActionResult GetUser([FromQuery] GetUserInput userInput)
    {
        if (userInput == null)
            return BadRequest();

        UserModel? foundUser = null;
        if (userInput.UserId != null)
            foundUser = _dbContext.Users.FirstOrDefault(u => u.Id == userInput.UserId);
        else if (!string.IsNullOrEmpty(userInput.Email))
            foundUser = _dbContext.Users.FirstOrDefault(u => u.Email == userInput.Email);
        else if (!string.IsNullOrEmpty(userInput.Username))
            foundUser = _dbContext.Users.FirstOrDefault(u => u.Username == userInput.Username);

        if (foundUser == null)
            return NotFound();

        return Ok(foundUser);
    }

    [HttpGet]
    [Route("GetUsers")]
    public IActionResult GetUsers([FromQuery] GetUserInput userInput)
    {
        if (userInput == null)
            return BadRequest();

        var query = _dbContext.Users.AsQueryable();

        if (!string.IsNullOrEmpty(userInput.Email))
            query = query.Where(u => u.Email == userInput.Email);

        if (!string.IsNullOrEmpty(userInput.Username))
            query = query.Where(u => u.Username == userInput.Username);

        var foundUsers = query.ToList();

        if (foundUsers.Count == 0)
            return NotFound();

        return Ok(foundUsers);
    }

    [HttpPost]
    [Route("LoginUser")]
    public IActionResult LoginUser([FromBody] LoginInput login)
    {
        if (login == null)
            return BadRequest();

        var user = _dbContext.Users.SingleOrDefault(user => user.Email == login.Email);

        if (user == null)
            return Unauthorized("The email/password is wrong");

        var salt = _dbContext.Salts.SingleOrDefault(salt => salt.UserId == user.Id);

        if (!_passwordService.IsPasswordMatching(user.Id, login.Password, user.HashedPassword))
            return Unauthorized("The email/password is wrong");

        var jwt = GenerateJwtToken();
        return Ok(new { jwt });
    }

    [HttpPost]
    [Route("CreateUser")]
    public IActionResult CreateUser([FromBody] UserCreationInput userCreation)
    {
        if (userCreation == null)
            return BadRequest();

        var existingUser = _dbContext.Users.FirstOrDefault(p => p.Email == userCreation.Email);

        if (existingUser != null)
            return BadRequest("This email is already used");

        Guid userId = Guid.NewGuid();

        if(!_passwordService.TryPushSaltToDatabase(userId, userCreation.Password, out string hashedPassword))
        {
            throw new InvalidOperationException("Failed to push salt to the database hence couldn't hash the password");
        }

        _dbContext.Users.Add(new UserModel { 
            Id = userId, 
            Email = userCreation.Email, 
            Username = userCreation.Username,
            HashedPassword = hashedPassword
        });
        _dbContext.SaveChanges();

        return Ok();
    }

    [HttpPost]
    [Route("UploadProfilePicture")]
    public async Task<IActionResult> UploadProfilePicture([FromForm] UserPictureInput userPicture)
    {
        string[] allowedImageTypes = { "image/jpeg", "image/png", "image/jpg" };

        if (userPicture.Image == null || userPicture.Image.Length == 0 || !allowedImageTypes.Contains(userPicture.Image.ContentType))
            return BadRequest("Invalid image file");

        var user = _dbContext.Users.FirstOrDefault(user => user.Id == userPicture.UserId);
        if (user == null)
            return BadRequest("User doesn't exists");

        var fileName = $"{userPicture.UserId}{Path.GetExtension(userPicture.Image.FileName).ToLowerInvariant()}";
        var path = Path.Combine("wwwroot/images/users", fileName);

        using (var stream = new FileStream(path, FileMode.Create))
            await userPicture.Image.CopyToAsync(stream);

        if (user.ProfilePictureFileName != "default.jpg")
            System.IO.File.Delete($"wwwroot/images/users/{user.ProfilePictureFileName}");

        user.ProfilePictureFileName = fileName;

        _dbContext.SaveChanges();

        return Ok();
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
