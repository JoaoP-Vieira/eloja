<!--
Sync Impact Report
Version change: 1.1.0 → 1.2.0
Princípios modificados: IV (Commands/Queries referenciam a camada Services, Princípio IX)
Princípios adicionados: IX. Separação de Responsabilidades por Camada (Data, Services, IoC)
Seções modificadas: Fluxo de Desenvolvimento (verificação passa a cobrir I–IX)
Seções removidas: nenhuma
Templates que leem a constituição em tempo de execução: não modificados por este comando.
TODOs adiados: nenhum
-->
# Eloja Constitution

## Core Principles

### I. Microserviços e Arquitetura Orientada a Eventos
O Eloja é uma plataforma de e-commerce composta por microserviços independentes
(API e workers, como `eloja.api`, `eloja-ordem-worker`, `eloja-inventario-worker`).

- Cada serviço MUST ter responsabilidade de negócio única e ser implantável de forma
  independente (um container/Dockerfile por serviço no `docker-compose.yml`).
- A comunicação entre serviços para fluxos de negócio MUST ocorrer por eventos/mensagens
  assíncronas. Chamadas síncronas entre serviços MUST NOT ser introduzidas sem justificativa
  documentada.
- Um serviço MUST NOT acessar diretamente o banco de dados de outro serviço.
- Handlers de eventos MUST ser tolerantes a reentrega (idempotentes) e a falhas parciais.

Racional: baixo acoplamento, escalabilidade independente e resiliência no fluxo de
pedidos e estoque.

### II. Stack Tecnológica Fixa (C# / .NET e React)
- O back-end MUST ser escrito exclusivamente em C# sobre .NET (.NET Core/.NET moderno).
  Outras linguagens ou runtimes de back-end MUST NOT ser usados.
- O front-end MUST ser escrito em React.
- Novas dependências MUST ser pacotes NuGet/npm mantidos e compatíveis com essas stacks.
- Observabilidade MUST seguir o Princípio VIII (stack Grafana já presente na stack).

Racional: uma única linguagem no back-end reduz custo cognitivo e acelera o desenvolvimento.

### III. Estrutura Padronizada de Pastas (Solução por Parte)
- Cada parte do projeto (API, cada worker, front-end, etc.) MUST ter sua própria pasta na raiz
  do repositório contendo sua solução (`.sln`) e o(s) projeto(s) dentro dela
  (ex.: `eloja-ordem-worker/eloja-ordem-worker.sln` + `eloja-ordem-worker/eloja-ordem-worker/`).
- Serviços MUST NOT compartilhar solução ou projeto com outro serviço; código comum MUST ser
  duplicado conscientemente ou extraído por decisão explícita e documentada.
- Todo novo serviço MUST incluir seu `Dockerfile`, `appsettings.Development.json` e
  entrada no `docker-compose.yml`.

Racional: isolamento entre serviços e previsibilidade de onde cada coisa está.

### IV. Padrão Arquitetural Único: DI + CQRS
- Todo novo projeto back-end MUST seguir o mesmo padrão dos projetos já desenvolvidos:
  Injeção de Dependência (registro centralizado, p.ex. `eloja.IoC/DependencyInjection.cs`
  ou `Program.cs`) e CQRS.
- Operações de escrita MUST ser modeladas como Commands (pasta `Commands/`) e leituras como
  Queries (pasta `Queries/`), cada uma com seu handler, na camada Services (Princípio IX).
- Dependências MUST ser consumidas por interfaces (`IXxxRepository`, `IUnitOfWork`, etc.)
  injetadas por construtor; `new` de serviços/repositórios dentro de handlers MUST NOT ocorrer.
- Acesso a dados MUST usar o padrão Repository/UnitOfWork existente.
- Antes de criar um projeto ou feature, é obrigatório inspecionar os projetos existentes e replicar
  nomenclatura, organização de pastas e convenções.

Racional: consistência entre serviços permite que qualquer desenvolvedor navegue em todos.

### V. Mensageria Exclusivamente via WolverineFX / RabbitMQ
- Toda publicação e consumo de eventos/mensagens no RabbitMQ MUST usar a biblioteca
  WolverineFX (`WolverineFx.RabbitMQ`, e demais pacotes `WolverineFx.*` na mesma versão).
- Clientes diretos do RabbitMQ (ex.: `RabbitMQ.Client`) MUST NOT ser usados para publicar ou
  consumir mensagens.
- Filas, exchanges e bindings MUST ser definidos de forma consistente com
  `rabbitmq/definitions.json` e a configuração do Wolverine do serviço.
- Quando houver persistência transacional de mensagens, é obrigatório usar o suporte do Wolverine
  (ex.: `WolverineFx.Postgresql`) em vez de implementações próprias.

Racional: um único mecanismo de mensageria padroniza roteamento, retentativas e handlers.

### VI. Persistência Poliglota com Responsabilidades Definidas
- PostgreSQL MUST ser usado para dados relacionais transacionais (produtos, categorias,
  usuários, inventário).
- MongoDB MUST ser usado para o armazenamento de ordens (documentos).
- Redis MUST ser usado para cache e locks distribuídos (ex.: reserva de inventário).
- RabbitMQ MUST ser o único broker de mensagens.
- Cada serviço MUST ser dono dos seus dados; esquemas/inicializações ficam em `db/`,
  `mongodb/` e `rabbitmq/`.
