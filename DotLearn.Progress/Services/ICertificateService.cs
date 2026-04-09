using DotLearn.Progress.Models.DTOs;

namespace DotLearn.Progress.Services;

public interface ICertificateService
{
    Task GenerateAndUploadAsync(EnrollmentCompletedEventDto evt);
    Task<List<CertificateResponseDto>> GetMyAsync(Guid studentId);
    Task<CertificateVerifyResponseDto?> VerifyAsync(string code);
    Task<string> GetDownloadUrlAsync(Guid id, Guid studentId);
}
