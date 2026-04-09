using DotLearn.Progress.Data;
using DotLearn.Progress.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotLearn.Progress.Repositories;

public class ProgressRepository : IProgressRepository
{
    private readonly ProgressDbContext _context;

    public ProgressRepository(ProgressDbContext context)
    {
        _context = context;
    }

    public async Task<LessonProgress?> GetByStudentAndLessonAsync(
        Guid studentId, Guid lessonId) =>
        await _context.LessonProgresses
            .FirstOrDefaultAsync(p =>
                p.StudentId == studentId && p.LessonId == lessonId);

    public async Task<List<LessonProgress>> GetByCourseAndStudentAsync(
        Guid courseId, Guid studentId) =>
        await _context.LessonProgresses
            .Where(p => p.CourseId == courseId && p.StudentId == studentId)
            .OrderBy(p => p.LessonId)
            .ToListAsync();

    public async Task AddAsync(LessonProgress progress)
    {
        await _context.LessonProgresses.AddAsync(progress);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(LessonProgress progress)
    {
        _context.LessonProgresses.Update(progress);
        await _context.SaveChangesAsync();
    }
}
