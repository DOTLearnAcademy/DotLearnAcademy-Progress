using Amazon.SQS;
using Amazon.SQS.Model;
using DotLearn.Progress.Models.DTOs;
using DotLearn.Progress.Models.Entities;
using DotLearn.Progress.Repositories;
using DotLearn.Progress.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DotLearn.Progress.Tests;

[TestClass]
public class ProgressServiceTests
{
    private Mock<IProgressRepository> _repoMock = null!;
    private Mock<IAmazonSQS> _sqsMock = null!;
    private Mock<IConfiguration> _configMock = null!;
    private Mock<ILogger<ProgressService>> _loggerMock = null!;
    private IProgressService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _repoMock = new Mock<IProgressRepository>();
        _sqsMock = new Mock<IAmazonSQS>();
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<ProgressService>>();

        _configMock.Setup(c => c["SQS:LessonCompletedQueue"])
            .Returns("https://sqs.ap-southeast-2.amazonaws.com/test/lesson-completed");
        _sqsMock.Setup(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), default))
            .ReturnsAsync(new SendMessageResponse());

        _service = new ProgressService(
            _repoMock.Object,
            _sqsMock.Object,
            _configMock.Object,
            _loggerMock.Object);
    }

    [TestMethod]
    public async Task TrackProgressAsync_Below85Percent_NotMarkedComplete()
    {
        var studentId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();

        _repoMock.Setup(r => r.GetByStudentAndLessonAsync(studentId, lessonId))
            .ReturnsAsync((LessonProgress?)null);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<LessonProgress>()))
            .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<LessonProgress>()))
            .Returns(Task.CompletedTask);

        await _service.TrackProgressAsync(new TrackProgressRequestDto(
            lessonId, Guid.NewGuid(), WatchedSeconds: 84, DurationSeconds: 100), studentId);

        // 84/100 = 84% < 85% threshold — SQS must NOT be called
        _sqsMock.Verify(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), default),
            Times.Never);
    }

    [TestMethod]
    public async Task TrackProgressAsync_At85Percent_MarksCompleteAndPublishesSqsEvent()
    {
        var studentId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();

        _repoMock.Setup(r => r.GetByStudentAndLessonAsync(studentId, lessonId))
            .ReturnsAsync((LessonProgress?)null);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<LessonProgress>()))
            .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<LessonProgress>()))
            .Returns(Task.CompletedTask);

        await _service.TrackProgressAsync(new TrackProgressRequestDto(
            lessonId, Guid.NewGuid(), WatchedSeconds: 85, DurationSeconds: 100), studentId);

        // 85/100 = 85% >= threshold — SQS MUST be called once
        _sqsMock.Verify(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), default),
            Times.Once);
    }

    [TestMethod]
    public async Task TrackProgressAsync_NeverDecrementsWatchedSeconds()
    {
        var studentId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var existing = new LessonProgress
        {
            StudentId = studentId, LessonId = lessonId,
            WatchedSeconds = 60, DurationSeconds = 100, IsCompleted = false
        };

        _repoMock.Setup(r => r.GetByStudentAndLessonAsync(studentId, lessonId))
            .ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<LessonProgress>()))
            .Returns(Task.CompletedTask);

        // Student scrubbed back to 30 seconds — must not decrement below 60
        await _service.TrackProgressAsync(new TrackProgressRequestDto(
            lessonId, Guid.NewGuid(), WatchedSeconds: 30, DurationSeconds: 100), studentId);

        Assert.AreEqual(60, existing.WatchedSeconds);
    }
}
