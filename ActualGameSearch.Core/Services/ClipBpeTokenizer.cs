using System.IO.Compression;
using System.Text;

namespace ActualGameSearch.Core.Services;

/// <summary>
/// Minimal BPE tokenizer approximating OpenAI CLIP tokenization.
/// It supports loading a gzipped vocab/merges file (bpe_simple_vocab_16e6.txt.gz).
/// For now we map each line's first column (token) to an incremental id (rank order).
/// A lightweight BPE merge loop is applied; if merge pairs unavailable we fall back to character-level ids.
/// This is intentionally simplified until full parity is required.
/// </summary>
internal sealed class ClipBpeTokenizer
{
    private readonly Dictionary<string,int> _tokenToId;
    private readonly Dictionary<(string left,string right), int> _mergeRanks; // pair -> rank (lower = merge earlier)
    private readonly int _maxLen;
    private static readonly char[] Space = [' '];

    public ClipBpeTokenizer(string vocabPath, int maxLen = 77)
    {
        _maxLen = maxLen;
        (_tokenToId, _mergeRanks) = LoadVocabAndMerges(vocabPath);
    }

    public int[] Encode(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new[] { 0, 2 };
        var words = TokenizeBasic(Normalize(text));
        var ids = new List<int>(words.Count + 2) { 0 }; // start token id 0
        foreach (var w in words)
        {
            foreach (var sub in ApplyBpe(w))
            {
                if (_tokenToId.TryGetValue(sub, out var id)) ids.Add(id);
                else
                {
                    // fallback: character-level decomposition
                    foreach (var ch in sub)
                        if (_tokenToId.TryGetValue(ch.ToString(), out var cid)) ids.Add(cid);
                }
                if (ids.Count >= _maxLen - 1) break;
            }
            if (ids.Count >= _maxLen - 1) break;
        }
        ids.Add(2); // end token id 2 (CLIP convention); truncation safeguard
        if (ids.Count > _maxLen) ids = ids.Take(_maxLen).ToList();
        return ids.ToArray();
    }

    private static string Normalize(string t) => t.ToLowerInvariant();

    private IEnumerable<string> ApplyBpe(string token)
    {
        if (_mergeRanks.Count == 0 || token.Length <= 1)
            return new[] { token };
        // Initialize symbol list as characters
        var symbols = new List<string>(token.Length);
        foreach (var c in token) symbols.Add(c.ToString());

        // Build pair ranks list
        int IterationsGuard = 1000; // safety against pathological loops
        while (symbols.Count > 1 && IterationsGuard-- > 0)
        {
            (int rank, int index)? best = null;
            for (int i = 0; i < symbols.Count - 1; i++)
            {
                var pair = (symbols[i], symbols[i + 1]);
                if (_mergeRanks.TryGetValue(pair, out var r))
                {
                    if (best is null || r < best.Value.rank)
                        best = (r, i);
                }
            }
            if (best is null) break; // no more merges possible
            var idx = best.Value.index;
            var merged = symbols[idx] + symbols[idx + 1];
            symbols[idx] = merged;
            symbols.RemoveAt(idx + 1);
        }
        return symbols;
    }

    private static List<string> TokenizeBasic(string text)
    {
        var list = new List<string>();
        var sb = new StringBuilder();
        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                Flush();
            }
            else if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
            else
            {
                Flush();
                list.Add(c.ToString());
            }
        }
        Flush();
        return list;

        void Flush()
        {
            if (sb.Length > 0)
            {
                list.Add(sb.ToString());
                sb.Clear();
            }
        }
    }

    private static (Dictionary<string,int> vocab, Dictionary<(string,string),int> merges) LoadVocabAndMerges(string path)
    {
        var vocab = new Dictionary<string,int>(StringComparer.Ordinal);
        var merges = new Dictionary<(string,string),int>();
        Stream s = File.OpenRead(path);
        if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            s = new GZipStream(s, CompressionMode.Decompress);
        using (s)
        {
            using var sr = new StreamReader(s, Encoding.UTF8, true);
            string? line;
            int lineNo = 0;
            bool inMerges = false;
            while ((line = sr.ReadLine()) is not null)
            {
                if (line.Length == 0) { inMerges = true; continue; }
                if (line.StartsWith("#")) continue;
                var parts = line.Split(Space, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 0) continue;
                if (!inMerges)
                {
                    var tok = parts[0];
                    if (!vocab.ContainsKey(tok)) vocab[tok] = lineNo++;
                }
                else if (parts.Length == 2)
                {
                    var pair = (parts[0], parts[1]);
                    if (!merges.ContainsKey(pair)) merges[pair] = merges.Count;
                }
            }
        }
        return (vocab, merges);
    }
}