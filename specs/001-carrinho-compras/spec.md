# Feature Specification: Estrutura Inicial do Carrinho de Compras

**Feature Branch**: `feat/001-carrinho-compras`

**Created**: 2026-10-27

**Status**: Draft

**Input**: User description: "estrutura inicial carrinho de compras - O back-end deve ser capaz de armazenar em memória no Redis os itens do carrinho do usuário no formato json. Deve existir rotas capazes de alterar esse valor com segurança. O usuário não pode salvar no carrinho uma quantidade de um item maior do que a quantidade disponivel no inventário."

## Clarifications

### Session 2026-10-27

- Q: Quando o estoque cair abaixo da quantidade já no carrinho, o que a consulta do carrinho deve fazer com o item? → A: Retornar o item como salvo, marcado como "estoque insuficiente" e informando a quantidade disponível atual (sem alterar o carrinho).
- Q: O prazo de expiração do carrinho deve ser renovado a cada operação ou só nas alterações? → A: Renovado somente a cada alteração (adicionar, alterar, remover); consultas não renovam.
- Q: Deve existir um limite máximo de produtos diferentes por carrinho? → A: Sim, 50 produtos diferentes, ajustável por configuração.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Adicionar item ao carrinho respeitando o estoque (Priority: P1)

Um usuário autenticado escolhe um produto e informa uma quantidade para colocar no seu carrinho. O sistema guarda o item no carrinho do usuário, desde que a quantidade total desse produto no carrinho não ultrapasse a quantidade disponível no inventário.

**Why this priority**: É o núcleo do carrinho: sem a capacidade de guardar itens e impedir quantidades acima do estoque, nenhuma outra funcionalidade de carrinho tem valor.

**Independent Test**: Pode ser testado autenticando um usuário, adicionando um produto com quantidade válida (aceito) e depois tentando uma quantidade acima do estoque (rejeitado), entregando já um carrinho funcional e consistente com o inventário.

**Acceptance Scenarios**:

1. **Given** um usuário autenticado com carrinho vazio e um produto com 10 unidades disponíveis, **When** ele adiciona 3 unidades do produto, **Then** o carrinho passa a conter o produto com quantidade 3.
2. **Given** um carrinho que já contém 3 unidades de um produto com 10 disponíveis, **When** o usuário adiciona mais 4 unidades do mesmo produto, **Then** o carrinho passa a conter 7 unidades desse produto (um único item por produto).
3. **Given** um carrinho que já contém 7 unidades de um produto com 10 disponíveis, **When** o usuário tenta adicionar mais 4 unidades, **Then** a operação é rejeitada com mensagem clara informando a quantidade disponível e o carrinho permanece inalterado.
4. **Given** um produto com 0 unidades disponíveis, **When** o usuário tenta adicioná-lo ao carrinho, **Then** a operação é rejeitada e o carrinho permanece inalterado.

---

### User Story 2 - Consultar o carrinho (Priority: P2)

Um usuário autenticado consulta o conteúdo atual do seu carrinho (produtos e quantidades) para revisar o que pretende comprar.

**Why this priority**: Sem consulta, o usuário não consegue ver o resultado das alterações; é necessária para validar e usar o carrinho, mas depende de itens já terem sido adicionados.

**Independent Test**: Pode ser testado adicionando itens e consultando o carrinho, verificando que o conteúdo retornado corresponde ao que foi salvo; um usuário sem itens recebe carrinho vazio.

**Acceptance Scenarios**:

1. **Given** um usuário com itens no carrinho, **When** ele consulta o carrinho, **Then** recebe a lista de itens com produto e quantidade.
2. **Given** um usuário que nunca adicionou itens, **When** ele consulta o carrinho, **Then** recebe um carrinho vazio (não um erro).
3. **Given** dois usuários distintos com carrinhos diferentes, **When** cada um consulta o seu carrinho, **Then** cada um vê somente os próprios itens.
4. **Given** um carrinho com 7 unidades de um produto cujo estoque caiu para 5, **When** o usuário consulta o carrinho, **Then** o item é retornado com quantidade 7, marcado como "estoque insuficiente" e com a quantidade disponível atual (5), sem que o carrinho seja alterado.

---

### User Story 3 - Alterar quantidade e remover itens (Priority: P2)

Um usuário autenticado altera a quantidade de um item já presente no carrinho, remove um item específico ou esvazia o carrinho inteiro.

**Why this priority**: Completa o ciclo de gerenciamento do carrinho; sem isso o usuário só consegue acumular itens.

**Independent Test**: Pode ser testado com um carrinho preenchido, alterando a quantidade (dentro e acima do estoque), removendo um item e esvaziando o carrinho.

