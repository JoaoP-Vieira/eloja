# Implementation Plan: Estrutura Inicial do Carrinho de Compras

**Branch**: `feat/001-carrinho-compras` | **Date**: 2026-10-27 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-carrinho-compras/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Adicionar à `eloja.api` um carrinho de compras por usuário autenticado, armazenado no Redis como um documento JSON (chave `carrinho:{usuarioId}`, TTL de 7 dias renovado só em alterações). Cinco rotas sob `/carrinho` (consultar, adicionar, definir quantidade, remover item, esvaziar) seguem o padrão existente de Controller → Wolverine `IMessageBus` → Command/Query handler → Repository. A quantidade disponível vem do espelho Redis do inventário mantido pelo `eloja-inventario-worker` (`quantidade-disponivel:{produtoId}` − `quantidade-reservada:{produtoId}`), com fallback de leitura somente-leitura no PostgreSQL em caso de cache miss. Toda alteração é atômica e protegida contra concorrência por transação condicional do Redis (compare-and-set com retentativas limitadas).

## Technical Context

**Language/Version**: C# / .NET 8 (`net8.0`), mesmo dos demais projetos de `eloja.api`

**Primary Dependencies**: ASP.NET Core, WolverineFX 5.41.0 (CQRS in-process), Dapper 2.1.79 + Npgsql 10.0.3 (já em `eloja.Data`), **StackExchange.Redis 3.1.31** (novo em `eloja.Data`, mesma versão do `eloja-inventario-worker`), prometheus-net.AspNetCore, Serilog/OpenTelemetry (já existentes)

**Storage**: Redis 7.4 (já no `docker-compose.yml`, `--appendonly yes`) para o carrinho (JSON) e leitura do espelho de inventário; PostgreSQL somente-leitura (`public.inventario`) como fallback de cache miss; `public.produto` (já da API) para validar existência do produto

**Testing**: N/A — Princípio VII da constituição (sem testes automatizados). Validação por `dotnet build`, `docker compose up -d --build` e verificação manual (Swagger / `quickstart.md`)

**Target Platform**: Container Linux (Docker Compose), serviço `api` existente

**Project Type**: web-service (API REST) dentro da solução `eloja.api` (API + Data + Services + IoC)

**Performance Goals**: operação de carrinho (adicionar + consultar) percebida em < 2 s em uso normal (SC-003); alvo interno p95 < 200 ms por rota com Redis local

**Constraints**: id do usuário vem exclusivamente do claim `sub` do JWT; alterações atômicas; no máximo 50 produtos distintos por carrinho (configurável); TTL padrão 7 dias (configurável); nenhuma exposição de detalhes internos em erros (FR-013)

**Scale/Scope**: 5 rotas, 1 documento JSON por usuário (≤ 50 itens, poucos KB); sem rate limiting nesta versão

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Avaliação | Status |
|-----------|-----------|--------|
| I. Microserviços / eventos | O carrinho vive 100% na `eloja.api` (nenhum novo serviço). Não há chamada síncrona API→worker. **Exceção**: a API lê chaves Redis do inventário e, em cache miss, a tabela `inventario` (ver Complexity Tracking). Sem eventos novos. | ⚠️ Justificado |
| II. Stack C#/.NET | Somente C#/.NET 8; novo pacote NuGet `StackExchange.Redis` (já usado no repo). Observabilidade conforme VIII. | ✅ |
| III. Estrutura de pastas | Sem novo projeto/solução: código em `eloja.api/{eloja.API,eloja.Services,eloja.Data,eloja.IoC}`. | ✅ |
| IV. DI + CQRS | Escritas como Commands (`Commands/`), leitura como Query (`Queries/`), handlers Wolverine, dependências por interface (`ICarrinhoRepository`, `IInventarioRepository`, `IRedisConnection`), registro em `eloja.IoC/DependencyInjection.cs`. Replica nomenclatura/pastas existentes. | ✅ |
| V. WolverineFX / RabbitMQ | Handlers invocados com `IMessageBus.InvokeAsync` (padrão atual). Nenhuma mensagem RabbitMQ nova. | ✅ |
| VI. Persistência poliglota | Carrinho em Redis é estado efêmero com TTL (sem nova tecnologia). Interpretação ampliada de "Redis para cache e locks" → ver Complexity Tracking. | ⚠️ Justificado |
| VII. Sem testes | Nenhuma tarefa/projeto de teste; validação manual em `quickstart.md`. | ✅ |
| VIII. Stack Grafana | Logs estruturados via Serilog→Loki, traces via OTLP→Tempo (já configurados na API); nova métrica Prometheus `carrinho_operacoes_total` exposta em `/metrics`. Painel no Grafana listado como tarefa. | ✅ |
| Restrições adicionais | Segredos via env (`Redis__Password`), configuração via `appsettings`/env; JWT existente; README atualizado. | ✅ |

