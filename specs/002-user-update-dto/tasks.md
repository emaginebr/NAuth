---

description: "Task list for UserUpdatedInfo DTO for User Update"
---

# Tasks: UserUpdatedInfo DTO for User Update

**Input**: Design documents from `C:\repos\NAuth\NAuth\specs\002-user-update-dto\`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/user-update.md, quickstart.md

**Tests**: Existem tasks de teste — a feature modifica contratos públicos (DTO novo + DTO
alterado) consumidos por suítes de teste já existentes (`UserServiceInsertUpdateTests`,
`UserClientTests`, `UserServiceTests`). As atualizações desses testes são obrigatórias para
manter a suíte verde; novos testes só são criados se houver cobertura crítica faltando.

**Organization**: Tasks agrupadas por user story para permitir implementação e validação
independentes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependências).
- **[Story]**: US1, US2 ou US3.
- Paths absolutos/reais.

## Path Conventions

Clean Architecture .NET 8. Arquivos principais:

- `NAuth/DTO/User/` — DTOs.
- `NAuth/ACL/` — client ACL.
- `NAuth.Domain/Services/` — service + interface.
- `NAuth.API/Controllers/` — controller.
- `NAuth.Test/` — testes (xUnit + Moq).

---

## Phase 1: Setup

**Purpose**: Verificação de pré-requisitos antes de começar.

- [X] T001 Confirmar branch ativa `002-user-update-dto` via `git status` e que build/test do `main` estão na baseline (11 falhas pré-existentes documentadas na feature anterior)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Mudanças estruturais nos DTOs que bloqueiam todas as user stories.

**⚠️ CRITICAL**: Nenhuma user story pode começar antes destas tasks concluírem.

- [X] T002 [P] Criar novo DTO em `C:\repos\NAuth\NAuth\NAuth\DTO\User\UserUpdatedInfo.cs` com os campos: `long UserId`, `string? Slug`, `string? ImageUrl`, `string Name`, `string Email`, `bool IsAdmin`, `DateTime? BirthDate` (com `[JsonConverter(typeof(NullableDateTimeConverter))]`), `string? IdDocument`, `string? PixKey`, `int Status`, `IList<RoleInfo> Roles`, `IList<UserPhoneInfo> Phones`, `IList<UserAddressInfo> Addresses` — todos com `[JsonPropertyName("camelCase")]`; namespace file-scoped `namespace NAuth.DTO.User;`
- [X] T003 [P] Editar `C:\repos\NAuth\NAuth\NAuth\DTO\User\UserInfo.cs` removendo a propriedade `Password` e seu atributo `[JsonPropertyName("password")]` (linhas 31-32 do arquivo original); manter todas as outras propriedades intactas

**Checkpoint**: Dois arquivos de DTO prontos. Build pode falhar temporariamente até US1 propagar a nova assinatura — esperado.

---

## Phase 3: User Story 1 - Update User Profile Without Providing Password or Optional Documents (Priority: P1) 🎯 MVP

**Goal**: Fazer com que `POST /User/update` aceite payloads sem `PixKey`, `Password` ou `IdDocument`, propagando `UserUpdatedInfo` por toda a cadeia Controller → Service → Client ACL.

**Independent Test**: `POST /User/update` com apenas `{ "userId", "name", "email" }` retorna HTTP 200 e perfil atualizado; nenhum erro de validação para `PixKey`, `Password`, `IdDocument`.

### Implementation for User Story 1

- [X] T004 [US1] Atualizar assinatura em `C:\repos\NAuth\NAuth\NAuth.Domain\Services\Interfaces\IUserService.cs` linha 20: trocar `Task<IUserModel> Update(UserInfo user);` por `Task<IUserModel> Update(UserUpdatedInfo user);`
- [X] T005 [US1] Editar `C:\repos\NAuth\NAuth\NAuth.Domain\Services\UserService.cs`: (a) alterar assinatura `public async Task<IUserModel> Update(UserInfo user)` (linha 689) para `Update(UserUpdatedInfo user)`; (b) alterar assinatura `private async Task ValidateUserForUpdate(UserInfo user, IUserModel model)` (linha 773) para receber `UserUpdatedInfo`; (c) trocar o tipo do parâmetro nos 6 overloads privados usados por `Update`: `InsertPhones(UserInfo)` linha 387, `InsertAddresses(UserInfo)` linha 401, `InsertRoles(UserInfo)` linha 432, `ValidatePhones(UserInfo)` linha 515, `ValidateAddresses(UserInfo)` linha 552, `ValidateRoles(UserInfo)` linha 468 — cada um passa a receber `UserUpdatedInfo`; (d) lógica de corpo intocada
- [X] T006 [US1] Editar `C:\repos\NAuth\NAuth\NAuth.API\Controllers\UserController.cs` linha 194: trocar `[FromBody] UserInfo user` por `[FromBody] UserUpdatedInfo user` no método `Update`; lógica do controller intocada (todas as referências como `user.UserId`, `user.Name`, `user.Email` continuam válidas com o novo DTO)
- [X] T007 [US1] Atualizar assinatura em `C:\repos\NAuth\NAuth\NAuth\ACL\Interfaces\IUserClient.cs` linha 15: trocar `Task<UserInfo?> UpdateAsync(UserInfo user, string token);` por `Task<UserInfo?> UpdateAsync(UserUpdatedInfo user, string token);`
- [X] T008 [US1] Editar `C:\repos\NAuth\NAuth\NAuth\ACL\UserClient.cs` linha 152: trocar assinatura de `UpdateAsync(UserInfo user, string token)` para `UpdateAsync(UserUpdatedInfo user, string token)`; corpo do método (serialização e POST) inalterado
- [X] T009 [US1] Editar `C:\repos\NAuth\NAuth\NAuth.Test\Domain\Services\UserServiceInsertUpdateTests.cs`: em todos os testes que chamam `UserService.Update(...)`, substituir instâncias de `new UserInfo { ... }` usadas como argumento por `new UserUpdatedInfo { ... }`, removendo atribuições a `.Password` nesses testes específicos de update; testes de `Insert` permanecem usando `UserInsertedInfo`
- [X] T010 [US1] Editar `C:\repos\NAuth\NAuth\NAuth.Test\ACL\UserClientTests.cs` região `#region UpdateAsync Tests` (linhas 371 em diante): substituir instâncias de `new UserInfo { ... }` passadas para `userClient.UpdateAsync(...)` por `new UserUpdatedInfo { ... }`
- [X] T011 [US1] Rodar `dotnet build` na raiz do repo e confirmar build verde (sem novos erros de compilação)
- [X] T012 [US1] Rodar `dotnet test` na raiz do repo e confirmar baseline: 11 falhas pré-existentes, nenhuma falha nova introduzida em `UserServiceInsertUpdateTests`, `UserServiceTests` ou `UserClientTests`

