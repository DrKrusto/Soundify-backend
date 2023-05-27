using Microsoft.AspNetCore.Mvc;

namespace Soundify_backend.Controllers;

[ApiController]
[Route("[controller]")]
public class SoundifyController : ControllerBase
{
    private readonly ILogger<SoundifyController> _logger;

    public SoundifyController(ILogger<SoundifyController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "ConnectUser")]
    public void ConnectUser()
    {
        
    }
}
