using Microsoft.AspNetCore.Mvc;
using Soundify_backend.Models;
using Soundify_backend.Models.Input;
using Soundify_backend.Services;
using System.Net.NetworkInformation;
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

    [HttpGet]
    [Route("GetFavorites")]
    public IActionResult GetFavorites([FromQuery] GetFavoritesInput getFavoritesInput, [FromQuery] PagingInput pagingInput)
    {
        if (getFavoritesInput == null)
        {
            return BadRequest("Invalid input parameters");
        }

        var foundUser = _dbContext.Users.FirstOrDefault(user => user.Id == getFavoritesInput.UserId);

        if (foundUser == null)
        {
            return BadRequest($"User {getFavoritesInput.UserId} doesn't exists");
        }

        var favoriteSounds = _dbContext.Favorites
            .Where(favorite => favorite.UserId == foundUser.Id)
            .Select(favorite => _dbContext.Sounds.First(sound => sound.Id == favorite.SoundId));

        if (!pagingInput.UsePaging)
        {
            return Ok(new { Sounds = favoriteSounds
                .Select(sound => sound.ToSimplifiedSound(_settingsService.GetUrl(), _dbContext))
                .ToList() 
            });
        }

        if (pagingInput.Page <= 0 || pagingInput.Size <= 0)
        {
            return BadRequest($"Invalid pagingInput parameters");
        }

        var chunkedSounds = favoriteSounds.ToList().Chunk(pagingInput.Size);
        var countOfPages = chunkedSounds.Count();

        if (pagingInput.Page > countOfPages)
        {
            return BadRequest($"Invalid paging parameters: the page number ({pagingInput.Page}) is greater than the count of pages ({countOfPages})");
        }

        return Ok(new
        {
            MaxPages = chunkedSounds.Count(),
            CurrentPage = pagingInput.Page,
            Sounds = chunkedSounds.ElementAt(pagingInput.Page - 1)
            .Select(sound => sound.ToSimplifiedSound(_settingsService.GetUrl(), _dbContext)),
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

    [HttpPost]
    [Route("AddToFavorites")]
    public IActionResult AddToFavorites([FromBody] AddToFavoritesInput addToFavoritesInput)
    {
        if (addToFavoritesInput == null)
        {
            return BadRequest("Invalid input parameters");
        }

        var foundFavorite = _dbContext
            .Favorites
            .FirstOrDefault(favorite =>
                favorite.UserId == addToFavoritesInput.UserId &&
                favorite.SoundId == addToFavoritesInput.SoundId);
        if (foundFavorite != null)
        {
            return BadRequest($"User {addToFavoritesInput.UserId} already have the sound {addToFavoritesInput.SoundId} in its favorites");
        }

        var foundUser = _dbContext.Users.FirstOrDefault(user => user.Id == addToFavoritesInput.UserId);
        if (foundUser == null)
        {
            return BadRequest($"User {addToFavoritesInput.UserId} doesn't exists");
        }

        var foundSound = _dbContext.Sounds.FirstOrDefault(sound => sound.Id == addToFavoritesInput.SoundId);
        if (foundSound == null)
        {
            return BadRequest($"Sound {addToFavoritesInput.SoundId} doesn't exists");
        }

        _dbContext.Favorites.Add(new FavoriteModel { 
            SoundId = foundSound.Id, 
            UserId = foundUser.Id 
        });
        _dbContext.SaveChanges();

        return Ok();
    }

    [HttpDelete]
    [Route("DeleteFromFavorites")]
    public IActionResult DeleteFromFavorites([FromBody] AddToFavoritesInput addToFavoritesInput)
    {
        if (addToFavoritesInput == null)
        {
            return BadRequest("Invalid input parameters");
        }

        var foundFavorite = _dbContext
            .Favorites
            .FirstOrDefault(favorite => 
                favorite.UserId == addToFavoritesInput.UserId && 
                favorite.SoundId == addToFavoritesInput.SoundId);
        if (foundFavorite == null)
        {
            return BadRequest($"User {addToFavoritesInput.UserId} do not have the sound {addToFavoritesInput.SoundId} in its favorites");
        }

        _dbContext.Favorites.Remove(foundFavorite);
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