- Introduzir nova tecnologia de armazenamento ou mensageria exige emenda desta constituição.

Racional: cada tecnologia atende a um caso de uso claro, evitando sobreposição.

### VII. Sem Testes Automatizados (Decisão Deliberada)
- Para acelerar o desenvolvimento, NÃO serão desenvolvidos testes unitários, de integração
  nem end-to-end (E2E).
- Specs, planos e tarefas MUST NOT incluir tarefas de criação de testes nem projetos de teste.
- Fluxos TDD/Test-First MUST NOT ser exigidos em nenhuma etapa do fluxo Spec Kit.
- A validação MUST ser feita por build bem-sucedido (`dotnet build`, build do front-end),
  execução da stack via `docker compose` e verificação manual (Swagger, UI do RabbitMQ,
  dashboards), compensando a ausência de testes.
- Scripts de carga em `k6/` já existentes MAY ser mantidos, mas não são exigidos.

Racional: a velocidade de entrega é priorizada; o risco assumido é aceito explicitamente.

### VIII. Monitoramento Exclusivamente via Stack Grafana
- Todo monitoramento (métricas, logs, tracing e dashboards) MUST ser implementado usando a
  stack Grafana já existente no `docker-compose.yml`: Prometheus (métricas), Loki (logs),
  Tempo (tracing via OTLP) e Grafana (visualização e alertas).
- Todo novo serviço MUST expor métricas para o Prometheus (`prometheus.yml`), enviar logs
  estruturados ao Loki e exportar traces via OTLP ao Tempo, seguindo a configuração dos
  serviços existentes.
- Dashboards e alertas MUST ser criados no Grafana.
- Ferramentas de monitoramento/APM alternativas (ex.: Application Insights, Datadog,
  ELK, Jaeger) MUST NOT ser introduzidas sem emenda desta constituição.
- Health checks (ex.: `WolverineFx.HealthChecks`) MAY ser usados como fonte de métricas
  consumidas pela stack Grafana.

Racional: uma única stack de observabilidade evita fragmentação e já está operacional.

### IX. Separação de Responsabilidades por Camada (Data, Services, IoC)
Todo serviço back-end MUST organizar seu código em três camadas, cada uma em seu próprio
projeto dentro da pasta/solução do serviço (padrão `eloja.api`: `eloja.Data`,
`eloja.Services`, `eloja.IoC`), além do projeto de entrada (API ou worker).

- **Data**: MUST conter somente repositórios, classes (entidades, contextos) e interfaces
  relacionados à interação com o banco de dados (PostgreSQL, MongoDB).
- **Services**: MUST conter classes, interfaces, Commands, Queries e DTOs. Toda implementação
  de uso de algum serviço/tecnologia externa, como Redis, RabbitMQ (via WolverineFX), Kafka e
  similares, MUST residir nesta camada.
- **IoC**: MUST conter somente a inversão de dependências (registro de DI das demais camadas).
- A camada Data MUST NOT conter regras de negócio, Commands/Queries nem integrações com
  Redis/mensageria; a camada Services MUST NOT acessar o banco diretamente, apenas por
  interfaces da camada Data.
- O projeto de entrada (API/worker) MUST apenas compor a aplicação chamando a camada IoC e
  expor endpoints/handlers; MUST NOT instanciar repositórios ou clientes de infraestrutura.

Racional: fronteiras claras por camada tornam o código previsível e permitem trocar uma
tecnologia mexendo em um único lugar.

## Restrições Adicionais

- Segredos reais MUST NOT ser commitados; usar apenas placeholders de desenvolvimento
  (`.env.example`, `appsettings.Development.json`). A chave JWT MUST vir de configuração.
- Autenticação da API MUST usar JWT conforme o padrão existente (`eloja.Services/Auth`).
- Configuração MUST vir de variáveis de ambiente/`appsettings`, nunca hard-coded.
- Toda a stack MUST subir com `docker compose up -d --build`.
- A documentação (`README.md`) MUST ser atualizada ao adicionar ou alterar serviços.

## Fluxo de Desenvolvimento

- Mudanças seguem o fluxo Spec Kit: specify → plan → tasks → implement.
- Todo plano MUST incluir uma verificação de conformidade (Constitution Check) contra os
  princípios I–IX.
- Mudanças em contratos de eventos MUST considerar produtores e consumidores existentes
  para não quebrar outros serviços.
- Nomes de eventos, comandos e queries seguem as convenções dos projetos existentes
  (português, sufixo `Command`/`Query`).
- Desvios dos princípios MUST ser justificados por escrito no plano (Complexity Tracking).

## Governance

- Esta constituição prevalece sobre quaisquer outras práticas do projeto.
- Emendas MUST ser feitas via `/speckit-constitution`, documentando a mudança, o motivo e,
  quando aplicável, o impacto em serviços existentes.
- Versionamento semântico: MAJOR para remoção/redefinição incompatível de princípios;
  MINOR para novo princípio/seção ou expansão material; PATCH para esclarecimentos e
  correções de texto.
- Revisões de código e planos MUST verificar conformidade com esta constituição; violações
  sem justificativa MUST ser corrigidas antes do merge.
- Orientações de uso no dia a dia estão no `README.md`.

**Version**: 1.2.0 | **Ratified**: 2026-10-27 | **Last Amended**: 2026-10-29
