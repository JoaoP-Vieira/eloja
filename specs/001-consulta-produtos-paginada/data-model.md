# Data Model: Consulta de Produtos Paginada

A feature é somente leitura: **nenhuma tabela, coluna ou entidade persistida é criada ou alterada** (exceto o índice opcional descrito em [research.md](./research.md), item 7).

## Tabelas consultadas (existentes)

| Tabela | Dono | Uso nesta feature | Colunas lidas |
|--------|------|-------------------|---------------|
| `public.produto` | `eloja.api` | Itens listados | `id`, `nome`, `descricao`, `categoria_id`, `preco_unitario` |
| `public.categoria` | `eloja.api` | Dados da categoria do produto (`LEFT JOIN`) | `id`, `nome`, `descricao` |
| `public.inventario` | `eloja-inventario-worker` | Saldo (somente leitura; desvio justificado no plano) | `produto_id`, `quantidade_disponivel`, `quantidade_reservada` |

Relacionamentos: `produto.categoria_id → categoria.id` (nullable); `inventario.produto_id → produto.id` (pode haver 0..N linhas por produto).

## Regra derivada: saldo e disponibilidade

```text
saldo(produto)    = sum(quantidade_disponivel - quantidade_reservada)   -- 0 se não há registro
temEstoque        = saldo > 0
```

## Projeção de leitura (camada Data)

**`ProdutoListagem`** (`eloja.Data/Entities/ProdutoListagem.cs`)

| Campo | Tipo | Origem |
|-------|------|--------|
| `Id` | `Guid` | `produto.id` |
| `Nome` | `string` | `produto.nome` |
| `Descricao` | `string` | `produto.descricao` |
| `PrecoUnitario` | `decimal` | `produto.preco_unitario` |
| `CategoriaId` | `Guid?` | `produto.categoria_id` |
| `CategoriaNome` | `string?` | `categoria.nome` |
| `CategoriaDescricao` | `string?` | `categoria.descricao` |
| `TemEstoque` | `bool` | `saldo > 0` |

**Filtro de consulta** (parâmetros do repositório): `Nome?` (string já aparada), `CategoriaId?` (Guid), `Pagina` (≥ 1), `TamanhoPagina` (1–100), `Descendente` (bool).

## DTOs (camada Services)

**`ProdutoListaItemDTO`** — mesmo formato de `ProdutoDTO` (`id`, `nome`, `descricao`, `categoria { id, nome, descricao }`, `precoUnitario`) + `temEstoque: bool`. Categoria ausente → objeto de categoria vazio (mesmo comportamento de `ProdutoDTO.FromEntity`).

**`PaginaDTO<T>`**

| Campo | Tipo | Regra |
|-------|------|-------|
| `itens` | `T[]` | itens da página (vazio se fora do intervalo) |
| `pagina` | `int` | página solicitada (base 1) |
| `tamanhoPagina` | `int` | tamanho efetivo |
| `totalItens` | `int` | itens que atendem aos filtros |
| `totalPaginas` | `int` | `ceil(totalItens / tamanhoPagina)`; 0 se não há itens |

## Query (camada Services)

`ProdutosPaginadosQuery(string? Nome, Guid? CategoriaId, int? Pagina, int? TamanhoPagina, string? Direcao)`

Validações (→ `ArgumentException` → HTTP 400):

| Campo | Regra | Padrão |
|-------|-------|--------|
| `Pagina` | ≥ 1 | 1 |
| `TamanhoPagina` | 1 ≤ valor ≤ 100 | 10 |
| `Direcao` | `asc` ou `desc` (insensível a caixa) | `asc` |
| `Nome` | aparado; vazio = sem filtro | — |

## Ordenação

1. Produtos com `temEstoque = true` primeiro; `false` por último (sempre).
2. Dentro de cada grupo: `lower(nome)` na direção escolhida.
3. Desempate: `id`.
