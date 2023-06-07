using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Soundify_backend.Models;

public class HttpResponseModel
{
    static public IActionResult Ok(object value, string details)
    {
        return new OkObjectResult(new { value, details });
    }

    static public IActionResult BadRequest(string details)
    {
        return new BadRequestObjectResult(new { details });
    }

    static public IActionResult NotFound(string details)
    {
        return new NotFoundObjectResult(new { details });
    }

    static public IActionResult Unauthorized(string details)
    {
        return new UnauthorizedObjectResult(new { details });
    }
}