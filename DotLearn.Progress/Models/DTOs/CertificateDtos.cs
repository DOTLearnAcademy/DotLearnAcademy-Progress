namespace DotLearn.Progress.Models.DTOs;

// Matches the actual Enrollment service SQS event (no StudentName/CourseName — fetched via HTTP)
public record EnrollmentCompletedEventDto(
    string EventType,
    Guid StudentId,
    Guid CourseId,
    Guid EnrollmentId,
    DateTime Timestamp
);

public record CertificateResponseDto(
    Guid Id,
    Guid StudentId,
    Guid CourseId,
    string StudentName,
    string CourseName,
    string VerificationCode,
    string CertificateUrl,
    DateTime IssuedAt
);

public record CertificateVerifyResponseDto(
    string StudentName,
    string CourseName,
    DateTime IssuedAt,
    string VerificationCode
);
