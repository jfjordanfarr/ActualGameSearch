param(
  [string]$ModelUrl = 'https://clip-as-service.s3.us-east-2.amazonaws.com/models/onnx/ViT-B-32/textual.onnx',
  # For OpenAI CLIP BPE vocab we use the public bpe_simple_vocab_16e6.txt.gz (kept gz compressed)
  [string]$TokenizerVocabUrl = 'https://huggingface.co/sentence-transformers/clip-ViT-B-32/resolve/main/0_CLIPModel/bpe_simple_vocab_16e6.txt.gz?download=true',
  # Merges (ranks) distributed in same file historically; separate mirror retained for future diffing
  [string]$TokenizerMergesUrl = 'https://raw.githubusercontent.com/openai/CLIP/main/clip/bpe_simple_vocab_16e6.txt.gz',
  [string]$OutDir = 'models/openclip'
)

# NOTE: We default to the textual encoder ONNX. For image later: visual.onnx from same S3 path.

$ErrorActionPreference = 'Stop'

$fullOut = Join-Path (Get-Location) $OutDir
New-Item -ItemType Directory -Force -Path $fullOut | Out-Null

function Get-IfNeeded($url, $path) {
  if (Test-Path $path) {
    Write-Host "Exists: $path (skipping download)" -ForegroundColor Yellow
  } else {
    Write-Host "Downloading $url -> $path" -ForegroundColor Cyan
    Invoke-WebRequest -Uri $url -OutFile $path
  }
  $hash = (Get-FileHash -Algorithm SHA256 $path).Hash.ToLower()
  return $hash
}

$modelPath = Join-Path $fullOut 'model.onnx'
$vocabPath = Join-Path $fullOut 'bpe_simple_vocab_16e6.txt.gz'
$legacyVocab = Join-Path $fullOut 'vocab.json'
$mergesPath = Join-Path $fullOut 'merges.txt'

# Migrate old filename if present
if ((Test-Path $legacyVocab) -and -not (Test-Path $vocabPath)) {
  Write-Host "Renaming legacy vocab.json -> bpe_simple_vocab_16e6.txt.gz" -ForegroundColor Yellow
  Rename-Item $legacyVocab $vocabPath
}

$modelHash = Get-IfNeeded $ModelUrl $modelPath
$vocabHash = Get-IfNeeded $TokenizerVocabUrl $vocabPath
$mergesHash = Get-IfNeeded $TokenizerMergesUrl $mergesPath

Write-Host "Model SHA256:     $modelHash" -ForegroundColor Green
Write-Host "Tokenizer vocab:  $vocabHash" -ForegroundColor Green
Write-Host "Tokenizer merges: $mergesHash" -ForegroundColor Green

Write-Host "Set environment variables for ETL + API/WebApp:" -ForegroundColor Magenta
Write-Host "  $env:ACTUALGAME_MODEL_PATH=$modelPath" -ForegroundColor Magenta
Write-Host "  $env:ACTUALGAME_TOKENIZER_VOCAB=$vocabPath" -ForegroundColor Magenta
Write-Host "(Set ACTUALGAME_TOKENIZER_MERGES if/when merges are consumed)" -ForegroundColor Magenta
