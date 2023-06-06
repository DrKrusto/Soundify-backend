namespace Soundify_backend.Models.Input;

public class AddToFavoritesInput
{
    public Guid UserId { get; set; }
    public Guid SoundId { get; set; }
}
