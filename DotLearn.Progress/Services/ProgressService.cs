using Amazon.SQS;
using Amazon.SQS.Model;
using DotLearn.Progress.Models.DTOs;
using DotLearn.Progress.Models.Entities;
using DotLearn.Progress.Repositories;
using System.Text.Json;

namespace DotLearn.Progress.Services;

public class ProgressService : IProgressService
{
    private readonly IProgressRepository _repo;
    private readonly IAmazonSQS _sqsClient;
    private readonly IConfiguration _config;
    private readonly ILogger<ProgressService> _logger;

    public ProgressService(
        IProgressRepository repo,
        IAmazonSQS sqsClient,
        IConfiguration config,
        ILogger<ProgressService> logger)
    {
        _repo = repo;
        _sqsClient = sqsClient;
        _config = config;
        _logger = logger;
    }

    public async Task TrackProgressAsync(TrackProgressRequestDto dto, Guid studentId)
    {
        var existing = await _repo.GetByStudentAndLessonAsync(studentId, dto.LessonId);

        if (existing == null)
        {
            existing = new LessonProgress
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                LessonId = dto.LessonId,
                CourseId = dto.CourseId,
                DurationSeconds = dto.DurationSeconds,
                WatchedSeconds = 0
            };
            await _repo.AddAsync(existing);
        }

        // Never go backwards
        if (dto.WatchedSeconds > existing.WatchedSeconds)
            existing.WatchedSeconds = dto.WatchedSeconds;

        // Always update duration in case it changes
        if (dto.DurationSeconds > 0)
            existing.DurationSeconds = dto.DurationSeconds;

        existing.LastUpdatedAt = DateTime.UtcNow;

        bool wasCompleted = existing.IsCompleted;

        // 85% threshold
        existing.IsCompleted = existing.DurationSeconds > 0 &&
            existing.WatchedSeconds >= existing.DurationSeconds * 0.85;

        await _repo.UpdateAsync(existing);

        // Publish LessonCompleted exactly once — only on first completion
        if (!wasCompleted && existing.IsCompleted)
        {
            await PublishLessonCompletedAsync(existing);
        }
    }

    public async Task<LessonProgressResponseDto?> GetLessonProgressAsync(
        Guid lessonId, Guid studentId)
    {
        var progress = await _repo.GetByStudentAndLessonAsync(studentId, lessonId);
        return progress == null ? null : MapToDto(progress);
    }

    public async Task<List<LessonProgressResponseDto>> GetCourseProgressAsync(
        Guid courseId, Guid studentId)
    {
        var records = await _repo.GetByCourseAndStudentAsync(courseId, studentId);
        return records.Select(MapToDto).ToList();
    }

    private async Task PublishLessonCompletedAsync(LessonProgress progress)
    {
        try
        {
            var message = JsonSerializer.Serialize(new
            {
                EventType = "LessonCompleted",
                progress.StudentId,
                progress.LessonId,
                progress.CourseId,
                Timestamp = DateTime.UtcNow
            });

            await _sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = _config["SQS:LessonCompletedQueue"],
                MessageBody = message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish LessonCompleted for lesson {LessonId}", progress.LessonId);
        }
    }

    private static LessonProgressResponseDto MapToDto(LessonProgress p) => new(
        p.Id, p.StudentId, p.LessonId, p.CourseId,
        p.WatchedSeconds, p.DurationSeconds, p.IsCompleted, p.LastUpdatedAt);
}
