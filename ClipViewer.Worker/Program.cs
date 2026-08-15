using ClipViewer.Data;
using ClipViewer.Worker;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var exitCode = 0;
try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    // Fail fast if the DB password is missing or still the .env.example placeholder - mirrors the
    // same guard in ClipViewer.API/Program.cs.
    if (string.IsNullOrWhiteSpace(connectionString) ||
        connectionString.Contains("changeme", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection is missing or still uses the placeholder " +
            "'changeme' Postgres password. Set a real password via the POSTGRES_PASSWORD " +
            "environment variable (see .env.example) before starting the Worker.");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services.AddHostedService<VideoConversionWorker>();

    var host = builder.Build();

    var outputVideoFolder = builder.Configuration.GetSection("UploadOptions").GetSection("OutputVideoFolder").Value;
    var tempVideoFolder = builder.Configuration.GetSection("UploadOptions").GetSection("TempVideoFolder").Value;

    // Configure folders
    if (outputVideoFolder is null || tempVideoFolder is null)
        throw new InvalidOperationException("OutputVideoFolder or TempVideoFolder is not configured");
    if (!Directory.Exists(outputVideoFolder)) Directory.CreateDirectory(outputVideoFolder);
    if (!Directory.Exists(tempVideoFolder)) Directory.CreateDirectory(tempVideoFolder);

    if (args.Contains("--backfill-sizes"))
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var count = await VideoConversionWorker.BackfillClipSizesAsync(dbContext, outputVideoFolder);
        Log.Information("Backfilled SizeBytes for {Count} clip(s)", count);
    }
    else
    {
        host.Run();
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker terminated unexpectedly");
    exitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}

return exitCode;
