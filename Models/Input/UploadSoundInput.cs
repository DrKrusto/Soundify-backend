public class UploadSoundInput
{
    public string Name { get; set; }
    public Guid UploaderId { get; set; }
    public IFormFile Sound { get; set; }
}