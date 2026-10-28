# Data Model: Carrinho de Compras

## Armazenamento

| Dado | Onde | Chave | TTL | Dono |
|------|------|-------|-----|------|
| Carrinho (JSON) | Redis DB 0 | `carrinho:{usuarioId}` | `Carrinho:TtlDias` (7 dias), renovado só em alterações | `eloja.api` |
| Disponível (espelho) | Redis DB 0 | `quantidade-disponivel:{produtoId}` | 1800 s | `eloja-inventario-worker` (API só lê/semeia em miss) |
| Reservada (espelho) | Redis DB 0 | `quantidade-reservada:{produtoId}` | 1800 s | `eloja-inventario-worker` (API só lê/semeia em miss) |
| Inventário (fallback, leitura) | PostgreSQL `public.inventario` | `produto_id` | — | `eloja-inventario-worker` |
| Produto (existência) | PostgreSQL `public.produto` | `id` | — | `eloja.api` |

`{produtoId}` usa o formato padrão de `Guid.ToString()` (minúsculo, com hífens), idêntico ao worker.

## Carrinho (documento JSON)

```json
{
  "versao": 1,
  "atualizadoEm": "2026-10-27T21:50:00Z",
  "itens": [
    { "produtoId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "quantidade": 3 }
  ]
}
```

| Campo | Tipo | Regras |
|-------|------|--------|
| `versao` | inteiro | Sempre `1` nesta versão; permite evolução do formato |
| `atualizadoEm` | data/hora UTC (ISO 8601) | Atualizado a cada alteração bem-sucedida |
| `itens` | lista | 0..`Carrinho:MaxItens` (50); no máximo um item por `produtoId`; carrinho vazio nunca é persistido (a chave é removida) |
| `itens[].produtoId` | GUID | Diferente de `Guid.Empty`; produto deve existir em `public.produto` ao adicionar |
| `itens[].quantidade` | inteiro | ≥ 1; `quantidade` resultante ≤ disponível atual no momento da operação |

### Entidades (C#, `eloja.Data/Entities/Carrinho.cs`)

- `Carrinho`: `Versao`, `AtualizadoEm`, `Itens` (`List<CarrinhoItem>`).
- `CarrinhoItem`: `ProdutoId` (`Guid`), `Quantidade` (`int`).

## Regras de validação (origem → requisito)

| Regra | Requisito |
|-------|-----------|
| Quantidade ≥ 1 (zero/negativa/não inteira → 400) | FR-008 |
| Produto inexistente ao adicionar → 404 | FR-008 |
| `novaQuantidade = existente + informada` (adicionar) ou `informada` (definir) em `long`; se `> disponível` → 409 com `quantidadeDisponivel`, carrinho inalterado | FR-003, FR-004, FR-007 |
| Disponível = `max(0, disponivel − reservada)` | FR-014 |
| Novo produto quando já há 50 itens → 409 (`LIMITE_ITENS`) | FR-015 |
| Definir/remover item inexistente no carrinho → 404 | Edge case |
| Remover o último item ou esvaziar → remover a chave | FR-005, SC-005 |
| Gravação condicional (compare-and-set), ≤ 5 tentativas; falha → 503 | FR-010, FR-011 |

## Transições de estado

```text
(sem chave) --adicionar--> Carrinho(1..50 itens) --adicionar/definir--> Carrinho
Carrinho --remover último item / esvaziar / TTL expirou--> (sem chave)
```

Consulta nunca altera nem renova o TTL.

## Visão de resposta (DTO, `eloja.Services/DTO/CarrinhoDTO.cs`)

- `CarrinhoDTO`: `itens` (`CarrinhoItemDTO[]`), `atualizadoEm` (nulo se carrinho vazio).
- `CarrinhoItemDTO`: `produtoId`, `quantidade`, `quantidadeDisponivel`, `estoqueInsuficiente` (`quantidade > quantidadeDisponivel`). Calculado na leitura, nunca persistido (clarificação: carrinho não é alterado pela consulta).

## Configuração

```json
"Carrinho": { "TtlDias": 7, "MaxItens": 50 },
"Redis": { "Host": "localhost", "Port": 6379, "Password": "" }
```

Variáveis de ambiente equivalentes: `Carrinho__TtlDias`, `Carrinho__MaxItens`, `Redis__Host`, `Redis__Port`, `Redis__Password` (as de Redis já existem no `docker-compose.yml` para o serviço `api`).
