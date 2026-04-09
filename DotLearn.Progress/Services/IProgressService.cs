using DotLearn.Progress.Models.DTOs;

namespace DotLearn.Progress.Services;

public interface IProgressService
{
    Task TrackProgressAsync(TrackProgressRequestDto dto, Guid studentId);
    Task<LessonProgressResponseDto?> GetLessonProgressAsync(Guid lessonId, Guid studentId);
    Task<List<LessonProgressResponseDto>> GetCourseProgressAsync(Guid courseId, Guid studentId);
}
