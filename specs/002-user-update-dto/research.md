# Phase 0 Research: UserUpdatedInfo DTO for User Update

**Feature**: 002-user-update-dto
**Date**: 2026-04-17

Nenhum `NEEDS CLARIFICATION` foi levantado no Technical Context — o escopo está inteiramente
definido pelo payload de erro fornecido pelo usuário e pela diretriz explícita de criar
`UserUpdatedInfo`. Esta seção documenta as decisões técnicas e alternativas avaliadas.

---

## Decision 1 — Diagnóstico da causa-raiz

**Decisão**: O HTTP 400 observado vem da **validação implícita de nullable reference types** do
ASP.NET Core ModelBinder. O projeto habilita `<Nullable>enable</Nullable>` e os campos
`public string PixKey`, `public string Password`, `public string IdDocument` no DTO `UserInfo`
são declarados como não-nulos — com isso, o ModelBinder marca os campos automaticamente como
`[Required]` quando ausentes no payload JSON.

**Rationale**: Verificado lendo `NAuth/DTO/User/UserInfo.cs` (linhas 28–32). Não existe nenhum
`[Required]` explícito nem validador FluentValidation no projeto (grep por `FluentValidation`
retorna apenas a skill SKILL.md). Logo, o único agente de validação é o ModelBinder, e a
tipagem não-nula está causando o comportamento.

**Alternativas consideradas**:

- *Tornar os 3 campos nullable diretamente em `UserInfo`* — Rejeitado. `UserInfo` é contrato de
  leitura; adoinar nullable em campos de leitura quebraria consumidores que esperam strings
  não-nulas e obrigaria a propagar `?` em todo o cliente. Melhor é segregar: `UserInfo`
  continua sendo leitura; update recebe um DTO dedicado com propriedades nullable.
- *Adicionar `[Required(false)]` ou desligar validação implícita global* — Rejeitado. Solução
  cirúrgica no projeto inteiro gera efeitos colaterais em outros endpoints; a diretriz do
  usuário foi clara: criar `UserUpdatedInfo`.
- *Adotar FluentValidation agora* — Rejeitado. O projeto não usa; introduzi-lo só para este fix
  expande escopo e gera risco de regressão em outros pontos. Pode ser tema de feature futura.

---

## Decision 2 — Estrutura do DTO `UserUpdatedInfo`

**Decisão**: O DTO novo espelha `UserInsertedInfo` com as seguintes diferenças:

| Campo | `UserInsertedInfo` | `UserUpdatedInfo` |
|---|---|---|
| `UserId` | — (não existe) | `long UserId` (obrigatório) |
| `Name` | `string Name` | `string Name` (obrigatório) |
| `Email` | `string Email` | `string Email` (obrigatório) |
| `Slug` | `string Slug` | `string? Slug` |
| `ImageUrl` | `string ImageUrl` | `string? ImageUrl` |
| `IsAdmin` | `bool IsAdmin` | `bool IsAdmin` |
| `BirthDate` | `DateTime? BirthDate` | `DateTime? BirthDate` |
| `IdDocument` | `string IdDocument` | `string? IdDocument` (opcional) |
| `PixKey` | `string PixKey` | `string? PixKey` (opcional) |
| `Password` | `string Password` | **REMOVIDO** |
| `Status` | — | `int Status` |
| `Roles` / `Phones` / `Addresses` | coleções | coleções (mantidas) |

**Rationale**: O método `UserService.Update` já trata `IdDocument` e `PixKey` de forma defensiva
(se vazios/nulos, não valida nem sobrescreve com valor inválido). Não trata `Password` em momento
algum — logo o campo é puramente ornamental em update. `UserId` precisa estar presente porque o
método usa `user.UserId > 0` como guarda (linha 696 de `UserService.cs`).

**Alternativas consideradas**:

- *Reaproveitar `UserInsertedInfo` para update* — Rejeitado. Continuaria exigindo `Password` e
  os documentos; não resolve o bug.
