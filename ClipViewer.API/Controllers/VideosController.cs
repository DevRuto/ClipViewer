using ClipViewer.API.Data;
using Microsoft.AspNetCore.Mvc;

namespace ClipViewer.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VideosController(
    ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(context.VideoClips.ToList());
    }
}