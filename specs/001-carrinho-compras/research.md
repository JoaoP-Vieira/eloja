# Research: Estrutura Inicial do Carrinho de Compras

Não restaram `NEEDS CLARIFICATION` no Technical Context. As decisões abaixo registram as escolhas técnicas e alternativas avaliadas.

## 1. Fonte da quantidade disponível do inventário

- **Decision**: Ler as chaves Redis do worker `quantidade-disponivel:{produtoId}` e `quantidade-reservada:{produtoId}` (disponível = disponivel − reservada, mínimo 0). Em cache miss (qualquer das duas ausente), consultar `public.inventario` (somente leitura) e semear as chaves com `SET ... NX` e TTL de 1800 s, formato idêntico ao do `ReservarInventarioCommandHandler`.
- **Rationale**: Escolha do usuário. O espelho Redis é atualizado de forma síncrona a cada reserva, enquanto o PostgreSQL só recebe a reserva depois (`ReservaInventarioDbSaveCommand`), então o Redis é mais preciso. O fallback evita um carrinho inutilizável quando a chave expira/ainda não existe.
- **Alternatives considered**: (A) apenas PostgreSQL — ignora reservas ainda não persistidas e adiciona carga no banco; (C) request/reply Wolverine/RabbitMQ — aderente ao Princípio I, mas exige alterar o worker e é desproporcional para a estrutura inicial; apenas Redis sem fallback — rejeita erroneamente quando a chave não existe.
- **Semântica verificada no código**: o script Lua do worker usa `restante = disponivel − reservada − solicitada` e, se uma chave não existe, semeia a partir do banco com TTL 1800 s. A API reproduz exatamente a leitura/semeadura, nunca incrementa `reservada`.

## 2. Formato e chave do carrinho no Redis

- **Decision**: Um valor string JSON por usuário em `carrinho:{usuarioId}` (DB 0, mesmo do worker), serializado com `System.Text.Json`, contendo `versao`, `itens[]` (`produtoId`, `quantidade`) e `atualizadoEm`.
- **Rationale**: Atende "itens do carrinho no formato json"; uma chave por usuário facilita TTL e leitura em uma ida ao Redis; o `usuarioId` vem do JWT, portanto o cliente não escolhe a chave (isolamento, FR-009). O campo `versao` permite evoluir o formato.
- **Alternatives considered**: Redis Hash por item (`HSET`) — não é "JSON" e complica TTL/limite; RedisJSON (módulo) — exigiria trocar a imagem `redis:7.4-alpine` (nova tecnologia, Princípio VI).

## 3. Atomicidade e concorrência (FR-010, FR-011)

- **Decision**: Ler-modificar-gravar com **transação condicional** do StackExchange.Redis: ler o valor atual, aplicar a regra em memória, e gravar com `Condition.StringEqual`/`KeyNotExists` na mesma `MULTI/EXEC`; se a condição falhar (outra requisição alterou), repetir até 5 vezes (backoff curto); depois disso retornar 503 "tente novamente".
- **Rationale**: Mantém a regra de negócio em C# (testável por leitura, padrão do repo) sem Lua/cjson (que codifica arrays vazios como objetos e dificulta manter o JSON). Garante que duas requisições simultâneas somadas não ultrapassem o estoque nem percam atualização.
- **Alternatives considered**: Script Lua (como o worker) — bom, mas duplica lógica de validação em outra linguagem e tem a armadilha do `cjson` com arrays vazios; lock distribuído (`SET NX`) por usuário — mais partes móveis e risco de lock órfão.
- **Observação**: a disponibilidade é lida antes da gravação condicional; ela não é propriedade do carrinho (o carrinho não reserva estoque), então a janela entre leitura e gravação é aceitável e coberta pela revalidação no pedido (fora do escopo).

## 4. Expiração (FR-012 + clarificação)

- **Decision**: TTL padrão 7 dias (`Carrinho:TtlDias`), aplicado via `StringSet(..., expiry)` apenas em gravações (adicionar/alterar/remover). `GET` não renova. Esvaziar remove a chave (`KeyDelete`). Remover o último item também remove a chave.
- **Rationale**: Atende a clarificação "renovar só em alterações" e SC-005.
- **Alternatives considered**: `KeyExpire` separado após gravação — não atômico; sliding expiration — descartado na clarificação.

## 5. Identificação e segurança do usuário (FR-009, FR-013)

- **Decision**: `[Authorize]` em todas as rotas; id do usuário extraído de `User.FindFirstValue(JwtRegisteredClaimNames.Sub)` (como em `OrdemController`; `MapInboundClaims = false` já configurado). Nenhuma rota/corpo aceita `usuarioId`. Erros internos retornam mensagem genérica com status 503/500 sem `ex.Message`.
- **Rationale**: Elimina IDOR por construção. Os controllers atuais devolvem `ex.Message` em 500, o que violaria FR-013; o novo controller mapeia exceções conhecidas e usa mensagens fixas.
- **Alternatives considered**: Rotas `/carrinho/{usuarioId}` com checagem de dono — superfície de ataque maior sem benefício.

## 6. Validação de entrada

- **Decision**: `produtoId` como `Guid` (binding rejeita formatos inválidos, `Guid.Empty` é rejeitado), `quantidade` como `int` ≥ 1 (zero, negativo e não inteiro/texto são rejeitados por validação/model binding → 400). Soma de quantidades feita em `long` para evitar overflow. Produto inexistente verificado com `IProdutoRepository.ExistsByIdAsync` (já existente) → 404. Limite de 50 produtos distintos (`Carrinho:MaxItens`) → 409.
- **Rationale**: Reaproveita o repositório existente e cobre FR-007, FR-008, FR-015.

## 7. Indisponibilidade do Redis

- **Decision**: Capturar `RedisConnectionException`/`RedisTimeoutException` no controller e responder 503 com mensagem genérica; nenhuma gravação parcial (a gravação é uma única operação). Timeouts curtos na conexão (`ConnectTimeout`/`SyncTimeout` ≈ 2 s) e `AbortOnConnectFail=false` para que a API suba mesmo com Redis temporariamente fora.
- **Rationale**: Edge case da spec. Não se adota Polly/circuit breaker nesta estrutura inicial (o worker usa por exigir resiliência em processamento de fila); pode ser adicionado depois.

## 8. Observabilidade (Princípio VIII)

- **Decision**: Logs estruturados via Serilog existente (sem dados sensíveis); contador Prometheus `carrinho_operacoes_total{operacao,resultado}` (`prometheus-net` já referenciado na API) e painel no Grafana; traces automáticos do ASP.NET Core via OTLP.
- **Alternatives considered**: Instrumentação OpenTelemetry específica do Redis — pacote novo, fora do necessário agora.

## 9. Convenções do repositório observadas

- Controllers pequenos com `IMessageBus`, handlers Wolverine descobertos pelo assembly de `eloja.Services` (`opts.Discovery.IncludeAssembly(typeof(LoginCommand).Assembly)`), `InvokeAsync` propaga exceções originais (usado nos controllers atuais para mapear 404/400).
- Records para comandos/queries, handlers com construtor primário, nomes em português para novos tipos (`...Command`/`...Query`).
- `RedisSettings`/`RedisConnection` do worker servem de modelo (copiados conscientemente para `eloja.Data`, Princípio III).
