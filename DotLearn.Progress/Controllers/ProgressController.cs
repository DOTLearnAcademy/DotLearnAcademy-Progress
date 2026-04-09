using DotLearn.Progress.Models.DTOs;
using DotLearn.Progress.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DotLearn.Progress.Controllers;

[ApiController]
public class ProgressController : ControllerBase
{
    private readonly IProgressService _service;

    public ProgressController(IProgressService service)
    {
        _service = service;
    }

    // PUT /api/progress/track
    [HttpPut("api/progress/track")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> TrackProgress([FromBody] TrackProgressRequestDto dto)
    {
        await _service.TrackProgressAsync(dto, GetUserId());
        return Ok(new { message = "Progress updated." });
    }

    // GET /api/progress/lesson/{lessonId}/student/{studentId}
    [HttpGet("api/progress/lesson/{lessonId}/student/{studentId}")]
    [Authorize]
    public async Task<IActionResult> GetLessonProgress(Guid lessonId, Guid studentId)
    {
        var result = await _service.GetLessonProgressAsync(lessonId, studentId);
        if (result == null)
            return Ok(new LessonProgressResponseDto(
                Guid.Empty, studentId, lessonId, Guid.Empty, 0, 0, false, DateTime.UtcNow));
        return Ok(result);
    }

    // GET /api/progress/course/{courseId}/student/{studentId}
    [HttpGet("api/progress/course/{courseId}/student/{studentId}")]
    [Authorize]
    public async Task<IActionResult> GetCourseProgress(Guid courseId, Guid studentId)
    {
        var result = await _service.GetCourseProgressAsync(courseId, studentId);
        return Ok(result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found."));
}
