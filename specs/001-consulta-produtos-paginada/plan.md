# Implementation Plan: Consulta de Produtos Paginada

**Branch**: `feat/consulta-produtos-paginada` | **Date**: 2026-10-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-consulta-produtos-paginada/spec.md`

## Summary

Adicionar à `eloja.api` a rota `GET /produto` (autenticada) que devolve uma página de produtos com pesquisa por nome, filtro por categoria, ordenação alfabética crescente/decrescente e um indicador `temEstoque` por produto. Produtos sem saldo disponível (`quantidade_disponivel - quantidade_reservada <= 0`, ou sem registro de inventário) são sempre posicionados depois dos produtos com saldo.

Abordagem técnica (ver [research.md](./research.md)): nova Query CQRS (`ProdutosPaginadosQuery`) tratada por Wolverine na camada Services; o repositório de produtos (camada Data, Dapper) executa uma única consulta SQL paginada com `LEFT JOIN` somente-leitura em `public.inventario` e uma consulta de contagem para os totais. O controller apenas compõe a chamada via `IMessageBus`, como nas demais rotas.

## Technical Context

**Language/Version**: C# / .NET 8 (`net8.0`)

**Primary Dependencies**: ASP.NET Core (controllers), WolverineFx 5.41.0 (mediator/CQRS), Dapper 2.1.79, Npgsql 10.0.3 (todos já referenciados; nenhuma dependência nova)

**Storage**: PostgreSQL (tabelas `public.produto`, `public.categoria` e, somente leitura, `public.inventario`)

**Testing**: N/A por decisão da constituição (Princípio VII); validação por `dotnet build`, `docker compose up -d --build` e verificação manual (ver [quickstart.md](./quickstart.md))

**Target Platform**: Container Linux (Docker Compose), serviço `api`

**Project Type**: web-service (API REST dentro da solução `eloja.api`)

**Performance Goals**: primeira página em < 1 s para catálogo de até 10.000 produtos (SC-001)

**Constraints**: somente leitura; tamanho de página padrão 10 e máximo 100; ordenação determinística entre páginas; sem novas tecnologias de armazenamento

**Scale/Scope**: até ~10.000 produtos; 1 endpoint novo, 1 query/handler, 2 DTOs, 1 método de repositório (+ projeção de leitura)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Avaliação | Status |
|-----------|-----------|--------|
| I. Microserviços / eventos | Feature é uma leitura dentro da `eloja.api`; sem chamadas síncronas entre serviços. Porém lê `public.inventario` (dono: `eloja-inventario-worker`) diretamente no banco. | ⚠️ Desvio justificado (Complexity Tracking) |
| II. Stack fixa C#/.NET | Apenas C#/.NET 8; nenhum front-end envolvido. | ✅ |
| III. Estrutura de pastas | Sem novo serviço/projeto; arquivos entram nos projetos existentes de `eloja.api/`. | ✅ |
| IV. DI + CQRS | Nova Query em `Queries/` + handler injetando `IProdutoRepository`; sem `new` de dependências. | ✅ |
| V. WolverineFX | Controller usa `IMessageBus.InvokeAsync`; nenhuma mensageria nova. | ✅ |
| VI. Dados (Postgres/Redis/Mongo) | Usa somente PostgreSQL existente; Redis não é usado (chaves são efêmeras, TTL 30 min — ver research). | ✅ |
| VII. Sem testes | Plano não inclui tarefas de teste. | ✅ |
| VIII. Observabilidade Grafana | Reusa logs Serilog→Loki, métricas Prometheus e traces Npgsql/ASP.NET já configurados na API; handler registra log estruturado. | ✅ |
| IX. Camadas Data/Services/IoC | SQL e projeção de leitura em `eloja.Data`; Query, handler e DTOs em `eloja.Services`; IoC sem mudanças (repositório já registrado); controller só expõe endpoint. | ✅ |

**Gate**: aprovado, com 1 desvio documentado em Complexity Tracking (Princípio I), aceito pelo responsável do projeto.

**Re-check pós-design**: mantém-se o mesmo resultado; o design não introduziu violações adicionais.

## Project Structure

### Documentation (this feature)

```text
specs/001-consulta-produtos-paginada/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── produto-listagem.openapi.yaml
├── checklists/
│   └── requirements.md
└── tasks.md             # gerado por /speckit-tasks
```

### Source Code (repository root)

```text
eloja.api/
├── eloja.API/
│   ├── Controllers/ProdutoController.cs        # + GET /produto (GetProdutos)
│   └── eloja.api.http                          # + exemplos da nova rota
├── eloja.Services/
│   ├── Queries/ProdutosPaginadosQuery.cs       # NOVO: record + handler (validação, defaults, mapeamento)
│   └── DTO/
│       ├── ProdutoListaItemDTO.cs              # NOVO: ProdutoDTO + TemEstoque
│       └── PaginaDTO.cs                        # NOVO: envelope genérico de paginação
├── eloja.Data/
│   ├── Entities/ProdutoListagem.cs             # NOVO: projeção de leitura (produto + categoria + saldo)
│   ├── Interface/IProdutoRepository.cs         # + SelectPaginadoAsync, CountAsync
│   └── Repositories/ProdutoRepository.cs       # + consultas SQL paginada e de contagem
└── eloja.IoC/                                  # sem alterações

db/
└── inventario.sql                              # + índice em inventario(produto_id) (opcional, ver research)
```

**Structure Decision**: Reutilizar a solução `eloja.api` existente, nas três camadas (Data/Services/API) conforme os Princípios III, IV e IX. Nenhum projeto novo.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Princípio I: a `eloja.api` lê `public.inventario` (tabela do `eloja-inventario-worker`) via `LEFT JOIN` somente-leitura no mesmo Postgres | O indicador de estoque deve ser ordenável e paginável junto com os produtos (sem-saldo sempre no final), o que exige o saldo no mesmo conjunto de dados da consulta | (B) chamada síncrona ao worker exigiria novo endpoint HTTP e uma chamada por página/produto, violando também o Princípio I e adicionando latência; (C) read model próprio alimentado por eventos aumenta muito o escopo (novos eventos, consumidor e tabela) para uma consulta simples. Mitigação: acesso estritamente de leitura, sem escrita nem DDL em `inventario` pela API |
