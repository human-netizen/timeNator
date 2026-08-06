using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using TimeNator.Api;
using TimeNator.Api.Data;
using TimeNator.Api.Entities;
using TimeNator.Api.Hubs;
using TimeNator.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console());

var connectionString = builder.Configuration.GetConnectionString("Postgres")
                       ?? throw new InvalidOperationException(
                           "Connection string 'Postgres' is not configured. Set it with: "
                           + "dotnet user-secrets set \"ConnectionStrings:Postgres\" \"<value>\"");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddEntityFrameworkStores<AppDbContext>();

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwt = jwtSection.Get<JwtOptions>()
          ?? throw new InvalidOperationException("The 'Jwt' configuration section is missing.");
if (Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32)
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes.");

builder.Services.Configure<JwtOptions>(jwtSection);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            NameClaimType = "name",
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        // Browsers and the SignalR client cannot set headers on a WebSocket upgrade,
        // so hub connections carry the token in the query string instead.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    context.Token = token;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<SubjectService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<DayOffService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<InviteService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<ModerationService>();
builder.Services.AddScoped<AllowedAppService>();
builder.Services.AddScoped<DistractionService>();
builder.Services.AddScoped<TodoService>();
builder.Services.AddScoped<DdayService>();
builder.Services.AddScoped<TimetableService>();
builder.Services.AddScoped<DailyReviewService>();

var redisConnection = builder.Configuration.GetConnectionString("Redis")
                      ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddSingleton<PresenceService>();
builder.Services.AddScoped<LeaderboardService>();
builder.Services.AddScoped<IPasswordHasher<Group>, PasswordHasher<Group>>();

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddSingleton<HubRateLimitFilter>();
builder.Services.AddSignalR(options => options.AddFilter<HubRateLimitFilter>());

// Login and registration are the targets for password guessing and account spam, so they get
// a strict per-address window. Everything else is behind authentication already.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("RateLimits:AuthPerMinute", 10),
            Window = TimeSpan.FromMinutes(1)
        }));
});

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health", new() { ResponseWriter = HealthResponse.WriteAsync });

app.MapControllers();
app.MapHub<StudyHub>("/hubs/study");

app.Run();

public partial class Program;
