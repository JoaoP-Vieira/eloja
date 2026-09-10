using eloja_ordem_worker.Commands;
using eloja_ordem_worker.Infrastructure;
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

string lokiUrl = builder.Configuration["Loki:Url"] ?? "http://loki:3100";

Log.Logger = new LoggerConfiguration()
	.WriteTo.GrafanaLoki(
		uri: lokiUrl,
		labels: new[] { new LokiLabel { Key = "app", Value = "eloja-ordem-worker" } },
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
		.RetryWithCooldown(300.Milliseconds(), 10.Seconds(), 30.Seconds())
		.Then.MoveToErrorQueue();

	opts.ListenToRabbitQueue("queue.ordem.criada")
		.DeadLetterQueueing(new DeadLetterQueue("queue.ordem.criada.deadletter"))
		.DefaultIncomingMessage<OrdemCriadaCommand>()
		.PreFetchCount(100)
		.ListenerCount(5)
		.CircuitBreaker(cb =>
		{
			cb.PauseTime = 1.Minutes();
			cb.FailurePercentageThreshold = 10;
		});

	opts.ListenToRabbitQueue("queue.ordem.confirmada")
		.DeadLetterQueueing(new DeadLetterQueue("queue.ordem.confirmada.deadletter"))
		.DefaultIncomingMessage<OrdemConfirmadaCommand>()
		.PreFetchCount(100)
		.ListenerCount(5)
		.CircuitBreaker(cb =>
		{
			cb.PauseTime = 1.Minutes();
			cb.FailurePercentageThreshold = 10;
		});

	opts.ListenToRabbitQueue("queue.ordem.cancelada")
		.DeadLetterQueueing(new DeadLetterQueue("queue.ordem.cancelada.deadletter"))
		.DefaultIncomingMessage<OrdemCanceladaCommand>()
		.PreFetchCount(100)
		.ListenerCount(5)
		.CircuitBreaker(cb =>
		{
			cb.PauseTime = 1.Minutes();
			cb.FailurePercentageThreshold = 10;
		});

	opts.Policies.AutoApplyTransactions();
});

builder.Services.AddSingleton<IOrdemStore, OrdemStore>();

builder.Services.AddHealthChecks()
	.AddWolverine(tags: new[] { "live", "ready" })
	.AddWolverineListeners(tags: new[] { "ready" });

builder.Services.AddOpenTelemetry()
	.ConfigureResource(resource => resource.AddService(
		serviceName: builder.Configuration["OTEL_SERVICE_NAME"] ?? "eloja-ordem-worker"))
	.WithTracing(tracing => tracing
		.AddAspNetCoreInstrumentation()
		.AddOtlpExporter());

var host = builder.Build();

host.UseHttpMetrics();

host.MapHealthChecks("/health/live", new() { Predicate = c => c.Tags.Contains("live") });
host.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });
host.MapMetrics();

host.Run();
