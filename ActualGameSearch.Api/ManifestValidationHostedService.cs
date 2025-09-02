using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ActualGameSearch.Api;

internal sealed class ManifestValidationHostedService : IHostedService
{
    private readonly ILogger<ManifestValidationHostedService> _logger;
    public ManifestValidationHostedService(ILogger<ManifestValidationHostedService> logger) => _logger = logger;
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var manifestPath = ActualGameSearch.Core.AppConfig.ManifestPath;
            var dbPath = ActualGameSearch.Core.AppConfig.DbPath;
            if (File.Exists(manifestPath))
            {
                var manifest = ActualGameSearch.Core.Manifest.DatasetManifestLoader.Load(manifestPath);
                var expectedId = ActualGameSearch.Core.Model.EmbeddingModelDefaults.ModelId;
                var expectedDim = ActualGameSearch.Core.Model.EmbeddingModelDefaults.Dimension;
                bool strict = string.Equals(Environment.GetEnvironmentVariable("ACTUALGAME_STRICT_MANIFEST"), "true", StringComparison.OrdinalIgnoreCase);
                if (manifest.Model.Id != expectedId || manifest.Model.Dimension != expectedDim)
                {
                    var msg = $"Manifest model mismatch expected={expectedId}/{expectedDim} got={manifest.Model.Id}/{manifest.Model.Dimension}";
                    if (strict) throw new InvalidOperationException(msg);
                    _logger.LogWarning(msg);
                }
                else _logger.LogInformation("Manifest model validated: {Id}/{Dim}", manifest.Model.Id, manifest.Model.Dimension);

                // Recompute DB hash if DB exists; compare to manifest.DbSha256 for integrity
                if (File.Exists(dbPath))
                {
                    try
                    {
                        var recomputed = ActualGameSearch.Core.Manifest.DatasetManifestLoader.ComputeSha256(dbPath);
                        if (!string.Equals(recomputed, manifest.DbSha256, StringComparison.OrdinalIgnoreCase))
                        {
                            var msg = $"Database hash mismatch recomputed={recomputed} manifest={manifest.DbSha256}";
                            if (strict) throw new InvalidOperationException(msg);
                            _logger.LogWarning(msg);
                        }
                        else
                        {
                            _logger.LogInformation("Database hash validated ({Hash})", recomputed);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (strict) throw;
                        _logger.LogWarning(ex, "Failed to recompute DB hash");
                    }
                }
                else
                {
                    _logger.LogInformation("DB path {DbPath} not present; skipping hash recompute", dbPath);
                }

                // Model / tokenizer file hash validation (best-effort)
                try
                {
                    if (!string.IsNullOrWhiteSpace(manifest.ModelFileSha256))
                    {
                        var modelPath = Environment.GetEnvironmentVariable("ACTUALGAME_MODEL_PATH");
                        if (!string.IsNullOrWhiteSpace(modelPath) && File.Exists(modelPath))
                        {
                            var mh = ActualGameSearch.Core.Manifest.DatasetManifestLoader.ComputeSha256(modelPath);
                            if (!string.Equals(mh, manifest.ModelFileSha256, StringComparison.OrdinalIgnoreCase))
                            {
                                var msg = $"Model file hash mismatch current={mh} manifest={manifest.ModelFileSha256}";
                                if (strict) throw new InvalidOperationException(msg);
                                _logger.LogWarning(msg);
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(manifest.TokenizerFileSha256))
                    {
                        var tokPath = Environment.GetEnvironmentVariable("ACTUALGAME_TOKENIZER_VOCAB");
                        if (!string.IsNullOrWhiteSpace(tokPath) && File.Exists(tokPath))
                        {
                            var th = ActualGameSearch.Core.Manifest.DatasetManifestLoader.ComputeSha256(tokPath);
                            if (!string.Equals(th, manifest.TokenizerFileSha256, StringComparison.OrdinalIgnoreCase))
                            {
                                var msg = $"Tokenizer file hash mismatch current={th} manifest={manifest.TokenizerFileSha256}";
                                if (strict) throw new InvalidOperationException(msg);
                                _logger.LogWarning(msg);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (strict) throw;
                    _logger.LogWarning(ex, "Model/tokenizer hash validation failed");
                }
            }
            else
            {
                _logger.LogInformation("Manifest not present at {Path}; skipping validation", manifestPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Manifest validation failed");
        }
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
