using DotLearn.Progress.Models.Entities;

namespace DotLearn.Progress.Repositories;

public interface ICertificateRepository
{
    Task<Certificate?> GetByStudentAndCourseAsync(Guid studentId, Guid courseId);
    Task<Certificate?> GetByVerificationCodeAsync(string code);
    Task<Certificate?> GetByIdAsync(Guid id);
    Task<List<Certificate>> GetByStudentIdAsync(Guid studentId);
    Task AddAsync(Certificate certificate);
}
