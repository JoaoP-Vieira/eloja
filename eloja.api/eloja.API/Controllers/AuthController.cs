using eloja.Services.Commands;
using eloja.Services.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Wolverine;

namespace eloja.API.Controllers
{
	[ApiController]
	[Route("auth")]
	public class AuthController : Controller
	{
		private readonly IMessageBus _bus;
		private readonly ILogger<AuthController> _logger;

		public AuthController(IMessageBus bus, ILogger<AuthController> logger)
		{
			_bus = bus;
			_logger = logger;
		}

		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] CreateUsuarioCommand body)
		{
			try
			{
				var result = await _bus.InvokeAsync<UsuarioDTO>(body);

				return Ok(result);
			}
			catch (ArgumentException ex)
			{
				_logger.LogError(ex, "Falha durante processo de cadastro de usuário");

				return BadRequest(ex.Message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erro ao processar a solicitação");

				return StatusCode(500, ex.Message);
			}
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginCommand body)
		{
			try
			{
				var result = await _bus.InvokeAsync<AuthResultDTO>(body);

				return Ok(result);
			}
			catch (UnauthorizedAccessException ex)
			{
				_logger.LogError(ex, "Tentativa de acesso não autorizada");

				return Unauthorized(ex.Message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erro ao processar a solicitação");
				return StatusCode(500, ex.Message);
			}
		}

		[Authorize]
		[HttpGet("me")]
		public IActionResult Me()
		{
			var id = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
			var email = User.FindFirstValue(JwtRegisteredClaimNames.Email);
			var firstName = User.FindFirstValue(JwtRegisteredClaimNames.GivenName);
			var lastName = User.FindFirstValue(JwtRegisteredClaimNames.FamilyName);

			return Ok(new { id, email, firstName, lastName });
		}
	}
}
