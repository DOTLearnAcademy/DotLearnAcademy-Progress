using DotLearn.Progress.Models.Entities;

namespace DotLearn.Progress.Repositories;

public interface IProgressRepository
{
    Task<LessonProgress?> GetByStudentAndLessonAsync(Guid studentId, Guid lessonId);
    Task<List<LessonProgress>> GetByCourseAndStudentAsync(Guid courseId, Guid studentId);
    Task AddAsync(LessonProgress progress);
    Task UpdateAsync(LessonProgress progress);
}
