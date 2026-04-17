# Feature Specification: Fortuno Tenant Onboarding

**Feature Branch**: `001-fortuno-tenant`
**Created**: 2026-04-17
**Status**: Draft
**Input**: User description: "Implemente um novo tenant chamado \"fortuno\", use a skill \"dotnet-multi-tenant\" e respeite o padrão já usando"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Onboard Fortuno as an Isolated Tenant (Priority: P1)

Como operador da plataforma NAuth, preciso registrar o tenant "fortuno" no ambiente de produção para
que a organização Fortuno possa autenticar seus próprios usuários, emitir seus próprios tokens e
armazenar seus dados em isolamento total dos outros tenants existentes (emagine, viralt, devblog,
bazzuca, monexup).

**Why this priority**: Sem esse registro, nenhum usuário Fortuno consegue autenticar — a plataforma
não reconhece o tenant, não localiza a base de dados correspondente e não emite tokens válidos.
Este é o requisito mínimo para entregar valor à organização Fortuno.

**Independent Test**: Pode ser totalmente validado enviando uma requisição de autenticação
identificada como tenant "fortuno" e verificando que o sistema: (1) reconhece o tenant, (2) acessa
a base de dados correta da Fortuno, (3) emite um token assinado com o segredo exclusivo do tenant.

**Acceptance Scenarios**:

1. **Given** o ambiente de produção está em execução com os tenants existentes, **When** um cliente
   Fortuno realiza uma requisição de autenticação identificando o tenant "fortuno", **Then** a
   plataforma roteia a requisição para a base de dados da Fortuno e responde com sucesso.
2. **Given** um token foi emitido para um usuário Fortuno, **When** esse token é apresentado em uma
   chamada subsequente, **Then** a plataforma valida a assinatura usando o segredo do tenant
   "fortuno" e aceita a requisição.
3. **Given** os tenants emagine, viralt, devblog, bazzuca e monexup estão operando normalmente,
   **When** o tenant "fortuno" é adicionado, **Then** nenhum dos tenants pré-existentes sofre
   interrupção, alteração de comportamento ou vazamento de dados.

---

### User Story 2 - Operar Fortuno com Configuração Externa Segura (Priority: P2)

Como operador de infraestrutura, preciso fornecer a connection string e o segredo JWT do tenant
"fortuno" via variáveis de ambiente (não versionadas) para que credenciais sensíveis nunca sejam
expostas no repositório de código.

**Why this priority**: Atende ao Princípio V da constituição (segurança) e ao padrão já adotado
pelos tenants existentes. É crítico, mas depende do tenant estar tecnicamente registrado (US1).

**Independent Test**: Pode ser validado verificando que o arquivo de exemplo de variáveis de
ambiente declara os placeholders do tenant Fortuno e que a orquestração de deploy mapeia tais
variáveis para a configuração da aplicação sem que nenhum valor real apareça em arquivos
versionados.

**Acceptance Scenarios**:

1. **Given** o arquivo de exemplo de variáveis de ambiente de produção é consultado por um novo
   operador, **When** ele procura os segredos do tenant "fortuno", **Then** encontra duas
   variáveis (connection string e segredo JWT) claramente identificadas e documentadas como
   placeholders.
2. **Given** o ambiente de produção é provisionado com valores reais nas variáveis da Fortuno,
   **When** a aplicação inicia, **Then** ela carrega corretamente as credenciais do tenant sem
   ler nenhum valor literal dos arquivos versionados.

---

### Edge Cases

- Quando o segredo JWT da Fortuno não atinge o comprimento mínimo exigido pela plataforma (64
  caracteres), a inicialização do tenant deve falhar de forma observável — não silenciosa.
- Quando a connection string da Fortuno aponta para um banco inacessível, requisições ao tenant
  "fortuno" devem falhar com erro de indisponibilidade, sem afetar os demais tenants.
