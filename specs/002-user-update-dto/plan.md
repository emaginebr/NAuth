# Implementation Plan: UserUpdatedInfo DTO for User Update

**Branch**: `002-user-update-dto` | **Date**: 2026-04-17 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-user-update-dto/spec.md`

## Summary

Corrigir o bug de validação automática em `POST /User/update` (HTTP 400 para `PixKey`, `Password`,
`IdDocument` quando esses campos vêm nulos) introduzindo um DTO dedicado `UserUpdatedInfo` com os
três campos opcionais (nullable). Remover o campo `Password` do `UserInfo` (contrato de leitura),
pois a senha só é alterada via `changePassword`/`changePasswordUsingHash`. Propagar a nova
assinatura por toda a cadeia (Controller → Service → Client ACL) mantendo retrocompatibilidade
comportamental em todos os outros fluxos (login, criação, leitura, troca de senha).

## Technical Context

**Language/Version**: .NET 8.0
**Primary Dependencies**: ASP.NET Core MVC, EF Core 9 (Npgsql), Newtonsoft.Json (no ACL), xUnit +
Moq nos testes. Validação implícita por nullable reference types (sem FluentValidation).
**Storage**: PostgreSQL — nenhuma migração/alteração de esquema é necessária (apenas muda o
contrato de entrada do método `Update`; os campos persistidos permanecem os mesmos).
**Testing**: xUnit + Moq. Arquivos afetados: `NAuth.Test/Domain/Services/UserServiceInsertUpdateTests.cs`,
`NAuth.Test/ACL/UserClientTests.cs`, `NAuth.Test/Domain/Services/UserServiceTests.cs`.
**Target Platform**: Linux container (Docker) em produção; Kestrel HTTPS. Biblioteca `NAuth`
também publicada como pacote NuGet.
**Project Type**: Web service (REST API) + pacote NuGet (DTOs + ACL client).
**Performance Goals**: Sem impacto de performance — mudança é puramente contratual/DTO.
**Constraints**:
- Breaking change na API pública do pacote `NAuth` (assinatura de `IUserClient.UpdateAsync`).
- `UserInfo` precisa perder o campo `Password` sem quebrar JSON de resposta existente (o campo
  nunca era preenchido em respostas, portanto nenhum consumidor depende dele).
- Nenhum outro método da pilha (login, insert, changePassword, etc.) pode mudar comportamento.
- A senha jamais pode ser alterada pelo caminho de update.
**Scale/Scope**: Afeta 1 endpoint (`POST /User/update`) + 1 método do client ACL + 1 DTO novo +
1 DTO atualizado. Alterações concentradas em ~7 arquivos de código + 3 arquivos de teste +
documentação.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aderência | Nota |
|---|---|---|
| I. Skills Obrigatórias | ✅ | Feature altera DTO e serviço existentes — cabe a skill `dotnet-architecture`. `dotnet-fluent-validation` **não** se aplica (o projeto hoje não usa FluentValidation; introduzi-lo só para este fix seria fora de escopo). |
| II. Stack Tecnológica Fixa | ✅ | Zero mudança de stack; continua .NET 8 + EF Core 9 + PostgreSQL. |
| III. Convenções de Código .NET | ✅ | Novo DTO segue PascalCase + `[JsonPropertyName("camelCase")]`; namespace file-scoped; nenhuma mudança nas convenções existentes. |
| IV. Convenções de Banco PostgreSQL | ✅ | Nenhuma alteração de esquema ou migração. |
| V. Autenticação e Segurança | ✅ | Remover `Password` do `UserInfo` e do update REDUZ a superfície de ataque. Endpoint continua `[Authorize]`; nenhum secret exposto. |

**Resultado**: PASS — nenhum gate violado. Sem entradas em Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/002-user-update-dto/
├── plan.md              # Este arquivo
├── research.md          # Phase 0 — decisões técnicas e alternativas
├── data-model.md        # Phase 1 — DTOs UserUpdatedInfo + UserInfo (modificado)
├── quickstart.md        # Phase 1 — como validar o fix localmente
├── contracts/
│   └── user-update.md   # Phase 1 — contrato do endpoint POST /User/update
├── checklists/
│   └── requirements.md  # Gerado por /speckit.specify
└── tasks.md             # Phase 2 — gerado por /speckit.tasks
```

### Source Code (repository root — arquivos afetados)

```text
NAuth/
├── NAuth/DTO/User/
│   ├── UserInfo.cs                                            # MODIFICADO (remove Password)
│   └── UserUpdatedInfo.cs                                     # NOVO
├── NAuth/ACL/
│   ├── Interfaces/IUserClient.cs                              # MODIFICADO (UpdateAsync recebe UserUpdatedInfo)
│   └── UserClient.cs                                          # MODIFICADO (implementação)
├── NAuth.Domain/Services/
│   ├── UserService.cs                                         # MODIFICADO (Update, ValidateUserForUpdate, overloads Phones/Addresses/Roles)
│   └── Interfaces/IUserService.cs                             # MODIFICADO (assinatura Update)
├── NAuth.API/Controllers/
│   └── UserController.cs                                      # MODIFICADO (Update recebe UserUpdatedInfo)
├── NAuth.Test/
│   ├── Domain/Services/UserServiceInsertUpdateTests.cs        # MODIFICADO (testes de Update usam UserUpdatedInfo)
│   ├── Domain/Services/UserServiceTests.cs                    # MODIFICADO (se tocar Update)
│   └── ACL/UserClientTests.cs                                 # MODIFICADO (testes de UpdateAsync)
├── docs/USER_API_DOCUMENTATION.md                             # MODIFICADO (contrato do update)
└── NAuth/README.md                                            # MODIFICADO (interface IUserClient)
```

**Structure Decision**: Clean Architecture já estabelecida, seguindo Princípio I. O novo DTO
`UserUpdatedInfo` nasce em `NAuth/DTO/User/` (mesma pasta do `UserInfo` e `UserInsertedInfo`) —
isso preserva o padrão "Info/InsertedInfo/UpdatedInfo" citado na skill `dotnet-architecture`. A
propagação da nova assinatura é linear por uma única cadeia (Controller → Service → Client ACL),
sem ramificação, o que minimiza risco de regressão.

Os overloads `InsertPhones(UserInfo)`, `InsertAddresses(UserInfo)`, `InsertRoles(UserInfo)`,
`ValidatePhones(UserInfo)`, `ValidateAddresses(UserInfo)`, `ValidateRoles(UserInfo)` (hoje usados
pelo Update) serão atualizados para aceitar `UserUpdatedInfo` — a semântica de negócio não muda,
muda apenas o tipo do parâmetro.

## Complexity Tracking

> Nenhuma violação de constituição. Seção intencionalmente vazia.
