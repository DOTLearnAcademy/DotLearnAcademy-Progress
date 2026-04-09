using DotLearn.Progress.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotLearn.Progress.Data;

public class CertificateDbContext : DbContext
{
    public CertificateDbContext(DbContextOptions<CertificateDbContext> options)
        : base(options) { }

    public DbSet<Certificate> Certificates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Certificate>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.VerificationCode).IsUnique();
            e.HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();
        });
    }
}
