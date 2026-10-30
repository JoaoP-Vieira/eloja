# Research: Consulta de Produtos Paginada

Não havia itens `NEEDS CLARIFICATION` no Technical Context; as decisões abaixo consolidam as escolhas de design.

## 1. Fonte do saldo de estoque

- **Decision**: `LEFT JOIN` somente-leitura em `public.inventario` (agregado por `produto_id`) na própria consulta de listagem da `eloja.api`. Saldo efetivo = `sum(quantidade_disponivel - quantidade_reservada)`; `temEstoque = saldo > 0`; ausência de registro = sem estoque.
- **Rationale**: O inventário já existe no Postgres compartilhado (`db/inventario.sql`). Precisamos ordenar e paginar usando o saldo; fazer isso no banco é o único jeito de manter a ordem e os totais corretos entre páginas. A semântica `disponivel - reservada` é a mesma usada pelo worker (`ReservarInventarioCommand`: `restante = disponivel - reservada - solicitada`).
- **Alternatives considered**:
  - Redis (`quantidade-disponivel:{id}` / `quantidade-reservada:{id}`): descartado; as chaves são criadas sob demanda na reserva e expiram em 30 min, logo não são fonte autêntica do saldo.
  - Chamada síncrona ao `inventario-worker`: descartado; exigiria endpoint HTTP novo e violaria o Princípio I.
  - Read model próprio por eventos: descartado por escopo (aprovado pelo usuário a abordagem do JOIN; desvio registrado em Complexity Tracking).
- O `GROUP BY produto_id` protege contra múltiplas linhas de inventário por produto (não há `UNIQUE(produto_id)`), evitando produtos duplicados na listagem.

## 2. Paginação

- **Decision**: paginação por `LIMIT/OFFSET` com `pagina` (base 1, padrão 1) e `tamanhoPagina` (padrão 10, máximo 100). Duas consultas: página (`LIMIT/OFFSET`) e `count(*)` com os mesmos filtros. Valores < 1 ou `tamanhoPagina` > 100 resultam em `400` com mensagem clara (FR-003/FR-014). Página além do total retorna lista vazia com totais corretos.
- **Rationale**: Atende ao requisito de total de itens/páginas e navegação direta por página; escala bem para ~10.000 produtos.
- **Alternatives considered**: keyset/cursor (não fornece total de páginas nem salto para página arbitrária); `count(*) over()` na mesma consulta (retorna total 0 quando a página está fora do intervalo, perdendo a informação exigida na edge case).
- Limitar silenciosamente o tamanho acima de 100 foi descartado: rejeitar é mais previsível para o consumidor.

## 3. Pesquisa por nome

- **Decision**: `position(lower(@nome) in lower(p.nome)) > 0` com o termo previamente `Trim()`; termo vazio/espaços = sem filtro.
- **Rationale**: "contém", insensível a maiúsculas/minúsculas e sem interpretar `%`/`_` como curingas (texto literal, conforme edge case), sem necessidade de escapar caracteres. Parametrizado (sem concatenação de SQL).
- **Alternatives considered**: `ILIKE '%termo%'` (exige escapar `%`, `_`, `\`); full-text/`pg_trgm` (extensão nova, desnecessária para a escala).

## 4. Filtro por categoria

- **Decision**: parâmetro opcional `categoriaId` (Guid) com `p.categoria_id = @categoriaId`. Guid malformado é rejeitado pela validação de binding do ASP.NET (`400`); categoria inexistente retorna lista vazia. Produtos sem categoria aparecem sem filtro e não aparecem com filtro.
- **Rationale**: Mapeia direto os requisitos FR-006/FR-007 e as edge cases; não exige consulta prévia à tabela de categorias.

## 5. Ordenação

- **Decision**: parâmetro `direcao` (`asc` | `desc`, padrão `asc`, insensível a maiúsculas). `ORDER BY (saldo > 0) DESC, lower(p.nome) {ASC|DESC}, p.id` — o fragmento `ASC|DESC` vem de uma lista branca no código (nunca do texto do usuário). O `p.id` fixa o desempate (FR-013). Valor diferente de `asc`/`desc` → `400`.
- **Rationale**: Garante sem-saldo sempre no final em qualquer direção e ordem alfabética dentro de cada grupo (FR-011/FR-012).
- **Alternatives considered**: ordenar em memória após buscar a página (quebraria a paginação); `ORDER BY p.nome` sem `lower` (ordem sensível a maiúsculas/minúsculas e à collation do banco).

## 6. Contrato HTTP

- **Decision**: `GET /produto` com `[Authorize]`, query string `nome`, `categoriaId`, `pagina`, `tamanhoPagina`, `direcao`. Resposta `200` com envelope `{ itens, pagina, tamanhoPagina, totalItens, totalPaginas }`; cada item reaproveita o formato de `ProdutoDTO` e acrescenta `temEstoque`. Erros: `400` (parâmetros inválidos, mensagem em português seguindo o padrão do `ProdutoController`), `401` (sem autenticação), `500` (falha inesperada).
- **Rationale**: Mantém a convenção do controller existente (`try/catch` → `ArgumentException` = 400) e não conflita com `GET /produto/{id}`.
- Nomes de parâmetros em português e camelCase, coerentes com os contratos existentes (`categoriaId`, `precoUnitario`).

## 7. Desempenho e índices

- **Decision**: manter a consulta simples; adicionar (opcional) `create index inventario_produto_id_idx on public.inventario (produto_id)` em `db/inventario.sql` para o agregado/JOIN. Scripts de `db/` só rodam em volumes novos; ambientes existentes precisam aplicar o índice manualmente.
- **Rationale**: Para ~10.000 produtos a consulta atende SC-001 mesmo sem índice; o índice reduz o custo do JOIN sem nova tecnologia. Índice em `lower(nome)`/`pg_trgm` não é necessário nessa escala.

## 8. Observabilidade

- **Decision**: nenhum componente novo; handler emite log estruturado (`ILogger`) com filtros e totais (sem dados sensíveis); traces ASP.NET/Npgsql e métricas Prometheus já cobrem a rota.
- **Rationale**: Princípio VIII.

## 9. Validação

- **Decision**: sem testes automatizados (Princípio VII); validar com `dotnet build` e cenários manuais descritos em [quickstart.md](./quickstart.md).