**Checkpoint**: Endpoint de update funcional com payload parcial; suíte de testes na mesma baseline do `main`.

---

## Phase 4: User Story 2 - Password Change Remains Exclusive to ChangePassword Flow (Priority: P1)

**Goal**: Garantir que o campo `Password` não exista mais em nenhum contrato consumido pelo fluxo de update e que o fluxo de `changePassword`/`changePasswordUsingHash` permaneça intacto.

**Independent Test**: `grep` por `UserInfo.Password` ou `user.Password` no código de produção retorna zero ocorrências em caminhos que não sejam `Insert`; teste de integração/smoke confirma que enviar `"password"` no JSON do update não altera a senha persistida.

### Implementation for User Story 2

- [X] T013 [US2] Rodar `grep -rn "\.Password" NAuth NAuth.API NAuth.Domain` e confirmar que as únicas ocorrências remanescentes estão em: (a) `UserService.cs` linhas 633 e 672 (path de `Insert` usando `UserInsertedInfo.Password`), (b) DTOs de changePassword (`ChangePasswordParam`, `ChangePasswordUsingHashParam`), (c) `UserInsertedInfo.cs`. Nenhuma referência via `UserInfo.Password` ou `UserUpdatedInfo.Password` deve existir
- [X] T014 [US2] Revisar `C:\repos\NAuth\NAuth\NAuth.Test\Domain\Services\UserServiceTests.cs` e, se houver algum teste instanciando `UserInfo` com `Password`, remover a atribuição (o campo não existe mais); se um teste dependia de comparação com `Password`, substituir por `UserInsertedInfo` se for teste de Insert ou remover a comparação se for irrelevante
- [X] T015 [US2] Verificar que `C:\repos\NAuth\NAuth\NAuth.Domain\Services\UserService.cs` método `GetUserInfoFromModel` (linha 841+) não tenta mais atribuir `.Password` ao objeto retornado (ele já não atribuía; confirmar por leitura)