**Gate**: PASS (com 2 desvios justificados em Complexity Tracking). Re-avaliado após o design da Fase 1: sem novos desvios.

## Project Structure

### Documentation (this feature)

```text
specs/001-carrinho-compras/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/
│   └── carrinho-api.md  # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
eloja.api/
├── eloja.API/
│   ├── Controllers/CarrinhoController.cs          # NOVO: 5 rotas /carrinho
│   ├── appsettings.json                           # + seções Carrinho e Redis
│   ├── appsettings.Development.json               # + Redis (localhost)
│   └── eloja.api.http                             # + exemplos das rotas
├── eloja.Data/
│   ├── Configuration/RedisSettings.cs             # NOVO
│   ├── Entities/Carrinho.cs                       # NOVO: Carrinho + CarrinhoItem
│   ├── Infrastructure/IRedisConnection.cs         # NOVO
│   ├── Infrastructure/RedisConnection.cs          # NOVO
│   ├── Interface/ICarrinhoRepository.cs           # NOVO
│   ├── Interface/IInventarioRepository.cs         # NOVO (somente leitura)
│   ├── Repositories/CarrinhoRepository.cs         # NOVO: JSON no Redis + CAS
│   ├── Repositories/InventarioRepository.cs       # NOVO: espelho Redis + fallback PG
│   └── eloja.Data.csproj                          # + StackExchange.Redis 3.1.31
├── eloja.Services/
│   ├── Carrinho/CarrinhoSettings.cs               # NOVO: TtlDias, MaxItens
│   ├── Carrinho/CarrinhoExceptions.cs             # NOVO: EstoqueInsuficiente, LimiteExcedido
│   ├── Commands/AdicionarItemCarrinhoCommand.cs   # NOVO
│   ├── Commands/DefinirQuantidadeItemCarrinhoCommand.cs  # NOVO
│   ├── Commands/RemoverItemCarrinhoCommand.cs     # NOVO
│   ├── Commands/EsvaziarCarrinhoCommand.cs        # NOVO
│   ├── Queries/CarrinhoQuery.cs                   # NOVO
│   └── DTO/CarrinhoDTO.cs                         # NOVO
└── eloja.IoC/DependencyInjection.cs               # + registros Redis/repositórios/settings

docker-compose.yml                                 # + Carrinho__* (opcional) no serviço api
README.md                                          # + seção do carrinho
```

**Structure Decision**: Reutilizar a solução `eloja.api` existente (Princípio III, sem novo serviço), com as camadas já usadas: Controller → Command/Query (Services) → Repository (Data). A infraestrutura Redis fica em `eloja.Data` (nova pasta `Infrastructure/`, espelhando o `eloja-inventario-worker`, sem compartilhar projeto entre serviços — código duplicado conscientemente, conforme Princípio III).

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| API lê o espelho Redis do inventário (`quantidade-disponivel:*`/`quantidade-reservada:*`) e, em cache miss, a tabela `inventario` (dados do `eloja-inventario-worker`) — Princípios I e VI | O requisito central exige comparar a quantidade com o inventário disponível; o espelho Redis já reflete reservas em tempo real (o worker incrementa antes de gravar no PostgreSQL). O fallback é necessário porque as chaves têm TTL de 30 min e só existem após uma reserva. O fallback é somente-leitura e semeia as chaves com `SET NX` e o mesmo TTL/formato do worker, que continua sendo o único que as altera. | (C) request/reply via RabbitMQ exigiria alterar o worker e introduzir latência/complexidade desproporcional para a estrutura inicial; (A) ler apenas o PostgreSQL ignora reservas ainda não persistidas; só Redis sem fallback torna o carrinho inutilizável quando a chave não existe. Decisão do usuário no `/speckit-plan`: ler chaves Redis. |
| Carrinho como dado primário (não cache) no Redis — Princípio VI ("Redis para cache e locks distribuídos") | O pedido do usuário exige armazenar o carrinho em memória no Redis em JSON. O carrinho é estado efêmero com TTL, sem nova tecnologia de armazenamento; o Redis já roda com AOF (`--appendonly yes`). | PostgreSQL/MongoDB contrariam o requisito ("em memória no Redis"). Recomenda-se, no futuro, emendar o Princípio VI para listar "carrinho de compras" entre os usos do Redis. |
