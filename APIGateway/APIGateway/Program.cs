
using APIGateway.Configuration;
using APIGateway.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace APIGateway;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.Configure<GatewayJwtOptions>(
            builder.Configuration.GetSection(GatewayJwtOptions.SectionName));

        if (builder.Configuration.GetSection(GatewayJwtOptions.SectionName).Get<GatewayJwtOptions>() is null)
        {
            throw new InvalidOperationException("GatewayJwt configuration is missing.");
        }

        builder.Services.AddHttpClient(nameof(JwksKeyProvider), client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        builder.Services.AddSingleton<IJwksKeyProvider, JwksKeyProvider>();
        builder.Services.AddSingleton<Microsoft.Extensions.Options.IConfigureOptions<JwtBearerOptions>, JwtBearerOptionsSetup>();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        builder.Services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
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

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services
            .AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors("FrontendDev");
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/healthz", (IConfiguration configuration) =>
        {
            return Results.Ok(new
            {
                Status = "Healthy",
                Gateway = "APIGateway",
                Routes = configuration.GetSection("ReverseProxy:Routes").GetChildren().Select(route => route.Key).ToArray()
            });
        }).AllowAnonymous();

        app.MapControllers();
        app.MapReverseProxy();

        app.Run();
    }
}
