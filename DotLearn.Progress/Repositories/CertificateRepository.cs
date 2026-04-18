using DotLearn.Progress.Data;
using DotLearn.Progress.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotLearn.Progress.Repositories;

public class CertificateRepository : ICertificateRepository
{
    private readonly CertificateDbContext _context;

    public CertificateRepository(CertificateDbContext context)
    {
        _context = context;
    }

    public async Task<Certificate?> GetByStudentAndCourseAsync(
        Guid studentId, Guid courseId) =>
        await _context.Certificates
            .FirstOrDefaultAsync(c =>
                c.StudentId == studentId && c.CourseId == courseId);

    public async Task<Certificate?> GetByVerificationCodeAsync(string code) =>
        await _context.Certificates
            .FirstOrDefaultAsync(c => c.VerificationCode == code);

    public async Task<Certificate?> GetByIdAsync(Guid id) =>
        await _context.Certificates.FindAsync(id);

    public async Task<List<Certificate>> GetByStudentIdAsync(Guid studentId) =>
        await _context.Certificates
            .Where(c => c.StudentId == studentId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync();

    public async Task AddAsync(Certificate certificate)
    {
        await _context.Certificates.AddAsync(certificate);
        await _context.SaveChangesAsync();
    }
}
