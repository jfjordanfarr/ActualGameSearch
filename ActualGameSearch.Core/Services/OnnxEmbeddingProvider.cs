using ActualGameSearch.Core.Model;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Text;
using System.Text.RegularExpressions;

namespace ActualGameSearch.Core.Services;

/// <summary>
/// Placeholder ONNX embedding provider; until real model assets are added it throws when used.
/// </summary>
public sealed class OnnxEmbeddingProvider : IAsyncEmbeddingProvider
{
    private readonly OnnxEmbeddingModelLoader _loader;
    private readonly bool _enabled;
    private InferenceSession? _session;
    private readonly string? _tokenizerVocabPath;
    private readonly string? _tokenizerMergesPath;
    private ClipBpeTokenizer? _clipTokenizer; // simplified tokenizer instance
    private Dictionary<string,int>? _vocab; // token -> id (legacy support)
    private int _nextDynamicId = 10000; // for fallback OOV allocation (debug only)
    private string? _inputIdsName; // resolved ONNX input name
    private string? _pooledOutputName; // resolved pooled output tensor name
    // Split into alphanum sequences or individual punctuation symbols
    private static readonly Regex _basicTokenSplit = new("[A-Za-z0-9_]+|[^\\sA-Za-z0-9_]", RegexOptions.Compiled);
    public int Dimension => EmbeddingModelDefaults.Dimension;

