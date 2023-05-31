namespace Soundify_backend.Models;

public class SoundModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Guid UploaderId { get; set; }
    public string FileExtension { get; set; }
}