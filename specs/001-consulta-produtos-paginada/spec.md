# Feature Specification: Consulta de Produtos Paginada

**Feature Branch**: `feat/consulta-produtos-paginada`

**Created**: 2026-10-29

**Status**: Draft

**Input**: User description: "consulta de produtos paginada - O usuário por meio de uma rota get deve ser capaz de obter uma lista paginada de produtos. Essa rota deve permitir o usuário pesquisar por nome ou filtrar por categoria. A rota além de trazer as informações a respeito do produto ela deve também trazer um campo informando se o produto está tem saldo disponivel no estoque."

## Clarifications

### Session 2026-10-29

- Q: Como a lista deve ser ordenada? → A: Em ordem alfabética por nome, com direção crescente ou decrescente à escolha do usuário (crescente por padrão).
- Q: Onde ficam os produtos sem saldo no inventário? → A: Sempre no final da lista, em qualquer direção de ordenação, e a ordenação alfabética continua valendo dentro do grupo sem saldo.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Listar produtos de forma paginada (Priority: P1)

Um usuário autenticado consulta o catálogo de produtos e recebe uma página de produtos por vez, junto com as informações necessárias para navegar pelas demais páginas (página atual, tamanho da página, total de itens e total de páginas). Cada produto retornado traz suas informações cadastrais e um indicador informando se há saldo disponível em estoque.

**Why this priority**: É o núcleo da funcionalidade: sem a listagem paginada não há catálogo navegável. Entrega valor sozinha como MVP.

**Independent Test**: Cadastrar mais produtos do que cabem em uma página, consultar a listagem sem filtros e verificar que apenas a quantidade de itens da página é retornada, com os dados de paginação corretos e o indicador de estoque em cada item.

**Acceptance Scenarios**:

1. **Given** existem 25 produtos cadastrados e o tamanho de página é 10, **When** o usuário consulta a primeira página, **Then** recebe 10 produtos e a informação de que há 25 itens no total distribuídos em 3 páginas.
2. **Given** existem 25 produtos cadastrados e o tamanho de página é 10, **When** o usuário consulta a terceira página, **Then** recebe os 5 produtos restantes.
3. **Given** o usuário não informa página nem tamanho de página, **When** consulta a listagem, **Then** o sistema usa valores padrão (primeira página, tamanho padrão) e retorna o resultado normalmente.
4. **Given** um produto possui saldo disponível maior que zero no estoque, **When** ele aparece na listagem, **Then** o indicador de disponibilidade desse produto é positivo.
5. **Given** um produto não possui saldo disponível (zero ou sem registro de estoque), **When** ele aparece na listagem, **Then** o indicador de disponibilidade desse produto é negativo.

---

### User Story 2 - Pesquisar produtos por nome (Priority: P2)

O usuário informa um termo de busca e a listagem retorna apenas os produtos cujo nome contém esse termo, mantendo a paginação e o indicador de estoque.

**Why this priority**: Facilita a localização de um produto específico em um catálogo grande, sendo a principal forma de refinar a consulta depois da listagem básica.

**Independent Test**: Consultar a listagem informando parte do nome de um produto conhecido e verificar que somente produtos cujo nome contém o termo são retornados.

**Acceptance Scenarios**:

1. **Given** existem produtos "Camiseta Azul", "Camiseta Preta" e "Caneca", **When** o usuário pesquisa por "camiseta", **Then** recebe somente os dois produtos de camiseta, independentemente de maiúsculas/minúsculas.
2. **Given** nenhum produto contém o termo pesquisado, **When** o usuário realiza a pesquisa, **Then** recebe uma lista vazia com total de itens igual a zero.
3. **Given** a pesquisa retorna mais itens que o tamanho da página, **When** o usuário navega entre as páginas, **Then** os totais refletem apenas os itens que correspondem à pesquisa.

---

### User Story 3 - Filtrar produtos por categoria (Priority: P2)

O usuário seleciona uma categoria e a listagem retorna somente os produtos pertencentes a ela, mantendo a paginação e o indicador de estoque.

**Why this priority**: Permite navegar pelo catálogo por departamento/tipo de produto; complementa a pesquisa por nome.

**Independent Test**: Consultar a listagem informando o identificador de uma categoria com produtos cadastrados e verificar que todos os itens retornados pertencem a ela.

**Acceptance Scenarios**:

