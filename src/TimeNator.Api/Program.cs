using Microsoft.EntityFrameworkCore;
using Serilog;
using TimeNator.Api;
using TimeNator.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console());

var connectionString = builder.Configuration.GetConnectionString("Postgres")
                       ?? throw new InvalidOperationException(
                           "Connection string 'Postgres' is not configured. Set it with: "
                           + "dotnet user-secrets set \"ConnectionStrings:Postgres\" \"<value>\"");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

builder.Services.AddControllers();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapHealthChecks("/health", new() { ResponseWriter = HealthResponse.WriteAsync });

app.MapControllers();

app.Run();
