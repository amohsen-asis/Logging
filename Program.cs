using Microsoft.EntityFrameworkCore;
using Logging.Data;
using Serilog;
using Logging.Middleware;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build())
    .Enrich.FromLogContext()
    .CreateLogger();

try 
{
    var builder = WebApplication.CreateBuilder(args);

    // Replace default logging with Serilog
    builder.Logging.ClearProviders();
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add DbContext
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Add health checks
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("Database");

    // Configure Kestrel to use specific ports
    builder.WebHost.UseUrls("http://localhost:5000", "https://localhost:5001");

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else 
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee API v1"));
    }

    // Add global exception middleware
    app.UseGlobalExceptionHandler();

    app.UseRateLimiter(options =>
    {
        options.MaxRequestsPerTimeWindow = 100; // 100 requests per minute
        options.TimeWindow = TimeSpan.FromMinutes(1);
    });

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    // Add health check endpoint
    app.MapHealthChecks("/health");

    Log.Information("Starting the application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}
