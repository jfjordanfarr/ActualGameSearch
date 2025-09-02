using System.Text.Json;
using ActualGameSearch.Core.Services;
using Xunit;

namespace ActualGameSearch.Tests;

public class OnnxParityTests
{
    [Fact]
    public void Onnx_Embedding_Shape_And_Norm_When_Enabled()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("USE_ONNX_EMBEDDINGS"), "true", StringComparison.OrdinalIgnoreCase))
            return; // skip
        var provider = new OnnxEmbeddingProvider();
        var v = provider.Embed("semantic calibration test");
        Assert.Equal(provider.Dimension, v.Length);
        var norm = Math.Sqrt(v.Sum(f => f * f));
        Assert.InRange(norm, 0.98, 1.02);
    }

    [Fact]
    public void Calibration_Vectors_Cosine_Close_To_Current_When_Onnx()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("USE_ONNX_EMBEDDINGS"), "true", StringComparison.OrdinalIgnoreCase))
            return; // skip
        var calibPath = Path.Combine(AppContext.BaseDirectory, "..","..","..","..","embedding_parity_calibration.json");
        if (!File.Exists(calibPath)) return; // skip if not generated yet
        var json = JsonDocument.Parse(File.ReadAllText(calibPath));
        if (!json.RootElement.TryGetProperty("vectors", out var vectorsEl)) return;
        var prompts = json.RootElement.GetProperty("prompts").EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToArray();
        var provider = new OnnxEmbeddingProvider();
        int dim = provider.Dimension;
        int count = Math.Min(vectorsEl.GetArrayLength(), prompts.Length);
        for (int i=0;i<count;i++)
        {
            var refVec = vectorsEl[i].EnumerateArray().Select(x => (float)x.GetDouble()).ToArray();
            var cur = provider.Embed(prompts[i]);
            Assert.Equal(dim, refVec.Length);
            double dot = 0; for (int j=0;j<dim;j++) dot += refVec[j]*cur[j];
            Assert.True(dot >= 0.985, $"Cosine too low for prompt index {i}: {dot}");
        }
    }
}