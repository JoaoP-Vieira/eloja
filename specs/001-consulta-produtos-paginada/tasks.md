# Tasks: Consulta de Produtos Paginada

**Input**: Design documents from `/specs/001-consulta-produtos-paginada/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/produto-listagem.openapi.yaml](./contracts/produto-listagem.openapi.yaml), [quickstart.md](./quickstart.md)

**Tests**: NÃO incluídos. A constituição (Princípio VII) proíbe tarefas de teste; a validação é por `dotnet build` e verificação manual (quickstart).

**Organization**: Tarefas agrupadas por user story. As histórias compartilham os mesmos arquivos (repositório, query e controller), então são incrementais e devem ser executadas em sequência (US1 → US2 → US3 → US4).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- **[Story]**: história de usuário da spec (US1..US4)

## Path Conventions

Solução existente `eloja.api/` (camadas conforme Princípio IX): `eloja.API/` (controller), `eloja.Services/` (Queries, DTO), `eloja.Data/` (Entities, Interface, Repositories). Namespaces: `eloja.API.Controllers`, `eloja.Services.Queries`, `eloja.Services.DTO`, `eloja.Data.Entities`, `eloja.Data.Interface`, `eloja.Data.Repositories`. Indentação com tabs e arquivos com BOM, como nos arquivos existentes.

---

## Phase 1: Setup

- [X] T001 Confirmar baseline executando `dotnet build eloja.api/eloja.API.sln` e garantir que compila sem erros antes de qualquer alteração

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Tipos compartilhados por todas as histórias (projeção de leitura, DTOs, filtro e assinatura do repositório).

**⚠️ CRITICAL**: Nenhuma história pode começar antes desta fase.

- [X] T002 [P] Criar a projeção de leitura `ProdutoListagem` em `eloja.api/eloja.Data/Entities/ProdutoListagem.cs` com as propriedades: `Guid Id`, `string Nome` (string.Empty), `string Descricao` (string.Empty), `decimal PrecoUnitario`, `Guid? CategoriaId`, `string? CategoriaNome`, `string? CategoriaDescricao`, `bool TemEstoque` (derivado de `saldo > 0`)
- [X] T003 [P] Criar o filtro de consulta `FiltroProdutoListagem` (record) em `eloja.api/eloja.Data/Entities/FiltroProdutoListagem.cs` com: `string? Nome` (já aparado; nulo = sem filtro), `Guid? CategoriaId`, `int Pagina` (≥ 1), `int TamanhoPagina` (1 ≤ valor ≤ 100), `bool Descendente`
- [X] T004 [P] Criar o envelope genérico `PaginaDTO<T>` em `eloja.api/eloja.Services/DTO/PaginaDTO.cs` com `T[] Itens`, `int Pagina`, `int TamanhoPagina`, `int TotalItens`, `int TotalPaginas` (`TotalPaginas` = `ceil(totalItens / tamanhoPagina)`; 0 se não há itens)
- [X] T005 [P] Criar `ProdutoListaItemDTO : ProdutoDTO` em `eloja.api/eloja.Services/DTO/ProdutoListaItemDTO.cs` adicionando `bool TemEstoque` e um método estático `FromListagem(ProdutoListagem)` que preenche `Id`, `Nome`, `Descricao`, `PrecoUnitario`, `Categoria` (via `CategoriaDTO`; categoria ausente → `new CategoriaDTO()`, mesmo comportamento de `ProdutoDTO.FromEntity`) e `TemEstoque`
- [X] T006 Adicionar a `IProdutoRepository` em `eloja.api/eloja.Data/Interface/IProdutoRepository.cs` os métodos `Task<IEnumerable<ProdutoListagem>> SelectPaginadoAsync(FiltroProdutoListagem filtro)` e `Task<int> CountAsync(FiltroProdutoListagem filtro)` (depende de T002 e T003)

**Checkpoint**: Fundação pronta; histórias podem começar.

---

## Phase 3: User Story 1 - Listar produtos de forma paginada (Priority: P1) 🎯 MVP

**Goal**: `GET /produto` retorna uma página de produtos com `pagina`, `tamanhoPagina`, `totalItens`, `totalPaginas` e `temEstoque` em cada item.

**Independent Test**: Com mais produtos do que cabem em uma página, `GET /produto` (e `?pagina=3&tamanhoPagina=10`) retorna só os itens da página, totais corretos e `temEstoque` coerente com o inventário (cenários 1, 2, 3, 12, 13 e 14 do [quickstart.md](./quickstart.md)).

- [X] T007 [US1] Implementar `SelectPaginadoAsync` e `CountAsync` em `eloja.api/eloja.Data/Repositories/ProdutoRepository.cs` (Dapper, consultas parametrizadas como constantes, seguindo o estilo de `SELECT_BY_ID_QUERY`): `public.produto p LEFT JOIN public.categoria c ON c.id = p.categoria_id LEFT JOIN (select produto_id, sum(quantidade_disponivel - quantidade_reservada) saldo from public.inventario group by produto_id) i ON i.produto_id = p.id`; `TemEstoque = coalesce(i.saldo, 0) > 0`; aliases com aspas duplas mapeando para `ProdutoListagem` (`"Id"`, `"Nome"`, `"Descricao"`, `"PrecoUnitario"`, `"CategoriaId"`, `"CategoriaNome"`, `"CategoriaDescricao"`, `"TemEstoque"`); paginação `LIMIT @tamanhoPagina OFFSET (@pagina - 1) * @tamanhoPagina`; ordenação provisória `ORDER BY lower(p.nome), p.id` (a ordenação final vem na US4); `CountAsync` faz `select count(*)` sobre o mesmo conjunto (sem LIMIT). Acesso a `public.inventario` estritamente de leitura (desvio justificado no plan.md)
- [X] T008 [US1] Criar `eloja.api/eloja.Services/Queries/ProdutosPaginadosQuery.cs` com `public record ProdutosPaginadosQuery(string? Nome, Guid? CategoriaId, int? Pagina, int? TamanhoPagina, string? Direcao)` e `ProdutosPaginadosQueryHandler(IProdutoRepository, ILogger<ProdutosPaginadosQueryHandler>)` com `Handle(ProdutosPaginadosQuery, CancellationToken)` retornando `PaginaDTO<ProdutoListaItemDTO>` (padrão do `ProdutoByIdQueryHandler`). Nesta história: `Pagina` padrão 1 e deve ser ≥ 1; `TamanhoPagina` padrão 10 e deve estar entre 1 e 100; violações → `ArgumentException` com mensagens como "pagina deve ser maior ou igual a 1." e "tamanhoPagina deve estar entre 1 e 100."; chamar `CountAsync` e `SelectPaginadoAsync`, mapear com `ProdutoListaItemDTO.FromListagem`, calcular `TotalPaginas`, e registrar log estruturado (`LogInformation`) com página, tamanho e total. Página além do total retorna `Itens` vazio com totais corretos (sem erro)
- [X] T009 [US1] Adicionar em `eloja.api/eloja.API/Controllers/ProdutoController.cs` a ação `[Authorize] [HttpGet] GetProdutos([FromQuery] int? pagina, [FromQuery] int? tamanhoPagina)` (rota `GET /produto`, sem conflito com `{id}`) que chama `_bus.InvokeAsync<PaginaDTO<ProdutoListaItemDTO>>(new ProdutosPaginadosQuery(null, null, pagina, tamanhoPagina, null))`, retorna `Ok(...)`, `ArgumentException` → `StatusCode(400, $"Erro ao listar produtos: {ex.Message}")` e `Exception` → `StatusCode(500, $"Erro ao listar produtos: {ex.Message}")`, no mesmo padrão das demais ações

**Checkpoint**: US1 funcional e demonstrável (MVP).

---

## Phase 4: User Story 2 - Pesquisar produtos por nome (Priority: P2)

**Goal**: filtrar a listagem pelos produtos cujo nome contém o termo, mantendo paginação e `temEstoque`.

**Independent Test**: `GET /produto?nome=camiseta` retorna só nomes contendo "camiseta" em qualquer caixa; termo sem correspondência retorna `itens` vazio, `totalItens=0`; `nome=%25` é literal (cenários 4, 5 e 6 do quickstart).

- [X] T010 [US2] Em `eloja.api/eloja.Data/Repositories/ProdutoRepository.cs`, adicionar às consultas de página e de contagem a condição `(@nome is null or position(lower(@nome) in lower(p.nome)) > 0)` (texto literal, sem curingas; parâmetro nunca concatenado ao SQL) e passar `filtro.Nome` como parâmetro `nome`
- [X] T011 [US2] Em `eloja.api/eloja.Services/Queries/ProdutosPaginadosQuery.cs`, aplicar `Trim()` ao `Nome` e tratar vazio ou só espaços como `null` (sem filtro) antes de montar o `FiltroProdutoListagem`
- [X] T012 [US2] Em `eloja.api/eloja.API/Controllers/ProdutoController.cs`, adicionar o parâmetro `[FromQuery] string? nome` a `GetProdutos` e repassá-lo à `ProdutosPaginadosQuery`

**Checkpoint**: US1 e US2 funcionam; totais refletem apenas os itens que correspondem à pesquisa.

---

## Phase 5: User Story 3 - Filtrar produtos por categoria (Priority: P2)

**Goal**: filtrar a listagem por uma categoria, combinável com a pesquisa por nome.

**Independent Test**: `GET /produto?categoriaId=<id>` retorna só produtos da categoria; categoria inexistente/sem produtos → vazio; com `nome` + `categoriaId` aplica ambos; `categoriaId=abc` → 400 (cenários 7, 8 e 13 do quickstart).

- [X] T013 [US3] Em `eloja.api/eloja.Data/Repositories/ProdutoRepository.cs`, adicionar às consultas de página e de contagem a condição `(@categoriaId is null or p.categoria_id = @categoriaId)` e passar `filtro.CategoriaId` como parâmetro `categoriaId` (produtos sem categoria só aparecem sem o filtro)
- [X] T014 [US3] Em `eloja.api/eloja.Services/Queries/ProdutosPaginadosQuery.cs`, repassar `CategoriaId` ao `FiltroProdutoListagem`
- [X] T015 [US3] Em `eloja.api/eloja.API/Controllers/ProdutoController.cs`, adicionar o parâmetro `[FromQuery] Guid? categoriaId` a `GetProdutos` e repassá-lo à query (Guid malformado é rejeitado com 400 pela validação automática do `[ApiController]`)

**Checkpoint**: US1–US3 funcionam, inclusive combinadas.

---

## Phase 6: User Story 4 - Ordenar alfabeticamente com sem-saldo ao final (Priority: P2)

**Goal**: ordenação por nome crescente/decrescente, com produtos sem saldo sempre depois dos com saldo.

**Independent Test**: com produtos com e sem saldo, `direcao=asc` e `direcao=desc` retornam com saldo primeiro (nome na direção escolhida) e depois sem saldo (mesma direção), em todas as páginas; `direcao=x` → 400 (cenários 9, 10, 11 e 13 do quickstart).

- [X] T016 [US4] Em `eloja.api/eloja.Data/Repositories/ProdutoRepository.cs`, substituir a ordenação provisória da consulta de página por `ORDER BY (coalesce(i.saldo, 0) > 0) DESC, lower(p.nome) {ASC|DESC}, p.id`; o fragmento `ASC`/`DESC` deve ser escolhido no código a partir de `filtro.Descendente` entre duas constantes de lista branca (nunca interpolando texto vindo do usuário)
- [X] T017 [US4] Em `eloja.api/eloja.Services/Queries/ProdutosPaginadosQuery.cs`, validar `Direcao`: padrão `asc` quando nulo/vazio; aceitar somente `asc` ou `desc` (insensível a maiúsculas/minúsculas), caso contrário `ArgumentException("direcao deve ser 'asc' ou 'desc'.")`; definir `Descendente` no `FiltroProdutoListagem`
- [X] T018 [US4] Em `eloja.api/eloja.API/Controllers/ProdutoController.cs`, adicionar o parâmetro `[FromQuery] string? direcao` a `GetProdutos` e repassá-lo à query

**Checkpoint**: todas as histórias funcionam de forma independente e combinada.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T019 [P] Adicionar índice opcional em `db/inventario.sql`: `create index inventario_produto_id_idx on public.inventario (produto_id);` (script só roda em volumes novos; ver research.md item 7)
- [X] T020 [P] Adicionar exemplos da rota (`GET {{eloja.api_HostAddress}}/produto` com `pagina`, `tamanhoPagina`, `nome`, `categoriaId`, `direcao` e header `Authorization: Bearer {{token}}`) em `eloja.api/eloja.API/eloja.api.http`
- [X] T021 Executar `dotnet build eloja.api/eloja.API.sln` e corrigir quaisquer erros ou warnings novos
- [X] T022 Subir a stack com `docker compose up -d --build` e executar os 15 cenários de [quickstart.md](./quickstart.md) via Swagger (`http://localhost:8080/swagger`), confirmando também que `GET /produto/{id}` segue inalterado e que logs/traces aparecem no Grafana (Loki/Tempo)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** → **Foundational (Phase 2)** → **US1 (Phase 3)** → **US2** → **US3** → **US4** → **Polish**
- As histórias editam os mesmos três arquivos (`ProdutoRepository.cs`, `ProdutosPaginadosQuery.cs`, `ProdutoController.cs`); por isso a ordem é sequencial, embora cada incremento seja testável isoladamente.
- Dentro de cada história: repositório → query/handler → controller.

