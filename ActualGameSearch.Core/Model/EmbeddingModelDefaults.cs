namespace ActualGameSearch.Core.Model;

/// <summary>
/// Central authoritative bootstrap embedding model constants (placeholder until real multimodal model added).
/// </summary>
public static class EmbeddingModelDefaults
{
    public const string ModelId = "mini-clip-int8"; // single source of truth
    public const int Dimension = 512;
    public const string TokenizerId = "clip-tokenizer-v1";
    public const string Quantization = "int8";
}
