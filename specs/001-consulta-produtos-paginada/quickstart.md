# Quickstart: Consulta de Produtos Paginada

Guia de validação manual (sem testes automatizados, Princípio VII). Contrato: [contracts/produto-listagem.openapi.yaml](./contracts/produto-listagem.openapi.yaml); modelo: [data-model.md](./data-model.md).

## Pré-requisitos

```bash
cp .env.example .env            # opcional; defaults de dev funcionam
docker compose up -d --build    # sobe API, Postgres, RabbitMQ, Redis etc.
dotnet build eloja.api/eloja.API.sln
```

Swagger: `http://localhost:8080/swagger`. Obtenha um token JWT pela rota de login (`AuthController`) e use-o como `Authorization: Bearer <token>` (botão *Authorize* do Swagger).

## Massa de dados sugerida

Crie 2 categorias e ao menos 25 produtos via `POST /produto` (nomes como "Abacate", "Amora", "Banana", "Caju", "Camiseta Azul", "Camiseta Preta"...). Insira saldo diretamente no Postgres para alguns produtos:

```sql
insert into public.inventario (id, produto_id, quantidade_disponivel, quantidade_reservada, criado_em)
values (gen_random_uuid(), '<id-produto-com-saldo>', 10, 0, now()),
       (gen_random_uuid(), '<id-produto-totalmente-reservado>', 5, 5, now());
-- demais produtos: sem registro de inventário (sem saldo)
```

## Cenários de validação

Sempre com o token. Base: `http://localhost:8080/produto`.

| # | Requisição | Resultado esperado |
|---|------------|--------------------|
| 1 | `GET /produto` | 200; até 10 itens; `pagina=1`, `tamanhoPagina=10`, `totalItens`/`totalPaginas` corretos; todos com `temEstoque` |
| 2 | `GET /produto?pagina=3&tamanhoPagina=10` (com 25 produtos) | 5 itens |
| 3 | `GET /produto?pagina=99` | 200, `itens` vazio, totais corretos |
| 4 | `GET /produto?nome=camiseta` | somente nomes contendo "camiseta" (qualquer caixa); totais refletem o filtro |
| 5 | `GET /produto?nome=zzzz` | `itens` vazio, `totalItens=0`, `totalPaginas=0` |
| 6 | `GET /produto?nome=%25` | tratado como texto literal (não retorna tudo) |
| 7 | `GET /produto?categoriaId=<id>` | somente produtos da categoria |
| 8 | `GET /produto?categoriaId=<id>&nome=azul` | ambos os critérios aplicados |
| 9 | `GET /produto?direcao=asc` | com saldo em ordem A→Z, depois sem saldo A→Z |
| 10 | `GET /produto?direcao=desc` | com saldo em ordem Z→A, depois sem saldo Z→A |
| 11 | percorrer todas as páginas | nenhum produto repetido/omitido; nenhum produto com saldo após um sem saldo |
| 12 | produto com `disponivel=5`, `reservada=5` | `temEstoque=false` |
| 13 | `GET /produto?pagina=0` / `tamanhoPagina=101` / `direcao=x` / `categoriaId=abc` | 400 com mensagem clara |
| 14 | `GET /produto` sem token | 401 |
| 15 | `GET /produto/{id}` | comportamento anterior inalterado |

## Observabilidade (Princípio VIII)

- Logs da consulta no Loki/Grafana (label `app=eloja-api`).
- Trace da requisição no Tempo (spans ASP.NET e Npgsql).