### Parallel Opportunities

- Foundational: T002, T003, T004 e T005 são arquivos distintos e podem rodar em paralelo; T006 depende de T002 e T003.
- Polish: T019 e T020 são independentes entre si e das demais.

### Parallel Example: Foundational

```text
T002 ProdutoListagem.cs   |  T003 FiltroProdutoListagem.cs  |  T004 PaginaDTO.cs  |  T005 ProdutoListaItemDTO.cs
→ depois T006 IProdutoRepository.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Concluir Setup e Foundational (T001–T006).
2. Concluir US1 (T007–T009), compilar e validar os cenários 1, 2, 3, 12, 13 e 14.
3. Demonstrar `GET /produto` paginado com `temEstoque`.

### Incremental Delivery

1. US2 (pesquisa por nome) → validar cenários 4–6.
2. US3 (categoria) → validar cenários 7–8.
3. US4 (ordenação + sem-saldo ao final) → validar cenários 9–11.
4. Polish: índice, `.http`, build final e quickstart completo.

---

## Notes

- Sem tarefas de teste (Princípio VII); validação = build + quickstart.
- Camadas (Princípio IX): SQL/projeções só em `eloja.Data`; Query/DTOs em `eloja.Services`; controller apenas expõe o endpoint via `IMessageBus`; `eloja.IoC` não muda (`IProdutoRepository` já registrado).
- `eloja.api` apenas lê `public.inventario`; nenhuma escrita ou DDL nessa tabela pela API (exceto o índice opcional em `db/`).
