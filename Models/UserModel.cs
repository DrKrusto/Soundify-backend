using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace Soundify_backend.Models;

public class UserModel
{
    [Key]
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string ProfilePictureFileName { get; set; } = "default.jpg";
}