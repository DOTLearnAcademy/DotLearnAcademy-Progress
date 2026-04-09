namespace DotLearn.Progress.Models.DTOs;

public record TrackProgressRequestDto(
    Guid LessonId,
    Guid CourseId,
    int WatchedSeconds,
    int DurationSeconds
);

public record LessonProgressResponseDto(
    Guid Id,
    Guid StudentId,
    Guid LessonId,
    Guid CourseId,
    int WatchedSeconds,
    int DurationSeconds,
    bool IsCompleted,
    DateTime LastUpdatedAt
);
