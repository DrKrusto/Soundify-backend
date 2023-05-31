using Microsoft.AspNetCore.Mvc;
using Soundify_backend.Models;
using Soundify_backend.Services;

namespace Soundify_backend.Controllers;

[ApiController]
[Route("Api/[controller]")]
public class SoundController : ControllerBase
{
    private readonly SoundifyDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public SoundController(SoundifyDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    [HttpGet]
    [Route("GetSound")]
    public IActionResult GetSound([FromQuery] GetSoundInput soundInput)
    {
        if (soundInput == null || (soundInput.Id == null && string.IsNullOrEmpty(soundInput.Name)))
        {
            return BadRequest("Query is empty");
        }

        var query = _dbContext.Sounds.AsQueryable();

        if (soundInput.Id != null) 
            query = query.Where(sound => sound.Id == soundInput.Id);

        if (!string.IsNullOrEmpty(soundInput.Name)) 
            query = query.Where(sound => sound.Name == soundInput.Name);

        var soundFound = query.FirstOrDefault();

        if (soundFound != null)
        {
            var url = _configuration.GetValue<string>("Url");

            return Ok(new
            {
                Id = soundFound.Id,
                Name = soundFound.Name,
                Uploader = _dbContext.Users.FirstOrDefault(user => user.Id == soundFound.UploaderId),
                FileUrl = $"{url}/sounds/{soundFound.Id}{soundFound.FileExtension}"
            });
        }

        return NotFound();
    }

    [HttpPost]
    [Route("UploadSound")]
    public async Task<IActionResult> UploadSound([FromForm] UploadSoundInput soundInput)
    {
        string[] allowSoundExtensions = { "audio/mpeg", "audio/vnd.wav" };

        if (soundInput.Sound == null || soundInput.Sound.Length == 0 || !allowSoundExtensions.Contains(soundInput.Sound.ContentType))
            return BadRequest("Invalid sound file");

        if (_dbContext.Users.FirstOrDefault(user => user.Id == soundInput.UploaderId) == null)
            return BadRequest("Uploader doesn't exists");

        SoundModel sound = new()
        {
            Id = Guid.NewGuid(),
            Name = soundInput.Name,
            UploaderId = soundInput.UploaderId,
            FileExtension = Path.GetExtension(soundInput.Sound.FileName).ToLowerInvariant()
        };

        var fileName = $"{sound.Id}{sound.FileExtension}";
        var path = Path.Combine("wwwroot/sounds", fileName);

        using (var stream = new FileStream(path, FileMode.Create))
            await soundInput.Sound.CopyToAsync(stream);

        _dbContext.Sounds.Add(sound);
        _dbContext.SaveChanges();

        return Ok();
    }
}