1. **Given** existem produtos em categorias diferentes, **When** o usuário filtra por uma categoria, **Then** recebe apenas os produtos dessa categoria.
2. **Given** o usuário informa uma categoria sem produtos, **When** consulta a listagem, **Then** recebe uma lista vazia com total de itens igual a zero.
3. **Given** o usuário informa pesquisa por nome e filtro de categoria ao mesmo tempo, **When** consulta a listagem, **Then** recebe somente produtos que atendem aos dois critérios.

---

### User Story 4 - Ordenar produtos alfabeticamente com sem-saldo ao final (Priority: P2)

O usuário escolhe se a lista é ordenada por nome em ordem alfabética crescente ou decrescente. Em qualquer direção, os produtos sem saldo disponível no inventário aparecem sempre depois de todos os produtos com saldo.

**Why this priority**: Destaca primeiro os produtos que o cliente pode de fato comprar e dá controle sobre a ordem de navegação.

**Independent Test**: Cadastrar produtos com e sem saldo, consultar nas duas direções e verificar que os produtos com saldo vêm primeiro, ordenados por nome na direção escolhida, seguidos dos produtos sem saldo, também ordenados por nome na mesma direção.

**Acceptance Scenarios**:

1. **Given** os produtos "Banana" (com saldo), "Abacate" (sem saldo), "Caju" (com saldo) e "Amora" (com saldo), **When** o usuário consulta em ordem crescente, **Then** a ordem é Amora, Banana, Caju, Abacate.
2. **Given** os mesmos produtos, **When** o usuário consulta em ordem decrescente, **Then** a ordem é Caju, Banana, Amora, Abacate.
3. **Given** o usuário não informa a direção da ordenação, **When** consulta a listagem, **Then** a ordem crescente é aplicada.
4. **Given** produtos sem saldo e com saldo distribuídos em várias páginas, **When** o usuário percorre as páginas, **Then** nenhum produto com saldo aparece depois de um produto sem saldo.

---

### Edge Cases

- Página solicitada além do total de páginas: retorna lista vazia mantendo os totais corretos, sem erro.
- Direção de ordenação diferente de crescente/decrescente: o sistema rejeita a requisição com mensagem de erro clara.
- Todos os produtos da consulta sem saldo (ou todos com saldo): a ordenação alfabética se aplica normalmente ao único grupo existente.
- Número de página ou tamanho de página menor que 1 ou não numérico: o sistema rejeita a requisição com mensagem de erro clara.
- Tamanho de página acima do máximo permitido: o sistema rejeita a requisição ou limita ao máximo permitido, de forma documentada.
- Identificador de categoria em formato inválido: o sistema rejeita a requisição com mensagem de erro clara.
- Identificador de categoria válido, porém inexistente: retorna lista vazia.
- Termo de pesquisa vazio ou apenas com espaços: é tratado como ausência de pesquisa.
- Termo de pesquisa com caracteres especiais: é tratado como texto literal, sem alterar o comportamento da consulta.
- Produto com saldo reservado igual ao saldo total: o saldo efetivamente disponível é zero, logo o indicador é negativo.
- Produto sem categoria associada: aparece na listagem sem filtro de categoria e não aparece quando uma categoria é filtrada.
- Usuário não autenticado: acesso negado, seguindo o mesmo comportamento das demais consultas de produto.
- Indisponibilidade temporária da informação de estoque: a requisição não retorna dados incorretos de disponibilidade; falha de forma explícita ou usa o último valor conhecido conforme o comportamento já adotado para o estoque.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST disponibilizar uma consulta de leitura (rota GET) de produtos que retorne os resultados de forma paginada.
- **FR-002**: A consulta MUST aceitar o número da página e o tamanho da página como parâmetros opcionais, adotando valores padrão quando omitidos.
- **FR-003**: O sistema MUST impor um tamanho máximo de página e rejeitar ou limitar valores acima dele.
- **FR-004**: A resposta MUST incluir, além dos itens da página, a página atual, o tamanho da página, o total de itens que atendem aos critérios e o total de páginas.
- **FR-005**: A consulta MUST permitir pesquisar produtos pelo nome, retornando os produtos cujo nome contém o termo informado, sem diferenciar maiúsculas de minúsculas.
- **FR-006**: A consulta MUST permitir filtrar produtos por categoria, retornando apenas os produtos pertencentes à categoria informada.
- **FR-007**: A pesquisa por nome e o filtro por categoria MUST poder ser combinados, aplicando-se ambos os critérios simultaneamente; quando nenhum for informado, todos os produtos são listados.
- **FR-008**: Cada produto retornado MUST incluir as mesmas informações cadastrais já expostas na consulta de produto individual (identificador, nome, descrição, foto, categoria, preço unitário).
- **FR-009**: Cada produto retornado MUST incluir um campo indicando se há saldo disponível em estoque (verdadeiro quando a quantidade disponível efetiva é maior que zero; falso caso contrário, inclusive quando não há registro de estoque).
- **FR-010**: O indicador de disponibilidade MUST considerar apenas o saldo realmente disponível para venda, descontando quantidades já reservadas.
- **FR-011**: A consulta MUST permitir ordenar os resultados alfabeticamente por nome, em direção crescente ou decrescente, sendo crescente o padrão quando a direção não for informada.
- **FR-012**: Independentemente da direção escolhida, os produtos sem saldo disponível (mesmo critério do FR-009) MUST aparecer sempre depois de todos os produtos com saldo, em todas as páginas; dentro de cada grupo (com saldo e sem saldo) a ordem alfabética por nome na direção escolhida MUST ser mantida.
- **FR-013**: A ordenação MUST ser estável e determinística entre páginas (desempate por um critério fixo quando nomes forem iguais), de modo que um item não se repita nem seja omitido ao navegar entre páginas.
- **FR-014**: O sistema MUST rejeitar parâmetros inválidos (página ou tamanho inferiores a 1, valores não numéricos, identificador de categoria malformado, direção de ordenação diferente de crescente/decrescente) com mensagem de erro clara.
- **FR-015**: A consulta MUST exigir usuário autenticado, seguindo o mesmo padrão de acesso das demais consultas de produto.
- **FR-016**: A consulta MUST NOT alterar nenhum dado de produto ou de estoque.

