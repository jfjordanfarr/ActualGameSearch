using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// OpenTelemetry Collector container (development only) with minimal logging exporter.
var otel = builder.AddContainer("otel-collector", "otel/opentelemetry-collector:latest")
	.WithBindMount("./otel-collector-config.yaml", "/etc/otel-collector-config.yaml")
	.WithEnvironment("OTEL_CONFIG_FILE", "/etc/otel-collector-config.yaml")
	.WithHttpEndpoint(targetPort: 4318, name: "otlp-http")
	.WithEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc");

// API project
var api = builder.AddProject("api", "../ActualGameSearch.Api/ActualGameSearch.Api.csproj")
	.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4317")
	.WithHttpEndpoint(env: "ASPNETCORE_URLS", port: 5088, targetPort: 8080, name: "http")
	.WithExternalHttpEndpoints();

// WebApp project (Blazor) referencing API & collector (for future client diagnostics if needed)
var web = builder.AddProject("webapp", "../ActualGameSearch.WebApp/ActualGameSearch.WebApp.csproj")
	.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4317")
	.WithHttpEndpoint(env: "ASPNETCORE_URLS", port: 5099, targetPort: 8080, name: "http")
	.WithExternalHttpEndpoints();

builder.Build().Run();
