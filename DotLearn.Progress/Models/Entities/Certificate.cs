namespace DotLearn.Progress.Models.Entities;

public class Certificate
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public string StudentName { get; set; } = null!;
    public string CourseName { get; set; } = null!;
    public string VerificationCode { get; set; } = null!;
    public string CertificateS3Key { get; set; } = null!;
    public string CertificateUrl { get; set; } = null!;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}
