using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ActualGameSearch.ETL;

namespace ActualGameSearch.Api;

internal sealed class EtlOnStartHostedService : IHostedService
{
    private readonly ILogger<EtlOnStartHostedService> _logger;
    public EtlOnStartHostedService(ILogger<EtlOnStartHostedService> logger) => _logger = logger;
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var enabled = string.Equals(Environment.GetEnvironmentVariable("ACTUALGAME_ETL_ON_START"), "true", StringComparison.OrdinalIgnoreCase);
        if (!enabled)
        {
            _logger.LogInformation("Startup ETL disabled (set ACTUALGAME_ETL_ON_START=true to enable).");
            return;
        }
        try
        {
            _logger.LogInformation("Starting on-start ETL run...");
            var res = await EtlRunner.RunAsync(_logger, cancellationToken);
            _logger.LogInformation("On-start ETL complete games={Games} hash={Hash}", res.GameCount, res.DbSha256);
        }
        catch (Exception ex)
        {
            var strict = string.Equals(Environment.GetEnvironmentVariable("ACTUALGAME_STRICT_MANIFEST"), "true", StringComparison.OrdinalIgnoreCase);
            if (strict) throw; // fail fast in strict mode
            _logger.LogWarning(ex, "On-start ETL failed (continuing; strict mode off)");
        }
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
