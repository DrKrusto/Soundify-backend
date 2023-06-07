using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Soundify_backend.Models;
using Soundify_backend.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Soundify_backend.Controllers;

[ApiController]
[Route("Api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IPasswordService _passwordService;
    private readonly SoundifyDbContext _dbContext;
    private readonly ISettingsService _settings;

    public UserController(IConfiguration configuration, IPasswordService passwordService, SoundifyDbContext dbContext, ISettingsService settings)
    {
        _configuration = configuration;
        _passwordService = passwordService;
        _dbContext = dbContext;
        _settings = settings;
    }

    [HttpGet]
    [Route("GetUser")]
    public IActionResult GetUser([FromQuery] GetUserInput userInput)
    {
        if (userInput == null)
            return HttpResponseModel.BadRequest("The credentials you provided are empty");

        UserModel? foundUser = null;
        if (userInput.UserId != null)
            foundUser = _dbContext.Users.FirstOrDefault(u => u.Id == userInput.UserId);
        else if (!string.IsNullOrEmpty(userInput.Email))
            foundUser = _dbContext.Users.FirstOrDefault(u => u.Email == userInput.Email);
        else if (!string.IsNullOrEmpty(userInput.Username))
            foundUser = _dbContext.Users.FirstOrDefault(u => u.Username == userInput.Username);

        if (foundUser == null)
            return HttpResponseModel.NotFound("Couldn't find the user");

        return HttpResponseModel.Ok(foundUser.ToSimplifiedUser(_settings.GetUrl()), "User has been found");
    }

    [HttpGet]
    [Route("GetUsers")]
    public IActionResult GetUsers([FromQuery] PagingInput paging)
    {
        var users = _dbContext.Users.ToList();

        if (users.Count == 0)
            return HttpResponseModel.NotFound("Couldn't find any users");

        if (!paging.UsePaging)
        {
            return HttpResponseModel.Ok(users.Select(user => 
                user.ToSimplifiedUser(_settings.GetUrl())), "Users have been found");
        }

        if (paging.Page <= 0 || paging.Size <= 0)
        {
            return HttpResponseModel.BadRequest($"Invalid paging parameters");
        }

        var chunkedUsers = users.Chunk(paging.Size);
        var countOfPages = users.Count();

        if (paging.Page > countOfPages)
        {
            return HttpResponseModel.BadRequest($"Invalid paging parameters: the page number ({paging.Page}) is greater than the count of pages ({countOfPages})");
        }

        return HttpResponseModel.Ok(new
        {
            MaxPages = users.Count(),
            CurrentPage = paging.Page,
            Users = users.ElementAt(paging.Page - 1),
        }, "Users have been found");
    }

    [HttpPost]
    [Route("LoginUser")]
    public IActionResult LoginUser([FromBody] LoginInput login)
    {
        if (login == null)
            return HttpResponseModel.BadRequest("Invalid login parameters");

        if (!ValidateEmail(login.Email))
        {
            return HttpResponseModel.BadRequest("Please provide a valid email to login");
        }

        var user = _dbContext.Users.SingleOrDefault(user => user.Email == login.Email);

        if (user == null)
            return HttpResponseModel.Unauthorized("The email/password is wrong");

        var salt = _dbContext.Salts.SingleOrDefault(salt => salt.UserId == user.Id);

        if (!_passwordService.IsPasswordMatching(user.Id, login.Password, user.HashedPassword))
            return HttpResponseModel.Unauthorized("The email/password is wrong");

        var jwt = GenerateJwtToken();
        return HttpResponseModel.Ok(jwt, "User has been logged in");
    }

    [HttpPost]
    [Route("CreateUser")]
    public IActionResult CreateUser([FromBody] UserCreationInput userCreation)
    {
        if (userCreation == null || 
            string.IsNullOrEmpty(userCreation.Username) ||
            string.IsNullOrEmpty(userCreation.Email) ||
            string.IsNullOrEmpty(userCreation.Password))
            return HttpResponseModel.BadRequest("Invalid input parameters");

        if (!ValidateEmail(userCreation.Email))
        {
            return HttpResponseModel.BadRequest("Please provide a valid email to register your account");
        }

        var existingUser = _dbContext.Users.FirstOrDefault(p => p.Email == userCreation.Email);

        if (existingUser != null)
            return HttpResponseModel.BadRequest("This email is already used");

        Guid userId = Guid.NewGuid();

        if(!_passwordService.TryPushSaltToDatabase(userId, userCreation.Password, out string hashedPassword))
        {
            throw new InvalidOperationException("Failed to push salt to the database hence couldn't hash the password");
        }

        var user = new UserModel
        {
            Id = userId,
            Email = userCreation.Email,
            Username = userCreation.Username,
            HashedPassword = hashedPassword
        };

        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();

        return HttpResponseModel.Ok(user.Id, "User has been created");
    }

    [HttpPost]
    [Route("UploadProfilePicture")]
    public async Task<IActionResult> UploadProfilePicture([FromForm] UserPictureInput userPicture)
    {
        string[] allowedImageTypes = { "image/jpeg", "image/png", "image/jpg" };

        if (userPicture.Image == null || userPicture.Image.Length == 0 || !allowedImageTypes.Contains(userPicture.Image.ContentType))
            return HttpResponseModel.BadRequest("Invalid image file");

        var user = _dbContext.Users.FirstOrDefault(user => user.Id == userPicture.UserId);
        if (user == null)
            return HttpResponseModel.BadRequest("User doesn't exists");

        var fileName = $"{user.Id}{Path.GetExtension(userPicture.Image.FileName).ToLowerInvariant()}";
        var path = Path.Combine("wwwroot/images/users", fileName);

        if (user.ProfilePictureFileName != "default.jpg")
            System.IO.File.Delete($"wwwroot/images/users/{user.ProfilePictureFileName}");

        using (var stream = new FileStream(path, FileMode.Create))
            await userPicture.Image.CopyToAsync(stream);

        user.ProfilePictureFileName = fileName;

        _dbContext.SaveChanges();

        return HttpResponseModel.Ok(user.Id, "Image has been uploaded");
    }

    public static bool ValidateEmail(string email)
    {
        string pattern = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";
        Regex regex = new Regex(pattern);
        return regex.IsMatch(email);
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
