using ASPER.CORPORATE_BANKING.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using ASPER.AuthAPI.Grpc.Client.Services;
using ASPER.AuthAPI.Protos;

var builder = WebApplication.CreateBuilder(args);
var clientName = builder.Configuration["ClientInfo:ClientName"] ?? "default";

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

var authServiceUrl = builder.Configuration["GrpcSettings:AuthServiceUrl"] ?? "https://localhost:5001";
builder.Services.AddGrpcClient<UserInfoProtoService.UserInfoProtoServiceClient>(options =>
    options.Address = new Uri(authServiceUrl));
builder.Services.AddScoped<IUserInfoGrpcService, UserInfoGrpcService>();

// Register Corporate Banking Services
builder.Services.AddScoped<ASPER.CORPORATE_BANKING.Application.Interfaces.IAuthIntegrationService, ASPER.CORPORATE_BANKING.Application.Services.AuthIntegrationService>();
builder.Services.AddScoped<ASPER.CORPORATE_BANKING.Application.Interfaces.IApprovalWorkflowEngine, ASPER.CORPORATE_BANKING.Application.Services.ApprovalWorkflowEngine>();
builder.Services.AddScoped<ASPER.CORPORATE_BANKING.Application.Interfaces.IAdminConfigService, ASPER.CORPORATE_BANKING.Application.Services.AdminConfigService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddMassTransitWithRabbitMQ();

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
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

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
