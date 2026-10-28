# Contrato HTTP: Carrinho de Compras

Base: `eloja.api` (`http://localhost:8080`). Todas as rotas exigem `Authorization: Bearer <jwt>` (mesmo JWT de `/auth`). O dono do carrinho é sempre o usuário do claim `sub`; **nenhuma rota ou corpo aceita identificador de usuário**.

## Modelos

**CarrinhoResponse**

```json
{
  "itens": [
    {
      "produtoId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "quantidade": 3,
      "quantidadeDisponivel": 10,
      "estoqueInsuficiente": false
    }
  ],
  "atualizadoEm": "2026-10-27T21:50:00Z"
}
```

Carrinho vazio: `{ "itens": [], "atualizadoEm": null }`.

**ErroResponse**

```json
{ "codigo": "ESTOQUE_INSUFICIENTE", "message": "Quantidade solicitada maior que a disponível.", "quantidadeDisponivel": 5 }
```

`quantidadeDisponivel` só aparece em `ESTOQUE_INSUFICIENTE`. Mensagens nunca expõem detalhes internos (FR-013).

## Rotas

### GET `/carrinho`

Consulta o carrinho do usuário autenticado. Não renova o TTL e não altera o carrinho.

| Status | Quando |
|--------|--------|
| 200 | `CarrinhoResponse` (vazio se não houver carrinho). Itens com `quantidade > quantidadeDisponivel` vêm com `estoqueInsuficiente: true` |
| 401 | Sem token / token inválido |
| 503 | Redis indisponível (`INDISPONIVEL`) |

### POST `/carrinho/itens`

Adiciona o produto; se já existir, soma a quantidade.

Corpo: `{ "produtoId": "<guid>", "quantidade": 3 }`

| Status | Quando |
|--------|--------|
| 200 | Carrinho atualizado (`CarrinhoResponse`) |
| 400 | `produtoId` inválido/vazio, `quantidade` ≤ 0, não inteira ou ausente (`DADOS_INVALIDOS`) |
| 401 | Não autenticado |
| 404 | Produto inexistente (`PRODUTO_NAO_ENCONTRADO`) |
| 409 | Total > disponível (`ESTOQUE_INSUFICIENTE`, com `quantidadeDisponivel`) ou novo produto acima de 50 distintos (`LIMITE_ITENS`) — carrinho inalterado |
| 503 | Redis indisponível ou conflito de concorrência persistente (`INDISPONIVEL`) |

### PUT `/carrinho/itens/{produtoId}`

Define a quantidade de um item já existente no carrinho.

Corpo: `{ "quantidade": 8 }`

| Status | Quando |
|--------|--------|
| 200 | Carrinho atualizado |
| 400 | `produtoId` inválido; `quantidade` ≤ 0/não inteira (zero **não** remove; use DELETE) |
| 401 | Não autenticado |
| 404 | Item não está no carrinho (`ITEM_NAO_ENCONTRADO`) |
| 409 | `quantidade` > disponível (`ESTOQUE_INSUFICIENTE`) — carrinho inalterado |
| 503 | `INDISPONIVEL` |

### DELETE `/carrinho/itens/{produtoId}`

Remove um item. Se for o último, o carrinho deixa de existir (próxima consulta retorna vazio).

| Status | Quando |
|--------|--------|
| 200 | Carrinho atualizado |
| 400 | `produtoId` inválido |
| 401 | Não autenticado |
| 404 | Item não está no carrinho (`ITEM_NAO_ENCONTRADO`) |
| 503 | `INDISPONIVEL` |

### DELETE `/carrinho`

Esvazia o carrinho (idempotente).

| Status | Quando |
|--------|--------|
| 204 | Carrinho removido (mesmo se já estava vazio) |
| 401 | Não autenticado |
| 503 | `INDISPONIVEL` |

## Garantias

- Cada alteração é atômica (tudo ou nada); falhas não deixam alteração parcial (FR-011).
- Alterações simultâneas do mesmo usuário nunca resultam em quantidade acima do disponível nem em perda de atualização (FR-010).
- TTL (padrão 7 dias) renovado somente por POST/PUT/DELETE de item bem-sucedidos.