**Checkpoint**: Campo `Password` inexistente em `UserInfo`/`UserUpdatedInfo`; senha imutável via update.

---

## Phase 5: User Story 3 - Existing Read Endpoints Continue Returning the Full User Profile (Priority: P2)

**Goal**: Confirmar que a remoção de `UserInfo.Password` não gera regressão em endpoints de leitura (`GET /User/by-slug/{slug}`, `POST /User/loginWithEmail`, etc.).

**Independent Test**: Chamar um endpoint de leitura, inspecionar JSON de resposta — deve conter todos os campos públicos do usuário, **sem** a chave `password`.

### Implementation for User Story 3

- [X] T016 [US3] Rodar `grep -rn "UserInfo" NAuth.Test` e garantir que todos os testes existentes de leitura (GetBySlug, LoginWithEmail, etc.) não quebraram — os mocks que retornam `UserInfo` sem `Password` continuam válidos porque o campo removido nunca foi preenchido
- [X] T017 [US3] Rodar novamente `dotnet test --filter "FullyQualifiedName~UserClient" ` e confirmar que os testes de métodos de leitura (`GetBySlugAsync`, `LoginWithEmailAsync`, etc.) continuam passando

**Checkpoint**: Contratos de leitura sem regressão.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentação, smoke test manual e commit.

- [X] T018 [P] Atualizar `C:\repos\NAuth\NAuth\NAuth\README.md` linha 348: trocar `Task<UserInfo?> UpdateAsync(UserInfo user, string token);` por `Task<UserInfo?> UpdateAsync(UserUpdatedInfo user, string token);`
- [X] T019 [P] Atualizar `C:\repos\NAuth\NAuth\docs\USER_API_DOCUMENTATION.md`: seção do endpoint `POST /User/update` — substituir exemplo de payload `UserInfo` pelo novo `UserUpdatedInfo`; remover coluna/menção a `password` como campo aceito; adicionar nota de que a senha só é alterada via `changePassword`/`changePasswordUsingHash`
- [ ] T020 (DEFERRED — requires running API + Bruno) Executar smoke test manual do `quickstart.md` Passo 4 (chamar `POST /User/update` com `{ "userId", "name", "email" }` via Bruno) e confirmar HTTP 200
- [ ] T021 (DEFERRED — requires running API + DB inspection) Executar smoke test manual do `quickstart.md` Passos 5.1 e 5.2 (preservação de `pixKey` omitida; atualização explícita de `pixKey`)
- [ ] T022 (DEFERRED — requires running API + DB inspection) Executar smoke test manual do `quickstart.md` Passo 6.1 (enviar `password` no JSON de update e confirmar que a senha no banco não mudou)
- [ ] T023 (DEFERRED — requires user authorization) Commitar as mudanças com mensagem `feature: add UserUpdatedInfo DTO and remove Password from UserInfo` (prefixo `feature:` = minor bump do GitVersion, pois é breaking change na API pública do pacote NAuth — alternativa: `breaking:` para major bump) e abrir PR contra `main`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sem dependências.
- **Foundational (Phase 2)**: T002 e T003 podem rodar em paralelo; ambas são pré-requisitos de TODO o trabalho de user story.
- **User Story 1 (Phase 3)**: Depende de T002 (existência de `UserUpdatedInfo`) e T003 (`UserInfo` sem `Password`). Internamente, T004–T010 são sequenciais pela cadeia de compilação — se T004 for feito sem T005, o build quebra. Recomendado: fazer T004→T005→T006→T007→T008 em ordem, depois T009+T010 em paralelo.
- **User Story 2 (Phase 4)**: Depende da conclusão de US1 (caso contrário ainda existem referências a `UserInfo.Password` no código de update).
- **User Story 3 (Phase 5)**: Depende da conclusão de US1 (suíte compilada) e de T003.
- **Polish (Phase 6)**: T018 e T019 em paralelo; T020–T022 requerem a API rodando localmente; T023 depende de tudo.

