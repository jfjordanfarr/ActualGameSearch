param(
  [int]$SampleSize = 25,
  [int]$Seed = 42,
  [switch]$ForceRefresh,
  [switch]$UseOnnx
)

$env:ETL_SAMPLE_SIZE = $SampleSize
$env:ETL_RANDOM_SEED = $Seed
if ($ForceRefresh) { $env:ETL_FORCE_REFRESH = 1 } else { Remove-Item Env:ETL_FORCE_REFRESH -ErrorAction SilentlyContinue }

if ($UseOnnx) { $env:USE_ONNX_EMBEDDINGS = 'true' } else { Remove-Item Env:USE_ONNX_EMBEDDINGS -ErrorAction SilentlyContinue }

# Auto-detect local model assets if ONNX requested and env vars not already set
if ($UseOnnx) {
  if (-not $env:ACTUALGAME_MODEL_PATH) {
    $defaultModel = Join-Path (Get-Location) 'models/openclip/model.onnx'
    if (Test-Path $defaultModel) { $env:ACTUALGAME_MODEL_PATH = $defaultModel }
  }
  if (-not $env:ACTUALGAME_TOKENIZER_VOCAB) {
    $defaultVocab = Join-Path (Get-Location) 'models/openclip/bpe_simple_vocab_16e6.txt.gz'
    if (Test-Path $defaultVocab) { $env:ACTUALGAME_TOKENIZER_VOCAB = $defaultVocab }
  }
}

Write-Host "Running ETL (sampleSize=$SampleSize seed=$Seed forceRefresh=$ForceRefresh useOnnx=$UseOnnx)" -ForegroundColor Cyan

dotnet run --project .\ActualGameSearch.ETL\ActualGameSearch.ETL.csproj --configuration Debug

if ($LASTEXITCODE -eq 0) {
  Write-Host "ETL complete." -ForegroundColor Green
} else {
  Write-Host "ETL failed with exit code $LASTEXITCODE" -ForegroundColor Red
  exit $LASTEXITCODE
}
