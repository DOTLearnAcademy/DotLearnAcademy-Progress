using Amazon.SQS;
using DotLearn.Progress.Data;
using DotLearn.Progress.Middleware;
using DotLearn.Progress.Repositories;
using DotLearn.Progress.Services;
using Kralizek.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Amazon;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// AWS Secrets Manager (Only in non-Development environments)
if (!builder.Environment.IsDevelopment())
{
    builder.Configuration.AddSecretsManager(region: RegionEndpoint.APSoutheast2);
}

// Add services to the container.
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ProgressDbContext>(options =>
    options.UseSqlServer(connStr));

builder.Services.AddHealthChecks().AddSqlServer(connStr);

builder.Services.AddScoped<IProgressRepository, ProgressRepository>();
builder.Services.AddScoped<IProgressService, ProgressService>();

builder.Services.AddDefaultAWSOptions(new Amazon.Extensions.NETCore.Setup.AWSOptions
{
    Region = Amazon.RegionEndpoint.APSoutheast2
});
builder.Services.AddAWSService<IAmazonSQS>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Authentication & Authorization (Placeholder)
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Middlewares
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandler>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
