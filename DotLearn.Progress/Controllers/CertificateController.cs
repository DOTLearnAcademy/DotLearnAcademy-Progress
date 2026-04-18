using DotLearn.Progress.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DotLearn.Progress.Controllers;

[ApiController]
public class CertificateController : ControllerBase
{
    private readonly ICertificateService _service;

    public CertificateController(ICertificateService service)
    {
        _service = service;
    }

    // GET /api/certificates/my
    [HttpGet("api/certificates/my")]
    [Authorize]
    public async Task<IActionResult> GetMy()
    {
        var result = await _service.GetMyAsync(GetUserId());
        return Ok(result);
    }

    // GET /api/certificates/verify/{code}
    [HttpGet("api/certificates/verify/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify(string code)
    {
        var result = await _service.VerifyAsync(code);
        if (result == null)
            return NotFound(new { error = "Invalid verification code." });
        return Ok(result);
    }

    // GET /api/certificates/{id}/download
    [HttpGet("api/certificates/{id}/download")]
    [Authorize]
    public async Task<IActionResult> Download(Guid id)
    {
        try
        {
            var url = await _service.GetDownloadUrlAsync(id, GetUserId());
            return Ok(new { downloadUrl = url });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found."));
}
