namespace DotLearn.Progress.Models.Entities;

public class LessonProgress
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid LessonId { get; set; }
    public Guid CourseId { get; set; }
    public int WatchedSeconds { get; set; } = 0;
    public int DurationSeconds { get; set; } = 0;
    public bool IsCompleted { get; set; } = false;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
