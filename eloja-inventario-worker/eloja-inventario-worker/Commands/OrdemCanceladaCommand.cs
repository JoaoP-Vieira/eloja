using eloja_inventario_worker.Model;

namespace eloja_inventario_worker.Commands
{
	public record OrdemCanceladaCommand(Guid OrdemId, int UsuarioId, ProdutoRollback[] Produtos);
}
