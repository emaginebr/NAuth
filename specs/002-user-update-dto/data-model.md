# Phase 1 Data Model: UserUpdatedInfo DTO for User Update

**Feature**: 002-user-update-dto
**Date**: 2026-04-17

Esta feature não altera o esquema persistido no PostgreSQL. O modelo de dados aqui se refere aos
**DTOs** trocados entre camadas/consumidores e ao modelo de domínio `IUserModel`/`UserModel` —
que permanece inalterado.

---

## DTO novo: `UserUpdatedInfo`

**Localização**: `NAuth/DTO/User/UserUpdatedInfo.cs`

**Finalidade**: Contrato de entrada do método `Update` (controller → service → client ACL).

### Campos

| Campo | Tipo (C#) | JsonPropertyName | Obrigatoriedade | Observação |
|---|---|---|---|---|
| `UserId` | `long` | `userId` | Obrigatório | `> 0`. Identificador do usuário a atualizar. |
| `Slug` | `string?` | `slug` | Opcional | Nullable; slug é regerado pelo service se nulo/vazio. |
| `ImageUrl` | `string?` | `imageUrl` | Opcional | Nullable. |
| `Name` | `string` | `name` | Obrigatório | Validado pelo service (`ValidateUserForUpdate`). |
| `Email` | `string` | `email` | Obrigatório | Validado (formato + unicidade). |
| `IsAdmin` | `bool` | `isAdmin` | Default `false` | Só respeitado se o requester for admin (regra existente). |
| `BirthDate` | `DateTime?` | `birthDate` | Opcional | Conversor nullable já existente. |
| `IdDocument` | `string?` | `idDocument` | **Opcional (fix do bug)** | Se presente, validado como CPF/CNPJ. Se nulo/vazio, preservado. |
| `PixKey` | `string?` | `pixKey` | **Opcional (fix do bug)** | Se nulo/vazio, preservado. |
| `Status` | `int` | `status` | Default `0` | Mapeado para enum `UserStatus`. |
| `Roles` | `IList<RoleInfo>` | `roles` | Opcional | Sobrescreve associações de papéis. |
| `Phones` | `IList<UserPhoneInfo>` | `phones` | Opcional | Sobrescreve telefones. |
| `Addresses` | `IList<UserAddressInfo>` | `addresses` | Opcional | Sobrescreve endereços. |

### Regras de validação (aplicadas no `UserService`, não no DTO)

- `UserId > 0`, caso contrário `UserValidationException("User not found")`.
- `Name` não vazio (`UserValidationException("Name is empty")`).
- `Email` não vazio, formato válido, único por usuário.
- `IdDocument`, se fornecido e não-vazio, deve ser CPF/CNPJ válido.
- `Phones`, `Addresses`, `Roles` seguem validações existentes (`ValidatePhones`,
  `ValidateAddresses`, `ValidateRoles`).

### Diferenças-chave vs `UserInsertedInfo`

- Possui `UserId`, `Status` (que `UserInsertedInfo` não tem).
- Não possui `Password` (por design — alteração de senha é fluxo separado).
- `Slug`, `ImageUrl`, `IdDocument`, `PixKey` são nullable.

---

## DTO modificado: `UserInfo`

**Localização**: `NAuth/DTO/User/UserInfo.cs`

**Finalidade**: Contrato de **leitura** — retornado em respostas de API (login, get by slug,
etc.) e usado internamente em `GetUserInfoFromModel`.

### Mudança

| Campo | Antes | Depois |
|---|---|---|
| `Password` | `public string Password { get; set; }` com `[JsonPropertyName("password")]` | **Removido** |

**Os demais campos permanecem inalterados** (UserId, Slug, ImageUrl, Name, Email, Hash, IsAdmin,
BirthDate, IdDocument, PixKey, Status, Roles, Phones, Addresses, CreatedAt, UpdatedAt).

### Justificativa

- `Password` nunca foi preenchido em respostas (`GetUserInfoFromModel` nunca atribuía o campo).
- Nenhum consumidor da biblioteca lê `UserInfo.Password` após deserialização.
- Remover reduz a superfície de ataque e alinha-se ao Princípio V da constituição (segurança).

---

## DTO intocado: `UserInsertedInfo`

**Localização**: `NAuth/DTO/User/UserInsertedInfo.cs`

**Finalidade**: Contrato do método `Insert`. Mantém `Password`, `PixKey`, `IdDocument` como
obrigatórios (criação requer esses dados).

**Sem mudança.** Incluído aqui apenas para deixar explícito que o fluxo de criação não é
afetado por esta feature.

---

## Interfaces e serviços afetados (nível tipo, não persistência)

| Elemento | Antes | Depois |
|---|---|---|
| `IUserService.Update(UserInfo)` | `Task<IUserModel>` | `Task<IUserModel> Update(UserUpdatedInfo)` |
| `UserService.Update(UserInfo)` | idem | idem |
| `UserService.ValidateUserForUpdate(UserInfo, IUserModel)` | assinatura atual | `ValidateUserForUpdate(UserUpdatedInfo, IUserModel)` |
| `UserService.InsertPhones(UserInfo)` | overload privado | `InsertPhones(UserUpdatedInfo)` |
| `UserService.InsertAddresses(UserInfo)` | idem | `InsertAddresses(UserUpdatedInfo)` |
| `UserService.InsertRoles(UserInfo)` | idem | `InsertRoles(UserUpdatedInfo)` |
| `UserService.ValidatePhones(UserInfo)` | idem | `ValidatePhones(UserUpdatedInfo)` |
| `UserService.ValidateAddresses(UserInfo)` | idem | `ValidateAddresses(UserUpdatedInfo)` |
| `UserService.ValidateRoles(UserInfo)` | idem | `ValidateRoles(UserUpdatedInfo)` |
| `UserController.Update(UserInfo)` | `[FromBody] UserInfo user` | `[FromBody] UserUpdatedInfo user` |
| `IUserClient.UpdateAsync(UserInfo, string)` | `Task<UserInfo?>` | `Task<UserInfo?> UpdateAsync(UserUpdatedInfo, string)` |
| `UserClient.UpdateAsync(UserInfo, string)` | idem | idem |

**Nota**: o retorno de `Update`/`UpdateAsync` continua sendo `UserInfo` (agora sem `Password`) —
o chamador recebe o perfil atualizado em formato de leitura.

---

## Persistência (sem mudança)

Tabela `users` e colunas relacionadas permanecem intactas. Nenhuma migração EF é necessária. O
comportamento do `UserService.Update` ao atribuir campos do DTO ao modelo (linhas 712–718 de
`UserService.cs`) passa a receber valores potencialmente `null` em `IdDocument`/`PixKey` — o
método já trata esses casos de forma defensiva antes desta feature.
