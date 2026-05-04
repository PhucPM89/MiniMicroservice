
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using TransactionService.Configuration;
using TransactionService.Infrastructure.Middleware;
using TransactionService.Infrastructure.Persistence;
using TransactionService.Interfaces.Services;
using TransactionService.Services;
using TransactionService.Services.Authentication;
using TransactionService.Services.Messaging;

namespace TransactionService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddApplicationInsightsTelemetry(builder.Configuration);

        var connectionString = builder.Configuration.GetConnectionString("TransactionDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:TransactionDatabase is not configured.");

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

        builder.Services.AddDbContext<TransactionServiceDbContext>(options =>
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

        builder.Services.AddScoped<ITransactionImportService, TransactionImportService>();
        builder.Services.AddScoped<ITransactionQueryService, TransactionQueryService>();
        builder.Services.AddHostedService<OutboxPublisher>();
        builder.Services.AddHostedService<FileImportConsumer>();

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
                Title = "Transaction Service API",
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
        app.MapGet("/healthz", () => Results.Ok(new { status = "Healthy", service = "TransactionService" }))
            .AllowAnonymous();

        app.Run();
    }
}
