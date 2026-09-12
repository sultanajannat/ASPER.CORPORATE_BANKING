using ASPER.CORPORATE_BANKING.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Runtime.InteropServices;
using ASPER.AuthAPI.Grpc.Client.Services;
using ASPER.AuthAPI.Protos;

// --- DinkToPdf Native Library Loading ---
var architectureFolder = (IntPtr.Size == 8) ? "x64" : "x86";
var wkHtmlToPdfPath = "";

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    wkHtmlToPdfPath = Path.Combine(Directory.GetCurrentDirectory(), "libwkhtmltox.dll");
}
else
{
    wkHtmlToPdfPath = Path.Combine(Directory.GetCurrentDirectory(), "runtimes", $"linux-{architectureFolder}", "native", "libwkhtmltox.so");
}

if (File.Exists(wkHtmlToPdfPath))
{
    NativeLibrary.Load(wkHtmlToPdfPath);
}
// ----------------------------------------

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

var clientProfile = builder.Configuration["CLIENT"]
    ?? Environment.GetEnvironmentVariable("CLIENT");

if (string.IsNullOrWhiteSpace(clientProfile))
{
    throw new InvalidOperationException(
        "CLIENT is not set. Select an ASPER, PBF, or SMBL launch profile, or set the CLIENT environment variable.");
}

builder.Configuration.AddJsonFile(
    $"appsettings.{clientProfile}.json",
    optional: false,
    reloadOnChange: true);

var clientName = builder.Configuration["ClientName"];
if (string.IsNullOrWhiteSpace(clientName))
{
    throw new InvalidOperationException($"ClientName is missing in appsettings.{clientProfile}.json.");
}
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy
                  .AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddInfrastructure(builder.Configuration);

static string RequireConfig(IConfiguration configuration, string key) =>
    configuration[key]
    ?? throw new InvalidOperationException($"Configuration '{key}' is missing.");

var authServiceUrl = RequireConfig(builder.Configuration, "GrpcSettings:AuthServiceUrl");
builder.Services.AddGrpcClient<UserInfoProtoService.UserInfoProtoServiceClient>(options =>
    options.Address = new Uri(authServiceUrl));
builder.Services.AddScoped<IUserInfoGrpcService, UserInfoGrpcService>();

// Register Corporate Banking Services
builder.Services.AddScoped<ASPER.CORPORATE_BANKING.Application.Interfaces.IAuthIntegrationService, ASPER.CORPORATE_BANKING.Application.Services.AuthIntegrationService>();
builder.Services.AddScoped<ASPER.CORPORATE_BANKING.Application.Interfaces.IApprovalWorkflowEngine, ASPER.CORPORATE_BANKING.Application.Services.ApprovalWorkflowEngine>();
builder.Services.AddScoped<ASPER.CORPORATE_BANKING.Application.Interfaces.IAdminConfigService, ASPER.CORPORATE_BANKING.Application.Services.AdminConfigService>();
builder.Services.AddScoped<ASPER.CORPORATE_BANKING.Application.Interfaces.IFileIngestionService, ASPER.CORPORATE_BANKING.Application.Services.FileIngestionService>();
builder.Services.AddScoped<ASPER.CORPORATE_BANKING.Application.Interfaces.IAdminMonitoringService, ASPER.CORPORATE_BANKING.Application.Services.AdminMonitoringService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddMassTransitWithRabbitMQ(builder.Configuration);
builder.Services.AddHostedService<ASPER.CORPORATE_BANKING.Workers.OutboxProcessorBackgroundService>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ASPER CORPORATE BANKING API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Just paste your Token below. (System auto-adds 'Bearer')",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http, 
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ValidateIssuer = false,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = false,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    var loadedSecret = builder.Configuration["JwtSettings:Secret"];
    var loadedIssuer = builder.Configuration["JwtSettings:Issuer"];

    Console.WriteLine("=============================================");
    Console.WriteLine($"[CB DEBUG] Loaded Secret: '{loadedSecret}'");
    Console.WriteLine($"[CB DEBUG] Loaded Issuer: '{loadedIssuer}'");
    Console.WriteLine("=============================================");

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var authorization = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authorization) && !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                context.Token = authorization;
            }
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
