using eloja.Data;
using eloja.Data.Interface;
using eloja.Data.Repositories;
using eloja.Services.Auth;
using eloja.Services.Commands;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using System.Text;
using Wolverine;
using Wolverine.RabbitMQ;

namespace eloja.IoC
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddInfrastructure(
				this IServiceCollection services,
				IConfiguration configuration)
		{
			string connectionString = configuration.GetConnectionString("DefaultConnection")
				?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

			services.AddScoped<IPgDbContext>((c) => new PgDbContext(connectionString));
			services.AddScoped<IUnitOfWork, UnitOfWork>();

			services.AddScoped<IUsuarioRepository, UsuarioRepository>();
			services.AddScoped<IProdutoRepository, ProdutoRepository>();
			services.AddScoped<ICategoriaRepository, CategoriaRepository>();

			services.AddSingleton<IPasswordHasher, PasswordHasher>();
			services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

			return services;
		}

		public static IServiceCollection AddServices(
			this IServiceCollection services,
			IConfiguration configuration)
		{
			services.AddWolverine(opts =>
			{
				opts.Discovery.IncludeAssembly(typeof(LoginCommand).Assembly);

				opts.UseRabbitMq(rabbit => {
					rabbit.HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "";
					rabbit.UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "";
					rabbit.Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "";
					rabbit.VirtualHost = Environment.GetEnvironmentVariable("RABBITMQ_VHOST") ?? "/";
					rabbit.Port = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var port) ? port : 5672;
					rabbit.Ssl = new RabbitMQ.Client.SslOption()
					{
						Enabled = bool.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_SSL_ENABLED"), out var sslEnabled) && sslEnabled
					};
				})
				.UseSenderConnectionOnly();

				opts.PublishMessage<CreateOrdemMessage>().ToRabbitExchange("exchange.ordem.criada");

				opts.Policies.AutoApplyTransactions();
			});

			return services;
		}

		public static IServiceCollection AddJwtAuthentication(
			this IServiceCollection services,
			IConfiguration configuration)
		{
			var jwtSection = configuration.GetSection("Jwt");

			services.Configure<JwtSettings>(jwtSection);

			var settings = jwtSection.Get<JwtSettings>()
				?? throw new InvalidOperationException("Jwt configuration section not found.");

			if (string.IsNullOrWhiteSpace(settings.Key))
				throw new InvalidOperationException("Jwt:Key configuration is required.");

			services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(options =>
			{
				options.MapInboundClaims = false;
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidIssuer = settings.Issuer,
					ValidateAudience = true,
					ValidAudience = settings.Audience,
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
					ValidateLifetime = true,
					ClockSkew = TimeSpan.FromMinutes(1)
				};
			});

			services.AddAuthorization();

			return services;
		}
	}
}
