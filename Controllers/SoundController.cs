using Microsoft.AspNetCore.Mvc;
using Soundify_backend.Models;
using Soundify_backend.Services;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Soundify_backend.Controllers;

[ApiController]
[Route("Api/[controller]")]
public class SoundController : ControllerBase
{
    private readonly SoundifyDbContext _dbContext;
    private readonly ISettingsService _settingsService;

    public SoundController(SoundifyDbContext dbContext, ISettingsService settings)
    {
        _dbContext = dbContext;
        _settingsService = settings;
    }

    [HttpGet]
    [Route("GetSound")]
    public IActionResult GetSound([FromQuery] GetSoundInput soundInput)
    {
        if (soundInput == null || (soundInput.Id == null && string.IsNullOrEmpty(soundInput.Name)))
        {
            return BadRequest("Query is empty");
        }

        var soundFound = QueryDbForSounds(soundInput).FirstOrDefault();

        if (soundFound == null)
        {
            return NotFound();
        }

        return Ok(soundFound.ToSimplifiedSound(_settingsService.GetUrl(), _dbContext));
    }

    [HttpGet]
    [Route("GetSounds")]
    public IActionResult GetSounds([FromQuery] PagingInput paging)
    {
        var soundsFound = _dbContext.Sounds.ToList();

        if (soundsFound == null || soundsFound.Count() <= 0)
        {
            return NotFound();
        }

        var sounds = soundsFound
            .Select(sound => sound.ToSimplifiedSound(_settingsService.GetUrl(), _dbContext))
            .ToList();

        if (!paging.UsePaging)
        {
            return Ok(new { Sounds = sounds });
        }

        if (paging.Page <= 0 || paging.Size <= 0)
        {
            return BadRequest($"Invalid paging parameters");
        }

        var chunkedSounds = sounds.Chunk(paging.Size);
        var countOfPages = chunkedSounds.Count();

        if (paging.Page > countOfPages)
        {
            return BadRequest($"Invalid paging parameters: the page number ({paging.Page}) is greater than the count of pages ({countOfPages})");
        }

        return Ok(new
        {
            MaxPages = chunkedSounds.Count(),
            Sounds = chunkedSounds.ElementAt(paging.Page - 1),
        });
    }

    [HttpGet]
    [Route("SearchSounds")]
    public IActionResult SearchSounds([FromQuery] string SearchByName = "", [FromQuery] PagingInput? paging = null)
    {
        var soundsFound = _dbContext.Sounds.ToList().Where(
            sound => sound.Name.ToLowerInvariant().Contains(SearchByName.ToLowerInvariant())
        );

        if (soundsFound == null || soundsFound.Count() <= 0)
        {
            return NotFound();
        }

        var sounds = soundsFound
            .Select(sound => sound.ToSimplifiedSound(_settingsService.GetUrl(), _dbContext))
            .ToList();

        if (!paging.UsePaging)
        {
            return Ok(new { Sounds = sounds });
        }

        if (paging.Page <= 0 || paging.Size <= 0)
        {
            return BadRequest($"Invalid paging parameters");
        }

        var chunkedSounds = sounds.Chunk(paging.Size);
        var countOfPages = chunkedSounds.Count();

        if (paging.Page > countOfPages)
        {
            return BadRequest($"Invalid paging parameters: the page number ({paging.Page}) is greater than the count of pages ({countOfPages})");
        }

        return Ok(new
        {
            MaxPages = chunkedSounds.Count(),
            CurrentPage = paging.Page,
            Sounds = chunkedSounds.ElementAt(paging.Page - 1),
        });
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

    private IQueryable<SoundModel> QueryDbForSounds(GetSoundInput soundInput)
    {
        var query = _dbContext.Sounds.AsQueryable();
        if (soundInput.Id != null)
            query = query.Where(sound => sound.Id == soundInput.Id);
        if (!string.IsNullOrEmpty(soundInput.Name))
            query = query.Where(sound => sound.Name == soundInput.Name);
        return query;
    } 
}