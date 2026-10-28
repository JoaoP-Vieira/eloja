# Tasks: Estrutura Inicial do Carrinho de Compras

**Input**: Design documents from `/specs/001-carrinho-compras/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/carrinho-api.md](./contracts/carrinho-api.md), [quickstart.md](./quickstart.md)

**Tests**: NÃO incluídos. O Princípio VII da constituição proíbe testes automatizados; a validação é `dotnet build`, `docker compose up -d --build` e verificação manual via [quickstart.md](./quickstart.md).

**Organization**: Tarefas agrupadas por user story (US1 adicionar com limite de estoque, US4 segurança/robustez, US2 consultar, US3 alterar/remover).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: US1, US2, US3, US4 (somente nas fases de user story)
- Todos os caminhos são relativos à raiz do repositório (`eloja.api/...`)

## Path Conventions

Solução existente `eloja.api/` com projetos `eloja.API` (controllers/Program), `eloja.Services` (Commands/Queries/DTO), `eloja.Data` (entidades/repositórios/infra) e `eloja.IoC` (DI). Código em C# / .NET 8, namespaces e estilo (tabs, `using` no topo, construtor primário em handlers) iguais aos arquivos existentes (ex.: `eloja.Services/Commands/CreateOrdemCommand.cs`).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Dependência e configuração

- [ ] T001 Adicionar `<PackageReference Include="StackExchange.Redis" Version="3.1.31" />` em `eloja.api/eloja.Data/eloja.Data.csproj` (mesma versão do `eloja-inventario-worker`)
- [ ] T002 [P] Adicionar em `eloja.api/eloja.API/appsettings.json` as seções `"Carrinho": { "TtlDias": 7, "MaxItens": 50 }` e `"Redis": { "Host": "", "Port": 6379, "Password": "" }` (sem segredos reais)
- [ ] T003 [P] Adicionar em `eloja.api/eloja.API/appsettings.Development.json` a seção `"Redis": { "Host": "localhost", "Port": 6379, "Password": "dev" }`, preservando o conteúdo existente
- [ ] T004 [P] Adicionar ao serviço `api` em `docker-compose.yml` as variáveis opcionais `Carrinho__TtlDias: ${CARRINHO_TTL_DIAS:-7}` e `Carrinho__MaxItens: ${CARRINHO_MAX_ITENS:-50}` (as variáveis `Redis__*` já existem) e documentar `CARRINHO_TTL_DIAS`/`CARRINHO_MAX_ITENS` em `.env.example`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infraestrutura Redis, repositórios, entidades, DTO, exceções, DI e esqueleto do controller usados por todas as stories

**⚠️ CRITICAL**: Nenhuma user story pode começar antes desta fase

- [ ] T005 [P] Criar `RedisSettings` (`Host` string, `Port` int = 6379, `Password` string) em `eloja.api/eloja.Data/Configuration/RedisSettings.cs`, espelhando `eloja-inventario-worker/eloja-inventario-worker/Configuration/RedisSettings.cs` (namespace `eloja.Data.Configuration`)
- [ ] T006 [P] Criar a interface `IRedisConnection` com `IDatabase GetDatabase(int db = 0)` em `eloja.api/eloja.Data/Infrastructure/IRedisConnection.cs`
- [ ] T007 Criar `RedisConnection : IRedisConnection, IAsyncDisposable` em `eloja.api/eloja.Data/Infrastructure/RedisConnection.cs` (depende de T005, T006): `ConnectionMultiplexer` via `ConfigurationOptions` com `Host:Port` e senha de `IOptions<RedisSettings>`, `AbortOnConnectFail = false`, `ConnectTimeout = 2000`, `SyncTimeout = 2000`, `AsyncTimeout = 2000`; conexão tardia (lazy) para a API subir mesmo com Redis fora; log via `ILogger` sem expor a senha; modelo em `eloja-inventario-worker/.../Infrastructure/RedisConnection.cs`
- [ ] T008 [P] Criar entidades `Carrinho` (`Versao` int = 1, `AtualizadoEm` DateTime UTC, `Itens` `List<CarrinhoItem>`) e `CarrinhoItem` (`ProdutoId` Guid, `Quantidade` int) em `eloja.api/eloja.Data/Entities/Carrinho.cs`; regras do data-model: "no máximo um item por `produtoId`", "`quantidade` ≥ 1"
- [ ] T009 [P] Criar `CarrinhoSettings` (`TtlDias` int = 7, `MaxItens` int = 50) em `eloja.api/eloja.Services/Carrinho/CarrinhoSettings.cs`
- [ ] T010 [P] Criar exceções em `eloja.api/eloja.Services/Carrinho/CarrinhoExceptions.cs`: `EstoqueInsuficienteException(int quantidadeDisponivel)` (expõe `QuantidadeDisponivel`), `LimiteItensExcedidoException`, `ItemNaoEncontradoNoCarrinhoException` e `CarrinhoIndisponivelException` (conflito de concorrência persistente ou carrinho ilegível); mensagens fixas em português, sem detalhes internos
- [ ] T011 Criar `CarrinhoDTO` (`itens` `CarrinhoItemDTO[]`, `atualizadoEm` DateTime?) e `CarrinhoItemDTO` (`produtoId`, `quantidade`, `quantidadeDisponivel`, `estoqueInsuficiente` = `quantidade > quantidadeDisponivel`) com `static CarrinhoDTO FromCarrinho(Carrinho? carrinho, IReadOnlyDictionary<Guid,int> disponiveis)` em `eloja.api/eloja.Services/DTO/CarrinhoDTO.cs` (depende de T008); carrinho nulo/vazio → `itens: []`, `atualizadoEm: null`; JSON em camelCase como nos demais DTOs
- [ ] T012 [P] Criar `IInventarioRepository` (somente leitura) com `Task<int> SelectQuantidadeDisponivelAsync(Guid produtoId)` e `Task<IReadOnlyDictionary<Guid,int>> SelectQuantidadesDisponiveisAsync(IEnumerable<Guid> produtoIds)` em `eloja.api/eloja.Data/Interface/IInventarioRepository.cs`
- [ ] T013 Implementar `InventarioRepository : IInventarioRepository` em `eloja.api/eloja.Data/Repositories/InventarioRepository.cs` (depende de T007, T012): ler `quantidade-disponivel:{produtoId}` e `quantidade-reservada:{produtoId}` (formato `Guid.ToString()`, Redis DB 0) em lote; "disponível = `max(0, disponivel − reservada)`"; em cache miss de qualquer uma das chaves, consultar via Dapper `select quantidade_disponivel, quantidade_reservada from public.inventario where produto_id = @produtoId` (somente leitura, usando `IPgDbContext` como `ProdutoRepository`) e semear ambas com `StringSet(..., TimeSpan.FromSeconds(1800), When.NotExists)` (mesmo formato/TTL do worker); produto sem linha de inventário → 0; a API NUNCA escreve valores diferentes dos lidos do banco nem incrementa `reservada`
- [ ] T014 [P] Criar `ICarrinhoRepository` em `eloja.api/eloja.Data/Interface/ICarrinhoRepository.cs` (depende de T008) com `Task<Carrinho?> SelectByUsuarioAsync(int usuarioId)`, `Task<Carrinho?> AlterarAsync(int usuarioId, Func<Carrinho, Carrinho> aplicar, TimeSpan ttl)` (retorna o carrinho resultante ou `null` quando ficou sem itens) e `Task DeleteAsync(int usuarioId)`
- [ ] T015 Implementar `CarrinhoRepository : ICarrinhoRepository` em `eloja.api/eloja.Data/Repositories/CarrinhoRepository.cs` (depende de T007, T008, T010, T014): chave `carrinho:{usuarioId}`; JSON via `System.Text.Json` (camelCase, `versao: 1`); `AlterarAsync` faz ler → `aplicar` → gravar com transação condicional (`Condition.KeyNotExists` ou `Condition.StringEqual` do valor lido) e `StringSet(..., ttl)`; condição falhou → repetir até 5 tentativas com backoff curto, depois lançar `CarrinhoIndisponivelException`; carrinho resultante com 0 itens → `KeyDelete` condicional em vez de gravar; exceções lançadas por `aplicar` propagam sem gravar; `SelectByUsuarioAsync` NÃO renova TTL; "TTL renovado somente em alterações"
- [ ] T016 Registrar em `eloja.api/eloja.IoC/DependencyInjection.cs` (método `AddInfrastructure`/`AddServices`): `services.Configure<RedisSettings>(configuration.GetSection("Redis"))`, `services.Configure<CarrinhoSettings>(configuration.GetSection("Carrinho"))`, `AddSingleton<IRedisConnection, RedisConnection>()`, `AddScoped<IInventarioRepository, InventarioRepository>()`, `AddScoped<ICarrinhoRepository, CarrinhoRepository>()` (depende de T007, T009, T013, T015)
- [ ] T017 Criar `eloja.api/eloja.API/Controllers/CarrinhoController.cs` (depende de T010): `[ApiController] [Route("carrinho")] [Authorize]` no nível da classe, construtor com `IMessageBus`, método privado que extrai `int usuarioId` de `User.FindFirstValue(JwtRegisteredClaimNames.Sub)` (ausente/não numérico → `Unauthorized`), e método privado `MapearErro(Exception)` retornando `ErroResponse { codigo, message, quantidadeDisponivel? }` para: `ArgumentException` → 400 `DADOS_INVALIDOS`; `KeyNotFoundException` → 404 `PRODUTO_NAO_ENCONTRADO`; `ItemNaoEncontradoNoCarrinhoException` → 404 `ITEM_NAO_ENCONTRADO`; `EstoqueInsuficienteException` → 409 `ESTOQUE_INSUFICIENTE` com `quantidadeDisponivel`; `LimiteItensExcedidoException` → 409 `LIMITE_ITENS`; mensagens fixas, nunca `ex.Message` de exceções desconhecidas. Nenhuma ação aceita `usuarioId` por rota, query ou corpo

**Checkpoint**: `dotnet build eloja.api/eloja.API.sln` compila; infraestrutura pronta para as stories

---

## Phase 3: User Story 1 - Adicionar item ao carrinho respeitando o estoque (Priority: P1) 🎯 MVP

**Goal**: Usuário autenticado adiciona produto/quantidade; o total no carrinho nunca excede o disponível do inventário.

**Independent Test**: [quickstart.md](./quickstart.md) cenários 1, 2, 3, 9 e 16 (adicionar 3 → 200; +4 → 7; +4 → 409 com `quantidadeDisponivel`; produto inexistente → 404; 51º produto → 409 `LIMITE_ITENS`).

### Implementation for User Story 1

- [ ] T018 [US1] Criar `eloja.api/eloja.Services/Commands/AdicionarItemCarrinhoCommand.cs` com `record AdicionarItemCarrinhoBody(Guid produtoId, int quantidade)`, `record AdicionarItemCarrinhoCommand(int usuarioId, Guid produtoId, int quantidade)` e handler (construtor primário: `ICarrinhoRepository`, `IProdutoRepository`, `IInventarioRepository`, `IOptions<CarrinhoSettings>`) que, nesta ordem: valida `quantidade ≥ 1` e `produtoId != Guid.Empty` (`ArgumentException`); `ExistsByIdAsync` (senão `KeyNotFoundException`); lê disponibilidade (`SelectQuantidadeDisponivelAsync`); chama `AlterarAsync` com TTL `TimeSpan.FromDays(TtlDias)` e callback que calcula `novaQuantidade = existente + informada` em `long`, lança `EstoqueInsuficienteException(disponivel)` se `> disponível`, lança `LimiteItensExcedidoException` se produto novo e já há `MaxItens` (50) itens, atualiza/insere o item (um único item por `produtoId`) e `AtualizadoEm = DateTime.UtcNow`; retorna `CarrinhoDTO` via `CarrinhoDTO.FromCarrinho` usando `SelectQuantidadesDisponiveisAsync` para todos os itens resultantes (depende de T011–T015)
- [ ] T019 [US1] Adicionar a ação `[HttpPost("itens")] Adicionar([FromBody] AdicionarItemCarrinhoBody body)` em `eloja.api/eloja.API/Controllers/CarrinhoController.cs` que invoca `_bus.InvokeAsync<CarrinhoDTO>(new AdicionarItemCarrinhoCommand(usuarioId, body.produtoId, body.quantidade))`, retorna `Ok(dto)` e usa `MapearErro` nos `catch` (depende de T017, T018)
- [ ] T020 [US1] Rodar `dotnet build eloja.api/eloja.API.sln` e validar manualmente os cenários 1, 2, 3, 9 e 16 do quickstart (Swagger/`curl`); confirmar o JSON em `carrinho:{id}` no Redis Insight

**Checkpoint**: US1 funcional e testável sozinha (MVP)

---

## Phase 4: User Story 4 - Proteção contra acesso e dados indevidos (Priority: P1)

**Goal**: Somente o dono acessa/altera o carrinho, com dados válidos; falhas nunca vazam detalhes internos nem deixam alteração parcial; concorrência segura.

**Independent Test**: [quickstart.md](./quickstart.md) cenários 8, 10, 11, 15 e 18.

### Implementation for User Story 4

- [ ] T021 [US4] Em `eloja.api/eloja.API/Program.cs`, configurar `builder.Services.Configure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = ...)` retornando `ErroResponse` 400 `DADOS_INVALIDOS` (mensagem fixa "Dados inválidos.") somente quando `context.HttpContext.Request.Path` começa com `/carrinho`, e o `ValidationProblemDetails` padrão nos demais caminhos (JSON malformado, `quantidade` não inteira/texto, GUID inválido → 400 sem alterar o carrinho)
- [ ] T022 [US4] Estender `MapearErro` em `eloja.api/eloja.API/Controllers/CarrinhoController.cs` (e todos os `catch` das ações existentes): `RedisConnectionException`, `RedisTimeoutException` e `CarrinhoIndisponivelException` → 503 `INDISPONIVEL` ("Serviço temporariamente indisponível. Tente novamente."); qualquer outra exceção → 500 com mensagem fixa genérica (FR-013: sem `ex.Message`, stack ou chaves Redis na resposta)
- [ ] T023 [US4] Em `eloja.api/eloja.Data/Repositories/CarrinhoRepository.cs`, tratar JSON corrompido/`versao` desconhecida: `SelectByUsuarioAsync` e `AlterarAsync` lançam `CarrinhoIndisponivelException` (sem expor o conteúdo) e `DeleteAsync` continua funcionando para o usuário conseguir esvaziar o carrinho
- [ ] T024 [US4] Registrar logs estruturados (Serilog `ILogger`) em `eloja.api/eloja.API/Controllers/CarrinhoController.cs`: `Warning` para rejeições de negócio (estoque/limite/validação) com `usuarioId`, operação e `produtoId`, e `Error` com exceção para falhas de infraestrutura; nunca logar token nem corpo completo
- [ ] T025 [US4] Rodar `dotnet build eloja.api/eloja.API.sln` e validar manualmente os cenários 8, 10, 11, 15 e 18 do quickstart (sem token → 401; `usuarioId` no corpo/query ignorado; ~50 `POST` simultâneos nunca excedem o disponível; `docker compose stop redis` → 503 genérico)

**Checkpoint**: US1 + US4 completas — núcleo seguro do carrinho

---

## Phase 5: User Story 2 - Consultar o carrinho (Priority: P2)

**Goal**: Usuário vê seus itens (ou carrinho vazio); itens com estoque agora insuficiente são sinalizados sem alterar o carrinho nem renovar o TTL.

**Independent Test**: [quickstart.md](./quickstart.md) cenários 4, 5 e 12.

### Implementation for User Story 2

- [ ] T026 [US2] Criar `eloja.api/eloja.Services/Queries/CarrinhoQuery.cs` com `record CarrinhoQuery(int usuarioId)` e handler (`ICarrinhoRepository`, `IInventarioRepository`) que chama `SelectByUsuarioAsync` (sem renovar TTL), busca disponibilidades em lote e retorna `CarrinhoDTO.FromCarrinho(...)`, marcando `estoqueInsuficiente` quando `quantidade > quantidadeDisponivel`, sem modificar o carrinho armazenado (depende de T011–T015)
- [ ] T027 [US2] Adicionar a ação `[HttpGet] Consultar()` em `eloja.api/eloja.API/Controllers/CarrinhoController.cs` retornando `Ok(CarrinhoDTO)` (carrinho vazio → 200 com `itens: []`) e usando `MapearErro` (depende de T017, T026)
- [ ] T028 [US2] Rodar `dotnet build eloja.api/eloja.API.sln` e validar os cenários 4, 5 e 12 do quickstart (TTL inalterado após `GET`; isolamento entre usuários; item com estoque reduzido vem `estoqueInsuficiente: true` e o JSON no Redis permanece igual)

**Checkpoint**: US2 funcional

---

## Phase 6: User Story 3 - Alterar quantidade e remover itens (Priority: P2)

**Goal**: Usuário altera a quantidade de um item existente (respeitando o estoque), remove um item ou esvazia o carrinho.

**Independent Test**: [quickstart.md](./quickstart.md) cenários 6, 7, 13 e 14.

### Implementation for User Story 3

- [ ] T029 [P] [US3] Criar `eloja.api/eloja.Services/Commands/DefinirQuantidadeItemCarrinhoCommand.cs` com `record DefinirQuantidadeItemCarrinhoBody(int quantidade)`, `record DefinirQuantidadeItemCarrinhoCommand(int usuarioId, Guid produtoId, int quantidade)` e handler: "`quantidade` ≥ 1" (zero não remove → `ArgumentException`); lê disponibilidade; `AlterarAsync` com callback que lança `ItemNaoEncontradoNoCarrinhoException` se o item não existe, `EstoqueInsuficienteException(disponivel)` se `quantidade > disponível`, senão define a quantidade e `AtualizadoEm`; retorna `CarrinhoDTO`
- [ ] T030 [P] [US3] Criar `eloja.api/eloja.Services/Commands/RemoverItemCarrinhoCommand.cs` com `record RemoverItemCarrinhoCommand(int usuarioId, Guid produtoId)` e handler: `AlterarAsync` com callback que lança `ItemNaoEncontradoNoCarrinhoException` se ausente e remove o item (último item → o repositório remove a chave); retorna `CarrinhoDTO`
- [ ] T031 [P] [US3] Criar `eloja.api/eloja.Services/Commands/EsvaziarCarrinhoCommand.cs` com `record EsvaziarCarrinhoCommand(int usuarioId)` e handler que chama `ICarrinhoRepository.DeleteAsync` (idempotente)
- [ ] T032 [US3] Adicionar em `eloja.api/eloja.API/Controllers/CarrinhoController.cs` as ações `[HttpPut("itens/{produtoId}")] DefinirQuantidade(Guid produtoId, [FromBody] DefinirQuantidadeItemCarrinhoBody body)` → 200, `[HttpDelete("itens/{produtoId}")] Remover(Guid produtoId)` → 200 e `[HttpDelete] Esvaziar()` → 204, todas com `MapearErro` (depende de T017, T029, T030, T031)
- [ ] T033 [US3] Rodar `dotnet build eloja.api/eloja.API.sln` e validar os cenários 6, 7, 13 e 14 do quickstart

**Checkpoint**: Todas as stories funcionais

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Observabilidade (Princípio VIII), documentação e validação final

- [ ] T034 Adicionar em `eloja.api/eloja.API/Controllers/CarrinhoController.cs` o contador prometheus-net `carrinho_operacoes_total` com labels `operacao` (`consultar|adicionar|definir|remover|esvaziar`) e `resultado` (`sucesso|estoque_insuficiente|limite_itens|invalido|nao_encontrado|indisponivel|erro`), incrementado em cada ação (exposto por `app.MapMetrics()` já existente)
- [ ] T035 Criar no Grafana (UI, `http://localhost:3000`) um painel "Carrinho" com `sum by (operacao, resultado) (rate(carrinho_operacoes_total[5m]))` e um painel de logs Loki `{app="eloja-api"} |= "carrinho"`; documentar as consultas na seção do carrinho do `README.md` (não há provisionamento de dashboards versionado no repositório)
- [ ] T036 [P] Atualizar `README.md`: seção "Carrinho de compras" com rotas, variáveis `Carrinho__TtlDias`/`Carrinho__MaxItens`, uso do Redis (`carrinho:{usuarioId}`) e a observação de que a API lê o espelho Redis do inventário (Constitution Check)
- [ ] T037 [P] Adicionar exemplos das 5 rotas (com `Authorization: Bearer {{token}}`) em `eloja.api/eloja.API/eloja.api.http`
- [ ] T038 Executar `dotnet build eloja.api/eloja.API.sln`, `docker compose up -d --build` e percorrer todos os 18 cenários de [quickstart.md](./quickstart.md); confirmar que nenhum arquivo de teste foi criado (Princípio VII) e que não há segredos commitados

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências; T001 antes de qualquer código que use Redis
- **Foundational (Phase 2)**: depende da Phase 1; BLOQUEIA todas as stories
- **US1 (Phase 3)** e **US4 (Phase 4)**: ambas P1, dependem só da Phase 2; US4 refina o mesmo controller/repositório, então executar após US1 (T022–T024 editam arquivos de T017/T015/T019)
- **US2 (Phase 5)** e **US3 (Phase 6)**: P2, dependem só da Phase 2 (independentes entre si e de US1/US4); T027/T032 editam o mesmo controller → sequenciar
- **Polish (Phase 7)**: depende das stories desejadas

