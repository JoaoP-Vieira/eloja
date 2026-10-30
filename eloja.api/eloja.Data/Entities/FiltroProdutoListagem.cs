namespace eloja.Data.Entities
{
	public record FiltroProdutoListagem(string? Nome, Guid? CategoriaId, int Pagina, int TamanhoPagina, bool Descendente);
}
