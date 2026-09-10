# eloja

Plataforma de e-commerce composta por uma API (.NET) e workers de mensageria (RabbitMQ) para inventário e ordens, com observabilidade (Prometheus, Loki, Tempo, Grafana).

## Arquitetura (serviços do `docker-compose.yml`)

| Serviço             | Porta local        | Descrição                                 |
| ------------------- | ------------------ | ----------------------------------------- |
| `api`               | 8080               | API principal (eloja.API)                 |
| `inventario-worker` | 8081               | Worker de reserva/baixa de inventário     |
| `ordem-worker`      | 8082               | Worker de criação/confirmação de ordens   |
| `postgres-db`       | 5434               | PostgreSQL                                |
| `rabbitmq`          | 5672 / 15672       | Mensageria (UI de management em `:15672`) |
| `mongodb`           | 27017              | Persistência de ordens                    |
| `redis`             | 6379               | Cache / lock de reserva de inventário     |
| `redis-insight`     | 5540               | UI para o Redis                           |
| `prometheus`        | 9090               | Métricas                                  |
| `loki`              | 3100               | Logs                                      |
| `tempo`             | 3200 / 4317 / 4318 | Tracing (OTLP)                            |
| `grafana`           | 3000               | Dashboards                                |

## Como rodar localmente

Pré-requisitos: Docker e Docker Compose.

1. Clone o repositório.
2. (Opcional) copie o arquivo de exemplo de variáveis de ambiente e ajuste se quiser:
   ```bash
   cp .env.example .env
   ```
   Todos os valores já têm defaults de desenvolvimento definidos diretamente no `docker-compose.yml`, então o passo acima é opcional — o `.env` só é necessário se você quiser customizar usuários/senhas locais.
3. Suba toda a stack:
   ```bash
   docker compose up -d --build
   ```
4. A API estará disponível em `http://localhost:8080/swagger`.

Para derrubar tudo: `docker compose down` (adicione `-v` para também apagar os volumes/dados).

## Rodando um serviço individualmente (fora do Docker)

Cada projeto .NET possui um `appsettings.Development.json` com valores padrão apontando para `localhost` (assumindo que a infraestrutura de suporte — Postgres, RabbitMQ, Redis, Mongo, Loki — esteja rodando via `docker compose up`, usando as portas mapeadas na tabela acima).

```bash
cd eloja.api/eloja.API
dotnet run
```

## Configuração / segredos

Nenhum segredo real deve ser commitado. Este repositório usa apenas placeholders de desenvolvimento (`dev`/`dev`, `admin`/`admin`) definidos como defaults no `docker-compose.yml` e nos arquivos `appsettings.Development.json`.

- `.env.example` documenta todas as variáveis usadas pelo `docker-compose.yml` (credenciais de Postgres, RabbitMQ, Redis, MongoDB, Grafana e a chave JWT). Copie para `.env` e substitua pelos seus próprios valores caso não queira usar os defaults de dev.
- A chave `Jwt:Key` (usada para assinar tokens na `eloja.API`) **não** deve ser reaproveitada em produção. Gere uma nova com, por exemplo, `openssl rand -base64 32` e defina em `JWT_KEY` no seu `.env`.
- Nunca commite o arquivo `.env` real — ele já está no `.gitignore`.