- *Permitir `Status` ausente* — Rejeitado. `Status` é `int` (não-nullable por natureza no C#);
  o default `0` já existe e reflete o estado "inativo". Manter comportamento atual.

---

## Decision 3 — Remoção de `Password` do `UserInfo`

**Decisão**: Remover completamente a propriedade `Password` de `UserInfo.cs`. O fluxo de
troca de senha permanece exclusivo dos endpoints `POST /User/changePassword` e
`POST /User/changePasswordUsingHash`, que usam `ChangePasswordParam` e
`ChangePasswordUsingHashParam` respectivamente.

**Rationale**:

- `UserService.GetUserInfoFromModel` (linha 841–) nunca atribui `Password` ao objeto retornado —
  ou seja, o campo sempre saiu vazio em respostas. Nenhum consumidor depende dele.
- Nenhum endpoint da API referencia `UserInfo.Password` como entrada válida após esta feature
  (o único que lia era `Update`, agora substituído por `UserUpdatedInfo`).
- Mantê-lo seria convite a vazamento futuro: alguém poderia acidentalmente popular o campo em
  alguma rota.

**Alternativas consideradas**:

- *Marcar `Password` como `[JsonIgnore]`* — Rejeitado. Mantém a propriedade no C#, o que ainda
  permite uso em código interno. O objetivo é eliminar a possibilidade, não ocultar.

---

## Decision 4 — Compatibilidade do JSON de resposta

**Decisão**: Assumir que remover a chave `password` do JSON retornado em leituras é uma mudança
compatível — consumidores atuais não dependem dela. Nenhuma "chave morta" precisa ser preservada.

**Rationale**: Verificação por `grep` em `NAuth/ACL/UserClient.cs` e `IUserClient.cs`: nenhuma
leitura de `result?.Password` ou `user?.Password` após deserialização. Consumidores externos do
pacote NuGet que eventualmente leem o campo receberão `null` (comportamento atual), igual a
antes da remoção do campo — o diferencial é apenas o campo não aparecer no JSON, o que é
silenciosamente ignorado por `Newtonsoft.Json`/`System.Text.Json` durante deserialização.

**Alternativas consideradas**:

- *Manter `Password` com `[JsonIgnore]` para quem faz reflection* — Rejeitado (ver Decision 3).

---

## Decision 5 — Breaking change no pacote NuGet `NAuth`

**Decisão**: Aceitar breaking change em `IUserClient.UpdateAsync` e `UserClient.UpdateAsync`
(passam a receber `UserUpdatedInfo` em vez de `UserInfo`). Versionamento semântico via
GitVersion incrementará MINOR ou MAJOR conforme prefixo do commit.

**Rationale**: A assinatura anterior estava quebrada na prática (retornava HTTP 400 em todas as
chamadas do client ACL, porque o client enviava o `UserInfo` com `Password` vazio — que o
ModelBinder rejeitava). Corrigir o bug implica necessariamente mudar a assinatura pública.

**Alternativas consideradas**:

- *Adicionar overload `UpdateAsync(UserUpdatedInfo, string token)` e deixar a assinatura antiga
  marcada `[Obsolete]`* — Rejeitado. A assinatura antiga não produz nenhuma chamada bem-sucedida
  (é o bug). Manter overload obsoleto confunde mais do que ajuda.

**Nota operacional**: Para refletir breaking change semanticamente, usar prefixo de commit
`feature:` (minor bump do GitVersion) ou `breaking:` (major bump) conforme a política do projeto
— ver `CLAUDE.md` ("Commit message prefixes control version bumps").

---

## Decision 6 — Overloads de Phones/Addresses/Roles

**Decisão**: Trocar os overloads `InsertPhones(UserInfo)`, `InsertAddresses(UserInfo)`,
`InsertRoles(UserInfo)`, `ValidatePhones(UserInfo)`, `ValidateAddresses(UserInfo)` e
`ValidateRoles(UserInfo)` para receberem `UserUpdatedInfo` (o único chamador atual deles é o
`UserService.Update`). Os overloads que recebem `UserInsertedInfo` permanecem intocados.

**Rationale**: Método privado; troca mecânica; mesma semântica.

**Alternativas consideradas**:

- *Extrair interface comum `IUserEditableInfo`* — Rejeitado. Abstração prematura; três overloads
  já existem e funcionam.

---

## Referências

- `NAuth/DTO/User/UserInfo.cs` — DTO a modificar (remover `Password`).
- `NAuth/DTO/User/UserInsertedInfo.cs` — espelho estrutural para `UserUpdatedInfo`.
- `NAuth.Domain/Services/UserService.cs` linhas 689–802 — método `Update` e
  `ValidateUserForUpdate`.
- `NAuth.API/Controllers/UserController.cs` linhas 192–230 — endpoint a ajustar.
- `NAuth/ACL/UserClient.cs` linhas 152–167 — client ACL a ajustar.
- `NAuth/ACL/Interfaces/IUserClient.cs` linha 15 — interface do client.
- `.claude/skills/dotnet-architecture/SKILL.md` — regras de DTO e Clean Architecture.
- `CLAUDE.md` — regra de commit prefix e versionamento.

## Saídas

Todas as decisões resolvidas. Sem `NEEDS CLARIFICATION`. Pronto para Phase 1.