### Key Entities *(include if feature involves data)*

- **Produto**: Item do catálogo com identificador, nome, descrição, foto, categoria e preço unitário. É o elemento listado.
- **Categoria**: Agrupamento de produtos, identificada por id e nome. Usada como critério de filtro.
- **Inventário (saldo de estoque)**: Quantidade disponível e quantidade reservada de cada produto. Base para determinar o indicador de disponibilidade.
- **Página de resultados**: Conjunto de produtos de uma página, acompanhado de página atual, tamanho da página, total de itens e total de páginas.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Usuários obtêm a primeira página de produtos em menos de 1 segundo em um catálogo de até 10.000 produtos.
- **SC-002**: 100% dos produtos retornados trazem o indicador de disponibilidade de estoque, e ele corresponde ao saldo real em todos os casos verificados (com saldo, sem saldo, sem registro de estoque, totalmente reservado).
- **SC-003**: Ao pesquisar por nome ou filtrar por categoria, 100% dos itens retornados atendem ao critério informado e nenhum produto correspondente é omitido.
- **SC-004**: Percorrendo todas as páginas de uma consulta, a união dos itens contém cada produto correspondente exatamente uma vez.
- **SC-005**: Usuários localizam um produto específico pelo nome em no máximo uma consulta, sem precisar percorrer manualmente todo o catálogo.
- **SC-006**: Em 100% das consultas, nas duas direções de ordenação, nenhum produto sem saldo aparece antes de um produto com saldo, e dentro de cada grupo os nomes seguem a ordem alfabética escolhida.

## Assumptions

- Os usuários da consulta são usuários autenticados da plataforma, com a mesma regra de acesso da consulta de produto por identificador já existente.
- O tamanho de página padrão é 10 itens e o máximo permitido é 100 itens; a numeração de páginas começa em 1.
- A pesquisa por nome é do tipo "contém" e insensível a maiúsculas/minúsculas; não há correspondência por descrição nem busca por similaridade (tolerância a erros de digitação).
- O filtro por categoria aceita uma única categoria por consulta, identificada pelo seu identificador.
- A ordenação é sempre alfabética por nome do produto; o usuário escolhe apenas a direção (crescente ou decrescente). Ordenação por outros campos (preço, data) está fora do escopo.
- Filtros por faixa de preço, por disponibilidade de estoque e a exibição da quantidade exata em estoque estão fora do escopo; apenas o indicador de disponibilidade (sim/não) é exposto.
- O dado de saldo de estoque já é mantido pela plataforma (quantidade disponível e reservada por produto) e será reutilizado; esta funcionalidade somente lê esse dado.
- Esta funcionalidade é uma consulta de leitura e não altera o comportamento das rotas existentes de produto.
