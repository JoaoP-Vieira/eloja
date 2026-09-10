using eloja.Services.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Wolverine;

namespace eloja.API.Controllers
{
	[ApiController]
	[Route("ordem")]
	public class OrdemController : Controller
	{
		private readonly IMessageBus _bus;

		public OrdemController(IMessageBus bus)
		{
			_bus = bus;
		}

		[Authorize]
		[HttpPost("create")]
		public async Task<IActionResult> Create([FromBody] CreateOrdemBody body)
		{
			try
			{
				var id = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

				if (string.IsNullOrEmpty(id))
				{
					return Unauthorized(new { message = "Usuário não autenticado" });
				}

				await _bus.InvokeAsync(new CreateOrdemCommand(int.Parse(id), body.pedido));
				return Ok(new { message = "Ordem criada com sucesso" });
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}
	}
}
