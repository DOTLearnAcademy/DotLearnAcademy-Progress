using Amazon.S3;
using Amazon.S3.Model;
using DotLearn.Progress.Models.DTOs;
using DotLearn.Progress.Models.Entities;
using DotLearn.Progress.Repositories;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DotLearn.Progress.Services;

public class CertificateService : ICertificateService
{
    private readonly ICertificateRepository _repo;
    private readonly IAmazonS3 _s3Client;
    private readonly IConfiguration _config;
    private readonly ILogger<CertificateService> _logger;
    private readonly InternalHttpService _internalHttp;

    public CertificateService(
        ICertificateRepository repo,
        IAmazonS3 s3Client,
        IConfiguration config,
        ILogger<CertificateService> logger,
        InternalHttpService internalHttp)
    {
        _repo = repo;
        _s3Client = s3Client;
        _config = config;
        _logger = logger;
        _internalHttp = internalHttp;
    }

    public async Task GenerateAndUploadAsync(EnrollmentCompletedEventDto evt)
    {
        // Idempotency — skip if already issued
        var existing = await _repo.GetByStudentAndCourseAsync(evt.StudentId, evt.CourseId);
        if (existing != null)
        {
            _logger.LogInformation(
                "Certificate already exists for student {StudentId} course {CourseId}",
                evt.StudentId, evt.CourseId);
            return;
        }

        // Fetch names from internal service APIs
        var courseName = await _internalHttp.GetCourseNameAsync(evt.CourseId);
        var studentName = await _internalHttp.GetStudentNameAsync(evt.StudentId);

        var verificationCode = Guid.NewGuid().ToString();
        var issuedAt = DateTime.UtcNow;

        // Generate PDF
        var pdfBytes = GeneratePdf(studentName, courseName, issuedAt, verificationCode);

        // Upload to S3
        var bucketName = _config["AWS:CertificatesBucket"]!;
        var s3Key = $"certificates/{evt.StudentId}/{evt.CourseId}.pdf";

        using var stream = new MemoryStream(pdfBytes);
        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = s3Key,
            InputStream = stream,
            ContentType = "application/pdf"
        });

        var certificateUrl =
            $"https://{bucketName}.s3.ap-southeast-2.amazonaws.com/{s3Key}";

        // Persist record
        var cert = new Certificate
        {
            Id = Guid.NewGuid(),
            StudentId = evt.StudentId,
            CourseId = evt.CourseId,
            StudentName = studentName,
            CourseName = courseName,
            VerificationCode = verificationCode,
            CertificateS3Key = s3Key,
            CertificateUrl = certificateUrl,
            IssuedAt = issuedAt
        };

        await _repo.AddAsync(cert);

        _logger.LogInformation(
            "Certificate issued for student {StudentId} course {CourseId}",
            evt.StudentId, evt.CourseId);
    }

    public async Task<List<CertificateResponseDto>> GetMyAsync(Guid studentId)
    {
        var certs = await _repo.GetByStudentIdAsync(studentId);
        return certs.Select(MapToDto).ToList();
    }

    public async Task<CertificateVerifyResponseDto?> VerifyAsync(string code)
    {
        var cert = await _repo.GetByVerificationCodeAsync(code);
        if (cert == null) return null;

        return new CertificateVerifyResponseDto(
            cert.StudentName,
            cert.CourseName,
            cert.IssuedAt,
            cert.VerificationCode);
    }

    public async Task<string> GetDownloadUrlAsync(Guid id, Guid studentId)
    {
        var cert = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Certificate not found.");

        if (cert.StudentId != studentId)
            throw new UnauthorizedAccessException("Not your certificate.");

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _config["AWS:CertificatesBucket"]!,
            Key = cert.CertificateS3Key,
            Expires = DateTime.UtcNow.AddHours(1)
        };

        return _s3Client.GetPreSignedURL(request);
    }

    private static byte[] GeneratePdf(
        string studentName, string courseName,
        DateTime issuedAt, string verificationCode)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(40);
                page.Background().Background(Colors.White);

                page.Content().Column(col =>
                {
                    col.Spacing(20);

                    col.Item().PaddingTop(40).Text("Certificate of Completion")
                        .FontSize(36).Bold()
                        .FontColor(Colors.Blue.Darken2)
                        .AlignCenter();

                    col.Item().Text("This certifies that")
                        .FontSize(16).AlignCenter();

                    col.Item().Text(studentName)
                        .FontSize(28).Bold()
                        .FontColor(Colors.Grey.Darken3)
                        .AlignCenter();

                    col.Item().Text("has successfully completed")
                        .FontSize(16).AlignCenter();

                    col.Item().Text(courseName)
                        .FontSize(22).Bold()
                        .FontColor(Colors.Blue.Darken1)
                        .AlignCenter();

                    col.Item().PaddingTop(20)
                        .Text($"Issued: {issuedAt:MMMM dd, yyyy}")
                        .FontSize(14).AlignCenter();

                    col.Item().Text($"Verification Code: {verificationCode}")
                        .FontSize(11)
                        .FontColor(Colors.Grey.Medium)
                        .AlignCenter();
                });
            });
        }).GeneratePdf();
    }

    private static CertificateResponseDto MapToDto(Certificate c) => new(
        c.Id, c.StudentId, c.CourseId, c.StudentName, c.CourseName,
        c.VerificationCode, c.CertificateUrl, c.IssuedAt);
}
