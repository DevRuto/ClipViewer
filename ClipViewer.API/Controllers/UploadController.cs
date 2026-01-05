using ClipViewer.API.Interfaces;
using ClipViewer.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClipViewer.API.Controllers;

[Route("api/[controller]")]
public class UploadController(
    IVideoJobQueue videoJobQueue) : ControllerBase
{
    // GET
    public IActionResult Index()
    {
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Upload()
    {
        var jobId = Guid.NewGuid();

        await videoJobQueue.EnqueueAsync(new VideoConversionJob("input", "output", jobId));

        return Accepted(new { jobId });
    }
}