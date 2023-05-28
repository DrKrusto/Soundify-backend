using System.ComponentModel.DataAnnotations;

public class SaltModel
{
    [Key]
    public Guid UserId { get; set; }
    public byte[] Salt { get; set; }
}