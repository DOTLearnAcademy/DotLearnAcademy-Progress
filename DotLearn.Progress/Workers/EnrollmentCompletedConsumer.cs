using Amazon.SQS;
using Amazon.SQS.Model;
using DotLearn.Progress.Models.DTOs;
using DotLearn.Progress.Services;
using System.Text.Json;

namespace DotLearn.Progress.Workers;

public class EnrollmentCompletedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAmazonSQS _sqsClient;
    private readonly IConfiguration _config;
    private readonly ILogger<EnrollmentCompletedConsumer> _logger;

    public EnrollmentCompletedConsumer(
        IServiceScopeFactory scopeFactory,
        IAmazonSQS sqsClient,
        IConfiguration config,
        ILogger<EnrollmentCompletedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _sqsClient = sqsClient;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var response = await _sqsClient.ReceiveMessageAsync(
                    new ReceiveMessageRequest
                    {
                        QueueUrl = _config["SQS:EnrollmentCompletedQueue"],
                        MaxNumberOfMessages = 10,
                        WaitTimeSeconds = 20
                    }, ct);

                foreach (var message in response.Messages)
                {
                    try
                    {
                        var evt = JsonSerializer.Deserialize<EnrollmentCompletedEventDto>(
                            message.Body,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (evt != null)
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var service = scope.ServiceProvider
                                .GetRequiredService<ICertificateService>();
                            await service.GenerateAndUploadAsync(evt);
                        }

                        await _sqsClient.DeleteMessageAsync(
                            _config["SQS:EnrollmentCompletedQueue"],
                            message.ReceiptHandle, ct);

                        _logger.LogInformation(
                            "Processed EnrollmentCompleted message {Id}", message.MessageId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to process message {Id}", message.MessageId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQS polling error");
                await Task.Delay(5000, ct);
            }
        }
    }
}
