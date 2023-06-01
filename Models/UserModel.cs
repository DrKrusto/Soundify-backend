using Microsoft.EntityFrameworkCore;
using Soundify_backend.Services;
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

    public SimplifiedUser ToSimplifiedUser(string url)
    {
        return new SimplifiedUser
        (
            this.Id,
            this.Username,
            this.Email,
            $"{url}/images/users/{this.ProfilePictureFileName}"
        );
    }
}

public class SimplifiedUser
{
    public SimplifiedUser(Guid id, string username, string email, string pictureUrl) 
    {
        this.Id = id;
        this.Username = username;
        this.Email = email;
        this.PictureUrl = pictureUrl;
    }

    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PictureUrl { get; set; }
}