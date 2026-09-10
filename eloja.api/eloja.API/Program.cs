using eloja.IoC;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using Serilog.Sinks.Grafana.Loki;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

string lokiUrl = builder.Configuration["Loki:Url"] ?? "http://loki:3100";

builder.Host.UseSerilog((context, configuration) =>
	configuration
		.MinimumLevel.Information()
		.WriteTo.GrafanaLoki(
			uri: lokiUrl,
			labels: new[] { new LokiLabel { Key = "app", Value = "eloja-api" } },
			textFormatter: new Serilog.Formatting.Json.JsonFormatter()
		)
		.WriteTo.Console()
);

builder.Services.AddOpenTelemetry()
	.ConfigureResource(resource => resource.AddService(
		serviceName: builder.Configuration["OTEL_SERVICE_NAME"] ?? "eloja-api"))
	.WithTracing(tracing => tracing
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
		.AddSource("Npgsql")
		.AddOtlpExporter());

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAnyOrigin", policy =>
	{
		policy.AllowAnyOrigin()
			.AllowAnyMethod()
			.AllowAnyHeader();
	});
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		Scheme = "Bearer",
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Description = "Informe o token JWT: Bearer {token}"
	});

	options.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			Array.Empty<string>()
		}
	});
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAnyOrigin");

app.UseHttpsRedirection();

app.UseHttpMetrics();

app.UseAuthorization();

app.MapControllers();
app.MapMetrics();

app.Run();
