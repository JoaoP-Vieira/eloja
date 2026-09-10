namespace eloja_inventario_worker.Commands
{
	public record OrdemCanceladaSemAlteracaoCommand(Guid OrdemId, string motivoCancelamento);
}
