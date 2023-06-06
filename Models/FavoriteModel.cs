using System.ComponentModel.DataAnnotations;

namespace Soundify_backend.Models;

public class FavoriteModel
{
    [Key]
    public Guid SoundId { get; set; }
    public Guid UserId { get; set; }
}
