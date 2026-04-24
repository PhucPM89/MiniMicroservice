
using FileService.Configuration;
using FileService.Infrastructure.Middleware;
using FileService.Infrastructure.Persistence;
using FileService.Interfaces.Repositories;
using FileService.Interfaces.Services;
using FileService.Repositories;
using FileService.Services;
using FileService.Services.Authentication;
using FileService.Services.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace FileService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("FileDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:FileDatabase is not configured.");

        builder.Services.Configure<FileStorageOptions>(
            builder.Configuration.GetSection(FileStorageOptions.SectionName));
        builder.Services.Configure<OutboxOptions>(
            builder.Configuration.GetSection(OutboxOptions.SectionName));
        builder.Services.Configure<RabbitMqOptions>(
            builder.Configuration.GetSection(RabbitMqOptions.SectionName));
        builder.Services.Configure<JwtValidationOptions>(
            builder.Configuration.GetSection(JwtValidationOptions.SectionName));

        if (builder.Configuration.GetSection(JwtValidationOptions.SectionName).Get<JwtValidationOptions>() is null)
        {
            throw new InvalidOperationException("JwtValidation configuration is missing.");
        }

        builder.Services.AddDbContext<FileServiceDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        builder.Services.AddHttpClient(nameof(JwksKeyProvider), client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        builder.Services.AddSingleton<IJwksKeyProvider, JwksKeyProvider>();
        builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerOptionsSetup>();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        builder.Services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        builder.Services.AddScoped<IFileRepository, FileRepository>();
        builder.Services.AddScoped<IFileService, Services.FileService>();
        builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
        builder.Services.AddScoped<FileImportResultService>();
        builder.Services.AddHostedService<OutboxPublisher>();
        builder.Services.AddHostedService<FileImportResultConsumer>();

        builder.Services.AddControllers();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("FrontendDev", policy =>
            {
                policy
                    .WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "File Service API",
                Version = "v1"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        var app = builder.Build();

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors("FrontendDev");
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapGet("/healthz", () => Results.Ok(new { status = "Healthy", service = "FileService" }))
            .AllowAnonymous();

        app.Run();
    }
}
