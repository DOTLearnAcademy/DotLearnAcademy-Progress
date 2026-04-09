using System.Net.Http.Json;

namespace DotLearn.Progress.Services;

/// <summary>
/// Fetches course/student names from internal service APIs
/// since they are not included in the SQS event payload.
/// </summary>
public class InternalHttpService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<InternalHttpService> _logger;

    public InternalHttpService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<InternalHttpService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<string> GetCourseNameAsync(Guid courseId)
    {
        try
        {
            var url = $"{_config["Services:CourseService"]}/internal/courses/{courseId}";
            var response = await _httpClient.GetFromJsonAsync<CourseInternalDto>(url);
            return response?.Title ?? "Unknown Course";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch course name for {CourseId}", courseId);
            return "Unknown Course";
        }
    }

    public async Task<string> GetStudentNameAsync(Guid studentId)
    {
        try
        {
            var url = $"{_config["Services:AuthService"]}/internal/users/{studentId}";
            var response = await _httpClient.GetFromJsonAsync<UserInternalDto>(url);
            return response?.FullName ?? "Unknown Student";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch student name for {StudentId}", studentId);
            return "Unknown Student";
        }
    }
}

public record CourseInternalDto(Guid Id, string Title);
public record UserInternalDto(Guid Id, string FullName);