- Quando um cliente envia uma requisição com identificação de tenant "fortuno" mas usando token
  assinado pelo segredo de outro tenant, a validação deve rejeitar a requisição como não autorizada.
- Quando o identificador "fortuno" é enviado com diferença de caixa (ex.: "Fortuno", "FORTUNO"), o
  comportamento deve ser consistente com o dos tenants pré-existentes (mesmo tratamento já adotado
  para bazzuca/monexup).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A plataforma DEVE reconhecer "fortuno" como tenant válido ao receber requisições
  identificadas com esse nome.
- **FR-002**: A plataforma DEVE isolar os dados do tenant "fortuno" em uma base de dados
  exclusiva, sem compartilhamento de tabelas ou registros com os demais tenants.
- **FR-003**: A plataforma DEVE assinar e validar tokens de autenticação do tenant "fortuno"
  usando um segredo exclusivo, distinto do segredo de qualquer outro tenant.
- **FR-004**: A plataforma DEVE ler connection string e segredo JWT do tenant "fortuno" a partir
  de variáveis de ambiente (não de valores literais versionados).
- **FR-005**: A documentação de variáveis de ambiente de produção DEVE incluir exemplos/placeholders
  para as credenciais do tenant "fortuno", seguindo o mesmo formato usado pelos tenants existentes.
- **FR-006**: A orquestração de deploy de produção DEVE mapear as variáveis de ambiente do tenant
  "fortuno" para a configuração da aplicação, seguindo o mesmo padrão de mapeamento adotado para
  os tenants existentes.
- **FR-007**: A inclusão do tenant "fortuno" NÃO DEVE alterar o comportamento, configuração ou
  isolamento de qualquer tenant pré-existente (emagine, viralt, devblog, bazzuca, monexup).
- **FR-008**: A plataforma DEVE associar ao tenant "fortuno" um identificador de bucket
  (armazenamento) próprio, seguindo o mesmo padrão de nomenclatura dos demais tenants.

### Key Entities *(include if feature involves data)*

- **Tenant "fortuno"**: Unidade lógica de isolamento. Possui identificador próprio, base de dados
  própria, segredo JWT próprio e identificador de bucket próprio. Relaciona-se com seus próprios
  usuários, papéis e recursos, sem cruzar dados com outros tenants.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Um usuário cadastrado na base da Fortuno consegue autenticar-se com sucesso em até
  3 segundos após a subida do ambiente com o tenant configurado.
- **SC-002**: 100% das requisições dos 5 tenants pré-existentes continuam operando sem regressão
  funcional ou de performance após a adição do tenant "fortuno".
- **SC-003**: 0 valores reais de segredo ou connection string do tenant "fortuno" aparecem em
  arquivos versionados do repositório.
- **SC-004**: Um novo operador consegue provisionar o tenant "fortuno" em um ambiente limpo em até
  15 minutos, apenas seguindo a documentação de variáveis de ambiente existente.
- **SC-005**: Um token emitido pelo tenant "fortuno" é rejeitado em 100% das tentativas de uso
  contra recursos de outros tenants.

## Assumptions

- A equipe de infraestrutura fornecerá a connection string PostgreSQL e o segredo JWT (mínimo 64
  caracteres) do tenant Fortuno antes do deploy — a criação do banco em si é responsabilidade de
  infraestrutura, fora do escopo desta feature.
- O nome canônico em minúsculas do tenant é "fortuno"; a nomenclatura de bucket segue o padrão
  PascalCase adotado para os tenants existentes (ex.: "Fortuno").
- O fluxo de desenvolvimento local não exige o tenant Fortuno; a feature foca em produção, em
  paridade com o padrão das adições anteriores (bazzuca, monexup).
- A existência de usuários, papéis ou dados de seed para o tenant Fortuno está fora do escopo —
  essas entidades serão criadas via fluxos já existentes de cadastro de usuário.
- O ambiente Docker local não é utilizado durante a implementação (conforme Princípio II da
  constituição — Docker não acessível localmente).
