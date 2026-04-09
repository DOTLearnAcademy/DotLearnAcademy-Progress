using DotLearn.Progress.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotLearn.Progress.Data;

public class ProgressDbContext : DbContext
{
    public ProgressDbContext(DbContextOptions<ProgressDbContext> options)
        : base(options) { }

    public DbSet<LessonProgress> LessonProgresses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LessonProgress>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.StudentId, x.LessonId }).IsUnique();
        });
    }
}
