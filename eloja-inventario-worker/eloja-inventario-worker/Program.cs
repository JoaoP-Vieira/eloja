using eloja_inventario_worker.Commands;
using eloja_inventario_worker.Configuration;
using eloja_inventario_worker.Data;
using eloja_inventario_worker.Data.Interfaces;
using eloja_inventario_worker.Infrastructure;
using JasperFx.Core;
using Microsoft.AspNetCore.Builder;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.HealthChecks;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

string lokiUrl = builder.Configuration["Loki:Url"] ?? "http://loki:3100";

Log.Logger = new LoggerConfiguration()
	.WriteTo.GrafanaLoki(
		uri: lokiUrl,
		labels: new[] { new LokiLabel { Key = "app", Value = "eloja-inventario-worker" } },
		textFormatter: new Serilog.Formatting.Json.JsonFormatter()
	)
	.WriteTo.Console()
	.CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddWolverine(opts =>
{
	opts.UseRabbitMq(rabbit =>
	{
		rabbit.HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "";
		rabbit.UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "";
		rabbit.Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "";
		rabbit.VirtualHost = Environment.GetEnvironmentVariable("RABBITMQ_VHOST") ?? "/";
		rabbit.Port = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var port) ? port : 5672;
		rabbit.Ssl = new RabbitMQ.Client.SslOption()
		{
			Enabled = bool.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_SSL_ENABLED"), out var sslEnabled) && sslEnabled
		};
	});

	opts.OnAnyException()
		.RetryWithCooldown(300.Milliseconds(), 1.Seconds(), 5.Seconds())
		.Then.MoveToErrorQueue();

	opts.ListenToRabbitQueue("queue.inventario.reservar")
		.DeadLetterQueueing(new DeadLetterQueue("queue.inventario.reservar.deadletter"))
		.DefaultIncomingMessage<ReservarInventarioCommand>()
		.PreFetchCount(100)
		.ListenerCount(5)
		.CircuitBreaker(cb =>
		{
			cb.PauseTime = 1.Minutes();
			cb.FailurePercentageThreshold = 10;
		})
		.UseDurableInbox();

	opts.ListenToRabbitQueue("queue.inventario.reservar-db-save")
		.DeadLetterQueueing(new DeadLetterQueue("queue.inventario.reservar-db-save.deadletter"))
		.DefaultIncomingMessage<ReservaInventarioDbSaveCommand>()
		.PreFetchCount(100)
		.ListenerCount(5)
		.CircuitBreaker(cb =>
		{
			cb.PauseTime = 1.Minutes();
			cb.FailurePercentageThreshold = 10;
		});

	opts.PublishMessage<ReservaInventarioDbSaveCommand>()
		.ToRabbitQueue("queue.inventario.reservar-db-save");

	opts.PublishMessage<OrdemConfirmadaCommand>()
		.ToRabbitQueue("queue.ordem.confirmada");

	opts.PublishMessage<OrdemCanceladaSemAlteracaoCommand>()
		.ToRabbitRoutingKey("exchange.ordem.cancelada", "cancelamento-sem-reserva");

	opts.Policies.AutoApplyTransactions();
});

builder.Services.AddHealthChecks()
	.AddWolverine(tags: new[] { "live", "ready" })
	.AddWolverineListeners(tags: new[] { "ready" });

builder.Services.AddOpenTelemetry()
	.ConfigureResource(resource => resource.AddService(
		serviceName: builder.Configuration["OTEL_SERVICE_NAME"] ?? "eloja-inventario-worker"))
	.WithTracing(tracing => tracing
		.AddAspNetCoreInstrumentation()
		.AddSource("Npgsql")
		.AddOtlpExporter());

builder.Services.AddScoped<IPgDbContext>((c) => new PgDbContext(connectionString));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IInventarioRepository, InventarioRepository>();

builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.AddSingleton<IRedisConnection, RedisConnection>();

var host = builder.Build();

host.UseHttpMetrics();

host.MapHealthChecks("/health/live", new() { Predicate = c => c.Tags.Contains("live") });
host.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });
host.MapMetrics();

host.Run();
