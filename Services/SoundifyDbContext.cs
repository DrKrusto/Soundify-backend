using Microsoft.EntityFrameworkCore;
using Soundify_backend.Models;

namespace Soundify_backend.Services;

public class SoundifyDbContext : DbContext
{
    public DbSet<UserModel> Users { get; set; }
    public DbSet<SaltModel> Salts { get; set; }
    public DbSet<SoundModel> Sounds { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=soundify.db");
    }
}