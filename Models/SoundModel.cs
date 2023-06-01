using Microsoft.EntityFrameworkCore;
using Soundify_backend.Services;

namespace Soundify_backend.Models;

public class SoundModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Guid UploaderId { get; set; }
    public string FileExtension { get; set; }

    public SimplifiedSound ToSimplifiedSound(string url, SoundifyDbContext database) => new SimplifiedSound
    (
        this.Id,
        this.Name,
        database.Users.Single(user => user.Id == this.UploaderId).ToSimplifiedUser(url),
        $"{url}/sounds/{this.Id}{this.FileExtension}"
    );
}

public class SimplifiedSound
{
    public SimplifiedSound(Guid id, string name, SimplifiedUser uploader, string fileUrl)
    {
        Id = id;
        Name = name;
        Uploader = uploader;
        FileUrl = fileUrl;
    }

    public Guid Id { get; set; }
    public string Name { get; set; }
    public SimplifiedUser Uploader { get; set; }
    public string FileUrl { get; set; }
}