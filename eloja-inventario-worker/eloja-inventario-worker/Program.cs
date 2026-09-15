using eloja_inventario_worker.Commands;
using eloja_inventario_worker.Configuration;
using eloja_inventario_worker.Data;
using eloja_inventario_worker.Data.Interfaces;
using eloja_inventario_worker.Infrastructure;
using JasperFx.Core;
using Microsoft.AspNetCore.Builder;
using Npgsql;
using Npgsql.PostgresTypes;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Retry;
using Prometheus;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using StackExchange.Redis;
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

	opts.OnException<BrokenCircuitException>().MoveToErrorQueue();
	opts.OnException<RedisConnectionException>().MoveToErrorQueue();
	opts.OnException<RedisTimeoutException>().MoveToErrorQueue();
	opts.OnException<NpgsqlException>().MoveToErrorQueue();

	opts.OnAnyException()
		.RetryWithCooldown(300.Milliseconds(), 1.Seconds(), 5.Seconds())
		.Then.MoveToErrorQueue();

	opts.ListenToRabbitQueue("queue.inventario.reservar")
		.DeadLetterQueueing(new DeadLetterQueue("queue.inventario.reservar.deadletter"))
		.DefaultIncomingMessage<ReservarInventarioCommand>()
		.PreFetchCount(20)
		.ListenerCount(5);

	opts.ListenToRabbitQueue("queue.inventario.reservar-db-save")
		.DeadLetterQueueing(new DeadLetterQueue("queue.inventario.reservar-db-save.deadletter"))
		.DefaultIncomingMessage<ReservaInventarioDbSaveCommand>()
		.PreFetchCount(20)
		.ListenerCount(5);

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

builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.AddSingleton<IRedisConnection, RedisConnection>();

builder.Services.AddResiliencePipeline("redis", rp => {
	rp.AddCircuitBreaker(new CircuitBreakerStrategyOptions
	{
		FailureRatio = 0.5,
		MinimumThroughput = 10,
		SamplingDuration = TimeSpan.FromSeconds(30),
		BreakDuration = TimeSpan.FromSeconds(30)
	})
	.AddTimeout(TimeSpan.FromSeconds(2));
});

builder.Services.AddResiliencePipeline("postgres", rp => {
	rp.AddRetry(new RetryStrategyOptions
	{
		ShouldHandle = new PredicateBuilder()
				.Handle<NpgsqlException>()
				.Handle<TimeoutException>()
				.Handle<OperationCanceledException>(),
		MaxRetryAttempts = 3,
		Delay = TimeSpan.FromMilliseconds(100),
		BackoffType = DelayBackoffType.Exponential,
		UseJitter = true
	})
		.AddTimeout(TimeSpan.FromSeconds(5))
		.AddCircuitBreaker(new CircuitBreakerStrategyOptions
		{
			FailureRatio = 0.5,
			MinimumThroughput = 10,
			SamplingDuration = TimeSpan.FromSeconds(30),
			BreakDuration = TimeSpan.FromSeconds(30),
			ShouldHandle = new PredicateBuilder()
				.Handle<NpgsqlException>()
				.Handle<TimeoutException>()
		});
});

builder.Services.AddScoped<IPgDbContext>((c) => new PgDbContext(connectionString));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IInventarioRepository, InventarioRepository>();

var host = builder.Build();

host.UseHttpMetrics();

host.MapHealthChecks("/health/live", new() { Predicate = c => c.Tags.Contains("live") });
host.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });
host.MapMetrics();

host.Run();
