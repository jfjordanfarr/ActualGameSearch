namespace ActualGameSearch.Core.Services;

public interface IEmbeddingProvider
{
    /// <summary>
    /// Returns a dense embedding vector of fixed length (Dimension) and SHOULD be L2-normalized.
    /// </summary>
    float[] Embed(string text);
    int Dimension { get; }
}

public interface IAsyncEmbeddingProvider : IEmbeddingProvider
{
    ValueTask<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
}

/// <summary>
/// Deterministic placeholder embedding (NOT semantic). SHA256-based repeat -> normalize.
/// Stable for parity tests until real model integration.
/// </summary>
public sealed class DeterministicEmbeddingProvider : IAsyncEmbeddingProvider
{
    public int Dimension => Core.Model.EmbeddingModelDefaults.Dimension;
    public float[] Embed(string text)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text ?? string.Empty));
        var vec = new float[Dimension];
        for (int i = 0; i < vec.Length; i++)
        {
            byte b = bytes[i % bytes.Length];
            vec[i] = (float)((b / 255.0f) * 2 - 1);
        }
        double sumSq = 0; for (int i = 0; i < vec.Length; i++) sumSq += vec[i] * vec[i];
        var norm = (float)System.Math.Sqrt(sumSq);
        if (norm > 0) for (int i = 0; i < vec.Length; i++) vec[i] /= norm;
        return vec;
    }
    public ValueTask<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default) => ValueTask.FromResult(Embed(text));
}

public sealed class AsyncEmbeddingAdapter : IAsyncEmbeddingProvider
{
    private readonly IEmbeddingProvider _inner;
    public AsyncEmbeddingAdapter(IEmbeddingProvider inner) => _inner = inner;
    public int Dimension => _inner.Dimension;
    public float[] Embed(string text) => _inner.Embed(text);
    public ValueTask<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default) => ValueTask.FromResult(_inner.Embed(text));
}