**Acceptance Scenarios**:

1. **Given** um carrinho com 3 unidades de um produto com 10 disponíveis, **When** o usuário altera a quantidade para 8, **Then** o carrinho passa a ter 8 unidades.
2. **Given** o mesmo carrinho, **When** o usuário altera a quantidade para 11, **Then** a operação é rejeitada informando a quantidade disponível e o carrinho permanece com 3 unidades.
3. **Given** um carrinho com vários itens, **When** o usuário remove um item, **Then** apenas esse item deixa de existir no carrinho.
4. **Given** um carrinho com itens, **When** o usuário esvazia o carrinho, **Then** a consulta seguinte retorna um carrinho vazio.

---

### User Story 4 - Proteção do carrinho contra acesso e dados indevidos (Priority: P1)

O carrinho de um usuário só pode ser lido ou modificado por ele mesmo, e somente com dados válidos. Requisições sem autenticação, com quantidades inválidas ou referentes a produtos inexistentes são rejeitadas sem alterar nada.

**Why this priority**: O requisito de "alterar com segurança" é explícito na descrição; um carrinho que aceita qualquer usuário ou dado inválido compromete a integridade do inventário e a privacidade.

**Independent Test**: Pode ser testado enviando requisições sem autenticação, tentando acessar o carrinho de outro usuário e enviando quantidades zero, negativas ou não numéricas, confirmando que todas são rejeitadas e que nenhum carrinho muda.

**Acceptance Scenarios**:

1. **Given** uma requisição sem autenticação válida, **When** ela tenta consultar ou alterar um carrinho, **Then** é rejeitada como não autenticada.
2. **Given** um usuário autenticado, **When** ele tenta acessar ou alterar o carrinho de outro usuário (por qualquer meio, inclusive informando outro identificador), **Then** o sistema opera somente sobre o carrinho do usuário autenticado.
3. **Given** um usuário autenticado, **When** ele envia quantidade zero, negativa ou não inteira, **Then** a operação é rejeitada com mensagem de validação e o carrinho permanece inalterado.
4. **Given** um usuário autenticado, **When** ele tenta adicionar um produto que não existe, **Then** a operação é rejeitada e o carrinho permanece inalterado.
5. **Given** duas requisições simultâneas do mesmo usuário que, somadas, ultrapassariam o estoque disponível, **When** ambas são processadas, **Then** o carrinho nunca termina com quantidade acima do disponível.

---

### Edge Cases

- O estoque disponível diminui depois de o item já estar no carrinho (ex.: outra compra): a consulta do carrinho continua exibindo a quantidade salva, mas marca o item como "estoque insuficiente" e informa a quantidade disponível atual, sem alterar o carrinho; qualquer nova alteração é validada contra o estoque atual; a validação definitiva na finalização do pedido está fora do escopo desta feature.
- O usuário envia quantidade maior que o limite aceitável de inteiros ou texto no lugar de número: rejeitado como dado inválido.
- O serviço de armazenamento em memória do carrinho está indisponível: o usuário recebe erro claro de indisponibilidade temporária e nenhuma alteração parcial é registrada.
- O carrinho armazenado está corrompido ou em formato inesperado: o sistema não deve expor detalhes internos; trata como erro e preserva a possibilidade de esvaziar o carrinho.
- Alterar a quantidade de um item para zero: tratado como rejeição por quantidade inválida (a remoção é feita pela operação explícita de remover item).
- Remover um item que não está no carrinho: resposta indicando que o item não existe no carrinho, sem efeitos colaterais.
- Carrinho inativo por muito tempo: expira automaticamente e passa a ser vazio.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: O sistema MUST armazenar o carrinho de cada usuário em memória (Redis), com o conteúdo dos itens serializado em formato JSON.
- **FR-002**: O sistema MUST manter um único carrinho por usuário, identificado pelo usuário autenticado, contendo no máximo um item por produto.
- **FR-003**: O sistema MUST permitir ao usuário autenticado adicionar um produto ao carrinho com uma quantidade; se o produto já existir no carrinho, a quantidade informada é somada à existente.
- **FR-004**: O sistema MUST permitir ao usuário autenticado definir a quantidade de um item já existente no carrinho.
- **FR-005**: O sistema MUST permitir ao usuário autenticado remover um item específico e esvaziar o carrinho inteiro.
- **FR-006**: O sistema MUST permitir ao usuário autenticado consultar o conteúdo atual do próprio carrinho, retornando carrinho vazio quando não houver itens. Cada item retornado cuja quantidade salva exceda o estoque disponível atual MUST ser sinalizado como "estoque insuficiente", com a quantidade disponível atual, sem modificar o carrinho armazenado.
- **FR-007**: O sistema MUST rejeitar qualquer adição ou alteração cuja quantidade resultante de um produto no carrinho seja maior que a quantidade disponível no inventário no momento da operação, mantendo o carrinho inalterado e informando a quantidade disponível.
- **FR-008**: O sistema MUST rejeitar quantidades zero, negativas ou não inteiras, e produtos inexistentes, com mensagens de validação claras.
- **FR-009**: O sistema MUST exigir autenticação em todas as operações do carrinho e MUST operar exclusivamente sobre o carrinho do usuário autenticado, ignorando qualquer identificador de usuário enviado pelo cliente.
- **FR-010**: O sistema MUST garantir que operações simultâneas sobre o mesmo carrinho não resultem em quantidade acima do estoque nem em perda de atualizações.
- **FR-011**: O sistema MUST tratar cada alteração como atômica: ou a operação é aplicada por completo, ou o carrinho permanece como estava.
- **FR-012**: O sistema MUST expirar automaticamente carrinhos inativos após um período definido por configuração, renovado somente a cada alteração do carrinho (adicionar, alterar quantidade, remover); consultas não renovam o prazo.
- **FR-013**: O sistema MUST NOT expor detalhes internos (estrutura de armazenamento, erros técnicos) nas respostas de erro.
- **FR-014**: A consulta de disponibilidade do inventário MUST considerar a quantidade realmente disponível para venda, sem alterar nem reservar o inventário (o carrinho não reserva estoque).
- **FR-015**: O sistema MUST limitar o carrinho a 50 produtos diferentes (valor ajustável por configuração), rejeitando com mensagem clara a adição de um novo produto que ultrapasse o limite, sem alterar o carrinho; alterações em produtos já presentes continuam permitidas.

