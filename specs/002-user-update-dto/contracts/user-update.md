# Contract: POST /User/update

**Feature**: 002-user-update-dto
**Date**: 2026-04-17

Contrato do endpoint de atualização de perfil do usuário após a feature. Ele **substitui** o
contrato anterior (que usava `UserInfo` como request body e sofria do bug de validação
automática reportado na spec).

---

## Endpoint

```
POST /User/update
```

### Headers

| Header | Obrigatório | Descrição |
|---|---|---|
| `Authorization` | Sim | `Bearer {jwt}` — token emitido pelo tenant corrente. |
| `X-Tenant-Id` | Depende do fluxo | Obrigatório em requisições não autenticadas; para autenticadas, o tenant é resolvido via claim. |
| `Content-Type` | Sim | `application/json` |

### Autorização

- Requer `[Authorize]`.
- Regra de negócio (preservada): o `UserId` do payload deve ser do próprio usuário em sessão
  OU o usuário em sessão deve ter claim `isAdmin=true`.

---

## Request body (novo contrato — `UserUpdatedInfo`)

```json
{
  "userId": 123,
  "slug": "john-doe",
  "imageUrl": "https://bucket.example.com/avatar.png",
  "name": "John Doe",
  "email": "john@example.com",
  "isAdmin": false,
  "birthDate": "1990-01-15",
  "idDocument": "12345678900",
  "pixKey": "john@example.com",
  "status": 1,
  "roles": [ { "roleId": 1, "slug": "admin", "name": "Admin" } ],
  "phones": [ { "phone": "+5511999999999" } ],
  "addresses": [
    {
      "zipCode": "01310-100",
      "address": "Av. Paulista, 1000",
      "complement": "Apt 101"
    }
  ]
}
```

### Regras de campo

| Campo | Tipo | Obrigatório | Validação |
|---|---|---|---|
| `userId` | integer | **Sim** | `> 0`. Usuário deve existir. |
| `name` | string | **Sim** | Não vazio. |
| `email` | string | **Sim** | Formato válido; único por usuário. |
| `slug` | string\|null | Não | Se nulo/vazio, regenerado pelo service. |
| `imageUrl` | string\|null | Não | Se URL, extrai filename; caso contrário usa valor literal. |
| `isAdmin` | boolean | Default `false` | Só aceita `true` se requester for admin. |
| `birthDate` | ISO date\|null | Não | Conversor nullable. |
| `idDocument` | string\|null | **Não (fix do bug)** | Se preenchido, validado como CPF/CNPJ. |
| `pixKey` | string\|null | **Não (fix do bug)** | Se preenchido, sobrescreve; se nulo/vazio, preservado. |
| `status` | integer | Default `0` | Mapeado para `UserStatus`. |
| `roles` | array de `RoleInfo` | Não | Se presente, sobrescreve associações. |
| `phones` | array de `UserPhoneInfo` | Não | Se presente, sobrescreve telefones. |
| `addresses` | array de `UserAddressInfo` | Não | Se presente, sobrescreve endereços. |

**Importante**: O campo `password` **não é aceito**. Se enviado no JSON, é **ignorado** pelo
ModelBinder (não existe no DTO). A senha só é alterada via `POST /User/changePassword` ou
`POST /User/changePasswordUsingHash`.

---

## Responses

### 200 OK — `UserInfo` (contrato de leitura, agora sem `password`)

```json
{
  "userId": 123,
  "slug": "john-doe",
  "imageUrl": "https://bucket.example.com/avatar.png",
  "name": "John Doe",
  "email": "john@example.com",
  "hash": "...",
  "isAdmin": false,
  "birthDate": "1990-01-15",
  "idDocument": "12345678900",
  "pixKey": "john@example.com",
  "status": 1,
  "roles": [ ... ],
  "phones": [ ... ],
  "addresses": [ ... ],
  "createAt": "2024-01-01T10:00:00",
  "updateAt": "2026-04-17T09:30:00"
}
```

**Mudança vs contrato anterior**: o campo `password` foi removido do JSON de resposta. Como o
valor sempre vinha vazio (`""` ou `null`), nenhum consumidor depende dele.

### 400 Bad Request

Retornado quando:

- Body é `null` ou não é JSON válido.
- `userId <= 0` (mensagem: "User not found").
- `name` vazio (mensagem: "Name is empty").
- `email` vazio, inválido ou já pertencente a outro usuário.
- `idDocument` fornecido mas inválido (CPF/CNPJ).
- `phones`/`addresses`/`roles` violam validações específicas.

**Não retorna mais 400 por ausência de `pixKey`, `password` ou `idDocument` — este é o fix.**

### 401 Unauthorized

Retornado quando o token é ausente/inválido ou o requester tenta atualizar um `userId` que não é
o seu sem ser admin.

### 500 Internal Server Error

Retornado em falha interna do service/transação. Body: mensagem da exceção.

---

## Client ACL (`NAuth.ACL.UserClient`)

### Antes

```csharp
Task<UserInfo?> UpdateAsync(UserInfo user, string token);
```

### Depois (breaking change público)

```csharp
Task<UserInfo?> UpdateAsync(UserUpdatedInfo user, string token);
```

**Migração para consumidores do pacote NuGet**: Substituir `new UserInfo { ... }` por
`new UserUpdatedInfo { ... }` nas chamadas de `UpdateAsync`, e remover quaisquer atribuições a
`.Password` (não existe mais no contrato). O retorno continua sendo `UserInfo`.

---

## Critérios de aceite do contrato

- [ ] Requisição com payload contendo apenas `userId`, `name`, `email` retorna HTTP 200.
- [ ] Requisição SEM `pixKey`, sem `password`, sem `idDocument` não retorna erros de validação
      para esses campos.
- [ ] Requisição com `password` no body é aceita sem efeito sobre a senha persistida.
- [ ] Resposta JSON 200 NÃO contém a chave `password`.
- [ ] Fluxos `POST /User/changePassword` e `POST /User/changePasswordUsingHash` continuam
      operando sem regressão.
