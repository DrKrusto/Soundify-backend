using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace Soundify_backend.Models;

public class UserModel
{
    [Key]
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string HashedPassword { get; set; }
    public string ProfilePictureFileName { get; set; } = "default.jpg";
}