    public OnnxEmbeddingProvider()
    {
        _enabled = string.Equals(Environment.GetEnvironmentVariable("USE_ONNX_EMBEDDINGS"), "true", StringComparison.OrdinalIgnoreCase);
        var modelPath = Environment.GetEnvironmentVariable("ACTUALGAME_MODEL_PATH");
        _loader = new OnnxEmbeddingModelLoader(modelPath);
        _tokenizerVocabPath = Environment.GetEnvironmentVariable("ACTUALGAME_TOKENIZER_VOCAB");
        _tokenizerMergesPath = Environment.GetEnvironmentVariable("ACTUALGAME_TOKENIZER_MERGES");
        if (_enabled && modelPath is not null && File.Exists(modelPath))
        {
            try
            {
                _loader.Load();
                var opts = new SessionOptions();
                opts.EnableMemoryPattern = false;
                _session = new InferenceSession(modelPath, opts);
                // Determine acceptable input/output names
                try
                {
                    _inputIdsName = _session.InputMetadata.Keys.FirstOrDefault(k => k.Contains("input_ids", StringComparison.OrdinalIgnoreCase))
                        ?? _session.InputMetadata.Keys.First();
                    _pooledOutputName = _session.OutputMetadata.Keys.FirstOrDefault(k => k is "text_embeds" or "pooled_output" or "sentence_embedding");
                }
                catch { /* fallback later */ }
                // Preload vocab / tokenizer if provided
                if (!string.IsNullOrWhiteSpace(_tokenizerVocabPath) && File.Exists(_tokenizerVocabPath))
                {
                    try
                    {
                        _clipTokenizer = new ClipBpeTokenizer(_tokenizerVocabPath);
                        // Expose its internal map via reflection not needed; re-encode a small word list to warm JIT
                        _vocab = typeof(ClipBpeTokenizer)
                            .GetField("_tokenToId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                            .GetValue(_clipTokenizer) as Dictionary<string,int>;
                    }
                    catch { /* ignore */ }
                }
            }
            catch { /* swallow; will throw on Embed */ }
        }
    }

    public float[] Embed(string text)
    {
        if (!_enabled)
        {
            throw new InvalidOperationException("ONNX provider disabled (using deterministic placeholder). Set USE_ONNX_EMBEDDINGS=true and provide ACTUALGAME_MODEL_PATH.");
        }
        if (!_loader.IsLoaded || _session is null)
            throw new InvalidOperationException("ONNX embedding provider enabled but model/session not loaded. Check ACTUALGAME_MODEL_PATH and tokenizer env vars.");

        // Tokenize (OpenCLIP-style placeholder). TODO: Replace with real BPE merges + vocab when assets added.
    var safeText = text ?? string.Empty;
    var tokenIds = Tokenize(safeText);
        int maxLen = int.TryParse(Environment.GetEnvironmentVariable("ACTUALGAME_MAX_TOKENS"), out var m) ? m : 77; // CLIP default
        if (tokenIds.Length > maxLen) tokenIds = tokenIds[..maxLen];
        // Pad with 0
        long[] longTokens;
        if (tokenIds.Length < maxLen)
        {
            longTokens = new long[maxLen];
            for (int i = 0; i < tokenIds.Length; i++) longTokens[i] = tokenIds[i];
            // remaining already zero
        }
        else
        {
            longTokens = tokenIds.Select(i => (long)i).ToArray();
        }

        // Some exported CLIP text encoders expect int32 instead of int64.
        NamedOnnxValue MakeInput()
        {
            if (_inputIdsName is null) _inputIdsName = "input_ids";
            if (_session!.InputMetadata.TryGetValue(_inputIdsName, out var meta))
            {
                if (meta.ElementType == typeof(int) || meta.ElementType == typeof(Int32))
                {
                    var tensor = new DenseTensor<int>(new[] { 1, longTokens.Length });
                    for (int i = 0; i < longTokens.Length; i++) tensor[0, i] = (int)longTokens[i];
                    return NamedOnnxValue.CreateFromTensor(_inputIdsName, tensor);
                }
            }
            var inputIds64 = new DenseTensor<long>(new[] { 1, longTokens.Length });
            for (int i = 0; i < longTokens.Length; i++) inputIds64[0, i] = longTokens[i];
            return NamedOnnxValue.CreateFromTensor(_inputIdsName, inputIds64);
        }
        var inputList = new List<NamedOnnxValue> { MakeInput() };
        float[]? pooled = null;
        try
        {
            using var results = _session.Run(inputList);
            // Try common output names
            var wanted = results.FirstOrDefault(r => r.Name == _pooledOutputName) ??
                         results.FirstOrDefault(r => r.Name is "text_embeds" or "pooled_output" or "sentence_embedding");
            if (wanted is not null && wanted.Value is DenseTensor<float> dt)
            {
                var dims = dt.Dimensions;
                if (dims.Length == 2 && dims[0] == 1 && dims[1] == this.Dimension)
                {
                    pooled = dt.ToArray();
                }
            }
            // If no pooled output, attempt mean pool over last_hidden_state
            if (pooled is null)
            {
                var hidden = results.FirstOrDefault(r => r.Name == "last_hidden_state");
                if (hidden?.Value is DenseTensor<float> hdt)
                {
                    var hdims = hdt.Dimensions;
                    if (hdims.Length == 3)
                    {
                        int seq = hdims[1];
                        int dim = hdims[2];
                        pooled = new float[dim];
                        for (int s = 0; s < seq; s++)
                            for (int d = 0; d < dim; d++)
                                pooled[d] += hdt[0, s, d];
                        for (int d = 0; d < dim; d++) pooled[d] /= seq;
                        if (dim != this.Dimension) return Deterministic(safeText);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("ONNX inference failed", ex);
        }

        if (pooled is null)
            throw new InvalidOperationException("ONNX inference produced no pooled output");

        // L2 normalize
        double norm = 0; for (int i = 0; i < pooled.Length; i++) norm += pooled[i] * pooled[i];
        norm = Math.Sqrt(norm);
        if (norm > 0)
            for (int i = 0; i < pooled.Length; i++) pooled[i] = (float)(pooled[i] / norm);
    return pooled;
    }

    public ValueTask<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default) => ValueTask.FromResult(Embed(text));

    private float[] Deterministic(string text) => new DeterministicEmbeddingProvider().Embed(text); // retained for potential future fallback toggles

    private int[] Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new[] { 0, 2 };
        EnsureVocab();
        if (_vocab is null)
        {
            // Extremely naive fallback
            return text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(w => (int)((uint)w.GetHashCode() % 10000))
                .Prepend(0)
                .Append(2)
                .ToArray();
        }
        if (_clipTokenizer is not null)
        {
            try { return _clipTokenizer.Encode(text); } catch { /* fall through */ }
        }
        return new[] { 0, 2 };
    }

    private void EnsureVocab()
    {
        if (_vocab is not null) return;
        try
        {
            if (!string.IsNullOrWhiteSpace(_tokenizerVocabPath) && File.Exists(_tokenizerVocabPath))
            {
                // Expect JSON {"token": id, ...} or simple newline tokens list.
                var text = File.ReadAllText(_tokenizerVocabPath);
                if (text.TrimStart().StartsWith("{"))
                {
                    var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,int>>(text);
                    if (dict is not null) _vocab = dict;
                }
                else
                {
                    var dict = new Dictionary<string,int>(StringComparer.Ordinal);
                    int i = 0;
                    foreach (var line in text.Split('\n'))
                    {
                        var tok = line.Trim(); if (tok.Length == 0) continue; dict[tok] = i++;
                    }
                    if (dict.Count > 0) _vocab = dict;
                }
            }
        }
        catch { /* ignore; fallback hashing in place */ }
    }
}