### Task Dependencies (resumo)

- T007 ← T005, T006 · T011 ← T008 · T013 ← T007, T012 · T014 ← T008 · T015 ← T007, T008, T010, T014 · T016 ← T007, T009, T013, T015 · T017 ← T010
- T018 ← T011–T015 · T019 ← T017, T018 · T021–T024 ← T019 · T026 ← T011–T015 · T027 ← T017, T026 · T029–T031 ← T011–T015 · T032 ← T017, T029–T031 · T034 ← T032

### Parallel Opportunities

- Phase 1: T002, T003, T004 em paralelo (após/ao lado de T001)
- Phase 2: T005, T006, T008, T009, T010, T012 em paralelo; depois T007/T011/T014 e então T013/T015
- US3: T029, T030, T031 em paralelo (arquivos distintos)
- Polish: T036 e T037 em paralelo
- Com mais de um desenvolvedor: após a Phase 2, US2 (T026) e os commands de US3 (T029–T031) podem andar em paralelo com US1; as edições do `CarrinhoController.cs` devem ser serializadas

### Parallel Example: Phase 2

```text
Paralelo: T005 RedisSettings | T006 IRedisConnection | T008 Entidades | T009 CarrinhoSettings | T010 Exceções | T012 IInventarioRepository
Depois:   T007 RedisConnection | T011 CarrinhoDTO | T014 ICarrinhoRepository
Depois:   T013 InventarioRepository | T015 CarrinhoRepository → T016 DI → T017 Controller
```

---

## Implementation Strategy

### MVP First (US1)

1. Phase 1 → Phase 2 → Phase 3 (US1) → validar (T020). Entrega: adicionar ao carrinho com limite de estoque, JSON no Redis.
2. Adicionar US4 (Phase 4) antes de expor a rota fora de desenvolvimento: sem ela o contrato de erros/503 e o hardening ainda não estão completos.

### Incremental Delivery

1. Setup + Foundational → base pronta
2. US1 → MVP (adicionar com estoque)
3. US4 → segurança/robustez (P1)
4. US2 → consulta com sinalização de estoque insuficiente
5. US3 → alterar/remover/esvaziar
6. Polish → métricas, painel Grafana, README, `.http`, validação completa

---

## Notes

- Nenhum teste automatizado (Princípio VII); não criar projetos/arquivos de teste
- `[P]` = arquivos diferentes e sem dependência incompleta
- Commit sugerido ao final de cada fase/checkpoint
- Cada story deve ser validada em seu checkpoint antes de seguir
