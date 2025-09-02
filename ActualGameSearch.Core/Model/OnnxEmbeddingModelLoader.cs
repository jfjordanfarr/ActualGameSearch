namespace ActualGameSearch.Core.Model;

/// <summary>
/// Placeholder for future ONNX embedding model loading logic (text + possible multimodal support).
/// Provides a minimal contract to detect readiness and expose metadata without yet loading a real model.
/// </summary>
public sealed class OnnxEmbeddingModelLoader
{
    public string ModelId => EmbeddingModelDefaults.ModelId;
    public int Dimension => EmbeddingModelDefaults.Dimension;
    public string? ModelFilePath { get; }
    public bool IsLoaded { get; private set; }

    public OnnxEmbeddingModelLoader(string? modelFilePath = null)
    {
        ModelFilePath = modelFilePath;
    }

    /// <summary>
    /// Simulates loading; future implementation will load an ONNX session (e.g., Microsoft.ML.OnnxRuntime) and validate output dimension.
    /// </summary>
    public void Load()
    {
        // Future: validate file hash, initialize inference session, warm cache.
        IsLoaded = true;
    }
}