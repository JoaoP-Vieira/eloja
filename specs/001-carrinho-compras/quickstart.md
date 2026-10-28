# Quickstart: validar o Carrinho de Compras

Sem testes automatizados (Princípio VII): a validação é build + stack no Docker + verificação manual. Contrato das rotas em [contracts/carrinho-api.md](./contracts/carrinho-api.md); formato dos dados em [data-model.md](./data-model.md).

## Pré-requisitos

```bash
dotnet build eloja.api/eloja.API.sln
docker compose up -d --build
```

Swagger: `http://localhost:8080/swagger`. Redis Insight: `http://localhost:5540` (senha padrão do Redis: `dev`).

## Preparação

1. Criar usuário e autenticar nas rotas `/auth` existentes; copiar o JWT (`$TOKEN`). Repetir com um segundo usuário (`$TOKEN2`).
2. Criar um produto (`POST /produto`) e anotar o `id` (`$PID`).
3. Garantir inventário do produto: linha em `public.inventario` com `quantidade_disponivel = 10` e `quantidade_reservada = 0` para `$PID` (via `psql` no container `postgres-db`). Opcional: apagar as chaves `quantidade-disponivel:$PID` e `quantidade-reservada:$PID` no Redis para exercitar o fallback.

## Cenários

| # | Passo | Resultado esperado | Spec |
|---|-------|--------------------|------|
| 1 | `POST /carrinho/itens` `{produtoId:$PID, quantidade:3}` | 200; item com `quantidade: 3`, `quantidadeDisponivel: 10`; chave `carrinho:{id}` no Redis contém JSON | US1, FR-001 |
| 2 | Repetir com `quantidade: 4` | 200; um único item com `quantidade: 7` | US1.2, FR-003 |
| 3 | Repetir com `quantidade: 4` | 409 `ESTOQUE_INSUFICIENTE` com `quantidadeDisponivel: 10`; carrinho continua com 7 | US1.3, FR-007 |
| 4 | `GET /carrinho` | 200 com os itens; TTL da chave no Redis **não** muda | US2, FR-006/012 |
| 5 | `GET /carrinho` com `$TOKEN2` | Carrinho vazio (isolamento) | US2.3, FR-009 |
| 6 | `PUT /carrinho/itens/$PID` `{quantidade:8}` | 200; `quantidade: 8` | US3.1 |
| 7 | `PUT` com `{quantidade:11}` | 409; permanece 8 | US3.2 |
| 8 | `PUT`/`POST` com `0`, `-1`, `1.5`, `"abc"` | 400; carrinho inalterado | FR-008 |
| 9 | `POST` com `produtoId` inexistente | 404 | FR-008 |
| 10 | Sem `Authorization` em qualquer rota | 401 | US4.1 |
| 11 | Enviar `usuarioId` no corpo/query | Ignorado; opera apenas no carrinho do token | US4.2 |
| 12 | No SQL/Redis, reduzir `quantidade-disponivel:$PID` para 5 e `GET /carrinho` | Item com `quantidade: 8`, `estoqueInsuficiente: true`, `quantidadeDisponivel: 5`; JSON no Redis inalterado | Clarificação 1 |
| 13 | `DELETE /carrinho/itens/$PID` (e novamente) | 200 e depois 404; chave removida do Redis | US3.3 |
| 14 | `DELETE /carrinho` | 204; `GET` retorna vazio | US3.4 |
| 15 | Concorrência: ~50 `POST` simultâneos (ex.: `xargs -P 50 curl` ou script em `k6/`) de 1 unidade cada com disponível 10 | Total no carrinho ≤ 10; as demais retornam 409/503 | US4.5, SC-004 |
| 16 | Adicionar 51 produtos distintos | O 51º retorna 409 `LIMITE_ITENS` | FR-015 |
| 17 | Definir `Carrinho__TtlDias` pequeno (ou `EXPIRE` manual) e aguardar | `GET` retorna vazio após expirar | FR-012, SC-005 |
| 18 | `docker compose stop redis` e chamar qualquer rota | 503 `INDISPONIVEL` sem detalhes internos; `docker compose start redis` restaura | Edge case, FR-013 |

## Observabilidade

- Métrica: `carrinho_operacoes_total{operacao,resultado}` em `http://localhost:8080/metrics`; painel no Grafana (`http://localhost:3000`).
- Logs no Loki (label `app="eloja-api"`) e traces no Tempo.
