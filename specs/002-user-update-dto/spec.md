# Feature Specification: UserUpdatedInfo DTO for User Update

**Feature Branch**: `002-user-update-dto`
**Created**: 2026-04-17
**Status**: Draft
**Input**: User description: "Ao fazer update no usuário ocorre HTTP 400 exigindo PixKey, Password e IdDocument como obrigatórios. Para update eles não devem ser obrigatórios. A senha só é alterada no changePassword, então pode sair do UserInfo. Criar um UserUpdatedInfo para o método Update."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Update User Profile Without Providing Password or Optional Documents (Priority: P1)

Como consumidor autenticado da API, preciso atualizar atributos do meu próprio perfil (nome,
e-mail, foto, endereços, telefones, etc.) sem ser obrigado a reenviar a senha, a chave Pix ou o
número de documento a cada requisição.

**Why this priority**: Hoje qualquer chamada de update falha com HTTP 400 ("The PixKey field is
required", "The Password field is required", "The IdDocument field is required"), mesmo quando o
usuário só quer atualizar um campo trivial como o nome. Isso bloqueia completamente o fluxo de
manutenção de perfil — é o bug crítico que motivou esta feature.

**Independent Test**: Chamar `POST /User/update` com um payload contendo apenas os campos que o
consumidor deseja alterar (por exemplo só o `name`) e confirmar que a requisição retorna sucesso
com os dados atualizados, sem erros de validação para `PixKey`, `Password` ou `IdDocument`.

**Acceptance Scenarios**:

1. **Given** um usuário autenticado, **When** ele envia update contendo apenas `name` e `email`,
   **Then** o sistema aceita a requisição e retorna o perfil atualizado com HTTP 200.
2. **Given** um usuário autenticado, **When** ele envia update sem `password`, sem `pixKey` e sem
   `idDocument`, **Then** o sistema NÃO retorna erros de validação para esses campos.
3. **Given** um usuário autenticado com `pixKey` previamente preenchida, **When** ele envia update
   sem `pixKey`, **Then** o valor existente é preservado (não sobrescrito por vazio).
4. **Given** um usuário autenticado, **When** ele envia update com novo `pixKey` ou novo
   `idDocument`, **Then** o sistema aceita e persiste a alteração.

---

### User Story 2 - Password Change Remains Exclusive to ChangePassword Flow (Priority: P1)

Como mantenedor da plataforma, preciso garantir que a senha do usuário só seja alterada pelo
fluxo dedicado de `changePassword`, nunca pela rota de update — reduzindo a superfície de ataque
e simplificando auditoria.

**Why this priority**: Misturar alteração de senha com atualização de dados de perfil dificulta
trilhas de auditoria (quem mudou a senha? quando?) e aumenta o risco de alteração acidental.
Remover o campo `password` do contrato de update elimina toda uma classe de incidentes.

**Independent Test**: Inspecionar o contrato do endpoint de update e confirmar que o campo
`password` não existe; tentar enviar um `password` via update e confirmar que ele é ignorado
(não altera a senha do usuário persistida).

**Acceptance Scenarios**:

1. **Given** o contrato público do endpoint de update, **When** um consumidor inspeciona os
   campos aceitos, **Then** nenhum campo `password` está documentado.
2. **Given** um usuário com senha conhecida, **When** uma requisição de update enviar um campo
   `password` no JSON (extra), **Then** a senha persistida do usuário permanece inalterada.
3. **Given** o fluxo existente de `changePassword` e `changePasswordUsingHash`, **When** um
   consumidor precisar trocar a senha, **Then** ele continua usando exclusivamente esses
   endpoints — sem regressão no comportamento atual.

---

### User Story 3 - Existing Read Endpoints Continue Returning the Full User Profile (Priority: P2)

Como consumidor de leitura (ex.: telas de perfil), preciso continuar recebendo o perfil do
usuário completo (sem senha, que nunca foi exposta) em todos os endpoints que já retornavam
`UserInfo`.

**Why this priority**: Remover o campo `password` do `UserInfo` (objeto de leitura) não pode
quebrar nenhum consumidor existente de GET/lista. É importante para evitar regressão, mas
não bloqueia o fix principal (US1).

**Independent Test**: Executar as chamadas existentes de leitura (`GET /User/by-slug`, login,
etc.) e confirmar que o JSON de resposta continua contendo todos os campos antes expostos,
exceto `password` (que nunca deveria ter sido exposto de fato; ele sempre vinha vazio em
respostas por segurança).

**Acceptance Scenarios**:

1. **Given** uma chamada de leitura bem-sucedida de perfil, **When** a resposta é inspecionada,
   **Then** ela contém todos os campos públicos do usuário (id, slug, nome, e-mail, etc.).
2. **Given** as chamadas existentes que retornam `UserInfo`, **When** a feature é aplicada,
   **Then** nenhum dos consumidores atuais observa mudança de comportamento funcional.

---

### Edge Cases

- Enviar update com body completamente vazio `{}` → o sistema deve rejeitar com erro claro
  indicando que ao menos um campo válido é necessário (ou manter o perfil inalterado de forma
  idempotente, conforme política atual).
- Enviar update com `email` já pertencente a outro usuário → comportamento atual de rejeição
  deve ser preservado.
- Enviar update com `name` vazio (string vazia) → comportamento atual de validação do `name`
  deve ser preservado (campo continua obrigatório quando presente).
- Enviar update com `roles`, `phones` ou `addresses` explicitamente como listas vazias → deve
  manter paridade com o comportamento atual (não alterar semântica dessas coleções).
- Enviar update com `birthDate` inválida → comportamento atual de rejeição deve ser preservado.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema DEVE aceitar requisições de atualização de perfil sem exigir os campos
  `pixKey`, `password` e `idDocument` como obrigatórios.
- **FR-002**: O sistema DEVE preservar o valor atual de `pixKey` quando o campo for omitido ou
  nulo no payload de update.
- **FR-003**: O sistema DEVE preservar o valor atual de `idDocument` quando o campo for omitido
  ou nulo no payload de update.
- **FR-004**: O sistema DEVE ignorar qualquer campo `password` enviado no payload de update; a
  senha NÃO pode ser alterada por este endpoint.
- **FR-005**: O sistema DEVE continuar oferecendo fluxos dedicados de `changePassword` e
  `changePasswordUsingHash` como as únicas vias de alteração de senha.
- **FR-006**: O sistema DEVE remover o campo `password` do objeto de leitura `UserInfo` exposto
  em respostas de API, uma vez que essa informação não é nem deve ser retornada ao cliente.
- **FR-007**: O sistema DEVE manter retrocompatibilidade comportamental para campos que já eram
  obrigatórios ou opcionais (ex.: `name`, `email` continuam obrigatórios em update).
- **FR-008**: O sistema DEVE aceitar atualização dos campos `pixKey` e `idDocument` quando estes
  forem fornecidos explicitamente (ou seja, tornar opcional ≠ tornar read-only).
- **FR-009**: O sistema DEVE usar um contrato de entrada dedicado para o método de atualização
  (diferente do contrato de leitura), com os campos de update explicitamente opcionais quando
  apropriado.

### Key Entities *(include if feature involves data)*

- **UserInfo (contrato de leitura)**: Representação do perfil do usuário retornada por endpoints
  de leitura. Após esta feature, NÃO inclui campo `password`. Demais campos permanecem.
- **UserUpdatedInfo (novo contrato de entrada)**: Contrato aceito pelo endpoint de atualização
  de perfil. Inclui os mesmos campos editáveis do usuário, mas com `pixKey`, `idDocument` e
  quaisquer outros campos não obrigatórios marcados como opcionais. NÃO inclui `password`.
- **UserInsertedInfo (contrato de entrada existente, sem mudança)**: Contrato do fluxo de
  criação. Mantém suas obrigatoriedades atuais para garantir a correta criação do usuário.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Uma atualização de perfil contendo apenas `name` é aceita com sucesso (HTTP 200)
  em 100% das tentativas de consumidores autenticados válidos.
- **SC-002**: Nenhuma chamada de update retorna erro de validação mencionando `pixKey`,
  `password` ou `idDocument` quando esses campos são omitidos.
- **SC-003**: A senha persistida do usuário permanece inalterada em 100% das chamadas ao
  endpoint de update, independentemente do conteúdo do payload.
- **SC-004**: 0 regressões em fluxos existentes: login, criação, leitura por slug, troca de
  senha, recuperação por e-mail continuam funcionando com o mesmo comportamento anterior.
- **SC-005**: Consumidores existentes que usam os endpoints de leitura não precisam modificar
  seu código — o JSON de resposta permanece compatível (somente a chave `password`, que já
  vinha vazia por segurança, é removida).

## Assumptions

- A obrigatoriedade reportada no erro é resultado da validação automática do framework sobre
  propriedades não-nulas do objeto de entrada — não há validação explícita adicional sobre
  `pixKey`/`password`/`idDocument` no fluxo de update. Portanto, a correção envolve usar um
  objeto de entrada dedicado com essas propriedades opcionais.
- Os consumidores atuais do endpoint de update não dependem do campo `password` — historicamente
  ele nunca fez sentido em update (há fluxos dedicados de senha).
- O campo `password` nunca foi incluído em respostas JSON (por segurança), então removê-lo do
  objeto de leitura não altera o JSON observável pelos consumidores.
- Os testes unitários existentes (`UserServiceInsertUpdateTests`, `UserServiceTests`,
  `UserClientTests`) refletem o comportamento atual e serão atualizados para refletir o novo
  contrato de update.
- A biblioteca é publicada como pacote — alterar a assinatura do cliente ACL de update é uma
  mudança breaking na API pública; consumidores externos desta biblioteca precisarão atualizar
  suas chamadas após o bump de versão.
