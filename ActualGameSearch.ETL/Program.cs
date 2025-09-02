using ActualGameSearch.ETL;
using ActualGameSearch.Core;

var exportTopRaw = Environment.GetEnvironmentVariable("ACTUALGAME_EXPORT_EMBEDDINGS_TOP");
if (int.TryParse(exportTopRaw, out var exportTop) && exportTop > 0)
{
	// Export mode (does not run ETL; assumes existing DB)
	var outPath = Environment.GetEnvironmentVariable("ACTUALGAME_EXPORT_EMBEDDINGS_PATH") ?? Path.Combine(AppContext.BaseDirectory, "embeddings-sample.json");
	try
	{
		EtlRunner.ExportEmbeddings(outPath, exportTop, null);
		Console.WriteLine($"Export complete. File: {outPath}");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Export failed: {ex.Message}");
		Environment.ExitCode = 2;
	}
}
else
{
	var result = await EtlRunner.RunAsync();
	Console.WriteLine($"ETL complete. DB: {result.DbPath}\nManifest: {result.ManifestPath}\nSHA256: {result.DbSha256}");
}