using Soundify_backend.Services;
using System.Security.Cryptography;

public interface IPasswordService
{
    bool TryPushSaltToDatabase(Guid userId, string password, out string hashedPassword);
    bool IsPasswordMatching(Guid userId, string password, string hashedPassword);

    static internal byte[] GenerateSalt()
    {
        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        return salt;
    }

    static internal string HashPassword(string password, byte[] salt)
    {
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
        {
            byte[] hash = pbkdf2.GetBytes(32);
            return Convert.ToBase64String(hash);
        }
    }
}

public class PasswordService : IPasswordService
{
    private readonly SoundifyDbContext _dbContext;

    public PasswordService(SoundifyDbContext dbContext) 
    { 
        _dbContext = dbContext;
    }

    public bool IsPasswordMatching(Guid userId, string password, string hashedPassword)
    {
        var salt = _dbContext.Salts.SingleOrDefault(salt => salt.UserId == userId);

        if (salt == null)
            throw new ArgumentNullException(userId.ToString(), "No salt found for userId");

        return IPasswordService.HashPassword(password, salt.Salt) == hashedPassword;
    }

    public bool TryPushSaltToDatabase(Guid userId, string password, out string hashedPassword)
    {
        byte[] salt = IPasswordService.GenerateSalt();
        hashedPassword = IPasswordService.HashPassword(password, salt);

        try
        {
            _dbContext.Salts.Add(new SaltModel { UserId = userId, Salt = salt });
            _dbContext.SaveChanges();
        }
        catch
        {
            // not clean
            return false;
        }

        return true;
    }
}