### Key Entities _(include if feature involves data)_

- **Carrinho**: Coleção de itens pertencente a exatamente um usuário; possui data da última alteração e prazo de expiração por inatividade.
- **Item do Carrinho**: Representa um produto no carrinho; atributos principais: identificador do produto e quantidade desejada (inteiro positivo); no máximo um por produto em cada carrinho.
- **Inventário (existente)**: Quantidade disponível de cada produto; fonte de verdade usada para validar as quantidades do carrinho, somente leitura nesta feature.
- **Usuário (existente)**: Dono do carrinho, identificado pela autenticação já existente.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: 100% das tentativas de salvar no carrinho uma quantidade maior que a disponível no inventário são rejeitadas e deixam o carrinho inalterado.
- **SC-002**: 100% das requisições sem autenticação ou direcionadas ao carrinho de outro usuário são rejeitadas ou limitadas ao carrinho do próprio usuário autenticado, sem vazamento de dados.
- **SC-003**: Um usuário consegue adicionar um item ao carrinho e vê-lo refletido na consulta em menos de 2 segundos em condições normais de uso.
- **SC-004**: Em testes com pelo menos 50 requisições simultâneas ao mesmo carrinho, o total de um produto no carrinho nunca excede o estoque disponível.
- **SC-005**: Após a expiração configurada contada da última alteração, 100% dos carrinhos inativos passam a ser retornados como vazios.
- **SC-006**: Em 100% das falhas (dados inválidos, estoque insuficiente, indisponibilidade), o usuário recebe mensagem compreensível e o carrinho não sofre alteração parcial.

## Assumptions

- Somente usuários autenticados possuem carrinho; carrinho de visitantes anônimos está fora do escopo.
- A autenticação existente da API é reutilizada para identificar o usuário; o identificador do dono do carrinho vem sempre da identidade autenticada.
- O carrinho não reserva nem desconta estoque; a reserva de inventário continua ocorrendo no fluxo de pedidos existente. A revalidação final do estoque na finalização do pedido está fora do escopo desta feature.
- A disponibilidade é lida do inventário existente, sem que o carrinho acesse diretamente dados pertencentes a outro serviço fora dos meios já adotados pelo projeto.
- O carrinho guarda apenas produto e quantidade; preços, nomes e totais são obtidos do catálogo no momento da exibição ou do pedido e estão fora desta estrutura inicial.
- O prazo padrão de expiração por inatividade é de 7 dias, ajustável por configuração.
- O limite padrão é de 50 produtos diferentes por carrinho, ajustável por configuração (ver FR-015).
- Integração com o front-end, finalização de pedido a partir do carrinho e promoções/cupons estão fora do escopo desta estrutura inicial.
- O Redis já faz parte da infraestrutura do projeto e estará disponível no ambiente.
