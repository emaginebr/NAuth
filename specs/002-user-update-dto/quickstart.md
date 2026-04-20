# Quickstart: UserUpdatedInfo DTO for User Update

**Feature**: 002-user-update-dto
**Date**: 2026-04-17

Guia de validação local da feature. Docker local é proibido pela constituição; portanto a
validação é feita via `dotnet test` + chamada manual ao endpoint via Bruno (coleção existente
em `bruno-collection/User/`).

---

## Pré-requisitos

- Branch `002-user-update-dto` ativa.
- `dotnet build` e `dotnet test` verdes na baseline do `main` (mesmas 11 falhas pré-existentes
  documentadas na feature anterior — servem como referência de não-regressão).
- API rodando localmente via `dotnet run --project NAuth.API` apontando para um banco PostgreSQL
  do tenant de desenvolvimento.

---

## Passo 1 — Verificar o bug no estado inicial (opcional)

Com o código de `main`, chamar:

```http
POST http://localhost:5004/User/update
Authorization: Bearer {jwt-valido}
Content-Type: application/json

{
  "userId": 1,
  "name": "Novo Nome",
  "email": "user@example.com"
}
```

**Resultado esperado (antes do fix)**: HTTP 400 com mensagens de erro exigindo `PixKey`,
`Password`, `IdDocument`.

---

## Passo 2 — Aplicar a feature

Implementar conforme `tasks.md`. Todas as alterações de código estão em:

- `NAuth/DTO/User/UserUpdatedInfo.cs` (novo)
- `NAuth/DTO/User/UserInfo.cs` (remove `Password`)
- `NAuth.Domain/Services/UserService.cs` (Update + overloads privados)
- `NAuth.Domain/Services/Interfaces/IUserService.cs`
- `NAuth.API/Controllers/UserController.cs`
- `NAuth/ACL/UserClient.cs` + `NAuth/ACL/Interfaces/IUserClient.cs`
- Testes e docs correspondentes.

---

## Passo 3 — Build e testes

```bash
dotnet build
dotnet test
```

**Critério de aceite**:
- `dotnet build` verde, sem novos erros.
- `dotnet test` com a mesma contagem de pass/fail pré-existente, OU menos falhas (ideal: as
  mesmas 11 falhas históricas). Nenhuma falha nova em `UserServiceInsertUpdateTests`,
  `UserServiceTests` ou `UserClientTests`.

---

## Passo 4 — Teste manual do fix via Bruno

Coleção existente: `bruno-collection/User/`. Criar (ou adaptar) uma request "Update User" com
payload mínimo:

```http
POST http://localhost:5004/User/update
Authorization: Bearer {jwt-valido-do-usuario-1}
Content-Type: application/json

{
  "userId": 1,
  "name": "Nome Atualizado",
  "email": "user1@example.com"
}
```

**Critério de aceite (SC-001)**: HTTP 200 com o perfil atualizado no body de resposta.

---

## Passo 5 — Teste manual de preservação de `pixKey` / `idDocument`

### 5.1 — Usuário com `pixKey` previamente preenchida

1. Antes: confirmar no banco que `users.pix_key` do `UserId=1` é `'user1@pix.com'`.
2. Chamar update SEM enviar `pixKey`:

   ```json
   {
     "userId": 1,
     "name": "Outro Nome",
     "email": "user1@example.com"
   }
   ```

3. Depois: confirmar que `users.pix_key` permanece `'user1@pix.com'`.

**Critério de aceite (FR-002, SC-005)**.

### 5.2 — Atualização explícita de `pixKey`

1. Chamar update enviando novo valor:

   ```json
   {
     "userId": 1,
     "name": "Outro Nome",
     "email": "user1@example.com",
     "pixKey": "new@pix.com"
   }
   ```

2. Confirmar `users.pix_key = 'new@pix.com'`.

**Critério de aceite (FR-008)**.

---

## Passo 6 — Teste de não-regressão do fluxo de senha

### 6.1 — Password enviado em update é ignorado

1. Confirmar hash atual de senha no banco (campo `users.password` ou equivalente).
2. Chamar update com `"password": "trying-to-change"` (campo extra):

   ```json
   {
     "userId": 1,
     "name": "X",
     "email": "user1@example.com",
     "password": "trying-to-change"
   }
   ```

3. Confirmar que o hash de senha no banco **permanece o mesmo**.

**Critério de aceite (FR-004, SC-003)**.

### 6.2 — ChangePassword continua funcionando

Executar a chamada existente `POST /User/changePassword` com `oldPassword` e `newPassword`
válidos. Confirmar que a senha é atualizada normalmente.

**Critério de aceite (FR-005, SC-004)**.

---

## Passo 7 — Verificar resposta JSON sem `password`

Inspecionar o body de qualquer resposta que retorne `UserInfo` (ex.: resposta do próprio
`/User/update` ou `GET /User/by-slug/...`).

**Critério de aceite (FR-006, SC-005)**: chave `password` não aparece no JSON.

---

## Passo 8 — Revisão de impacto no pacote NuGet

Checar que os consumidores internos da biblioteca (`NAuth.Test/ACL/UserClientTests.cs`) foram
atualizados para usar `UserUpdatedInfo` nas chamadas de `UpdateAsync`. O bump de versão é
controlado pelo prefixo do commit — usar `feature:` (minor) ou `breaking:` (major) conforme
CLAUDE.md, dado que é breaking change público.

---

## Troubleshooting

| Sintoma | Causa provável | Ação |
|---|---|---|
| `dotnet build` falha com erro `UserInfo does not contain 'Password'` | Há referência a `UserInfo.Password` em código legado | Buscar e remover/substituir a referência (geralmente em teste ou mapeamento) |
| Update ainda retorna 400 com "The X field is required" | DTO `UserUpdatedInfo` tem alguma string não-nullable | Garantir `string?` em `Slug`, `ImageUrl`, `IdDocument`, `PixKey` |
| Testes de `UpdateAsync` falham com cast | Mock instancia `UserInfo` onde agora é esperado `UserUpdatedInfo` | Atualizar `new UserInfo { ... }` para `new UserUpdatedInfo { ... }` |
| Resposta do update continua trazendo `password: null` no JSON | `UserInfo.Password` não foi removido | Remover a propriedade inteira, não apenas o valor |

---

## Checklist final

- [ ] Novo DTO `UserUpdatedInfo` criado em `NAuth/DTO/User/`.
- [ ] `UserInfo.Password` removido.
- [ ] Cadeia Controller → Service → Client ACL propagou a nova assinatura.
- [ ] `dotnet build` verde.
- [ ] `dotnet test` sem novas falhas.
- [ ] Smoke test do Passo 4 retorna HTTP 200.
- [ ] Smoke test do Passo 5 confirma preservação de `pixKey`.
- [ ] Smoke test do Passo 6 confirma que senha não é alterada por update.
- [ ] Resposta JSON não contém chave `password`.