### User Story Dependencies

- **US1 (P1) = MVP**. Sem US1 o bug persiste.
- **US2 (P1)**: Depende de US1 + T003. É em grande parte auditoria/compliance — a remoção estrutural do `Password` já ocorreu em T003, US2 só valida que nada ficou escondido.
- **US3 (P2)**: Depende de US1 + T003. Também é verificação.

### Within Each User Story

- US1: T004 → T005 → T006 → T007 → T008 → (T009 ∥ T010) → T011 → T012. Ordem importa por compilação.
- US2: T013 → T014 → T015. Ordem lógica de verificação.
- US3: T016 → T017.

### Parallel Opportunities

- **Phase 2**: T002 ∥ T003 (arquivos distintos).
- **Phase 3**: T009 ∥ T010 (arquivos de teste distintos) após T004–T008.
- **Phase 6**: T018 ∥ T019 (docs distintos).

---

## Parallel Example: Foundational

```text
# Dois arquivos independentes — um novo e um modificado:
Task: "Criar NAuth/DTO/User/UserUpdatedInfo.cs"        # T002
Task: "Remover Password de NAuth/DTO/User/UserInfo.cs" # T003
```

## Parallel Example: US1 test updates

```text
# Após T004–T008 compilarem, atualizar testes em paralelo:
Task: "Atualizar NAuth.Test/Domain/Services/UserServiceInsertUpdateTests.cs" # T009
Task: "Atualizar NAuth.Test/ACL/UserClientTests.cs"                          # T010
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Phase 1 (Setup) → Phase 2 (Foundational) em paralelo → Phase 3 (US1).
2. **STOP & VALIDATE**: `dotnet build` + `dotnet test` + smoke test manual do fix.
3. Deploy/demo se aceito.

### Incremental Delivery

1. Foundation pronto após T002+T003.
2. US1 completo → bug corrigido + breaking change propagada.
3. US2 valida inexistência de `Password` em caminhos de update.
4. US3 valida não-regressão nos endpoints de leitura.
5. Polish: docs + commit + PR.

### Paralelização entre executores

1. Executor A: T002 (criar DTO) → T004 (interface service) → T005 (service) → T006 (controller).
2. Executor B: T003 (remover Password) → T007 (interface client) → T008 (client).
3. Convergem em T009+T010 (testes em paralelo), T011+T012 (build/test), depois phases 4/5/6.

---

## Notes

- [P] = arquivos distintos, sem dependências de task incompleta.
- Mudança é **breaking change pública** no pacote NuGet — justifica commit com prefixo
  `feature:` (minor) ou `breaking:` (major) conforme política do `CLAUDE.md` + `GitVersion.yml`.
- `FluentValidation` NÃO é introduzida nesta feature (Princípio I + Decision 1 do
  `research.md`).
- `dotnet-architecture` é a skill aplicável; convenções do Princípio III e da skill preservadas.
- Docker local proibido (Princípio II) — smoke tests usam `dotnet run` + Bruno apontando para
  banco dev local.
- Avoid: introduzir validação explícita extra (`[Required]`, FluentValidation) para `PixKey`/
  `IdDocument` — a semântica atual de "opcional + preservar valor existente se omitido" é
  suficiente.
