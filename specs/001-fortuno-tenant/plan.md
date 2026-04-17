# Implementation Plan: Fortuno Tenant Onboarding

**Branch**: `001-fortuno-tenant` | **Date**: 2026-04-17 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-fortuno-tenant/spec.md`

## Summary

Registrar o tenant "fortuno" em paridade com os demais (emagine, viralt, devblog, bazzuca, monexup),
aproveitando toda a infraestrutura multi-tenant já implementada (`TenantMiddleware`,
`TenantResolver`, `TenantDbContextFactory`, `TenantHeaderHandler`, JWT dinâmico). A mudança é
estritamente configuracional: adicionar uma entrada em três arquivos de configuração de produção,
replicando fielmente o padrão introduzido na adição do tenant monexup (commit f805fe7) — sem
alterar código de runtime, modelos, migrações ou DI.

## Technical Context

**Language/Version**: .NET 8.0
**Primary Dependencies**: ASP.NET Core, EF Core 9 (Npgsql), Swashbuckle 8, JwtBearer
**Storage**: PostgreSQL (banco exclusivo `fortuno_db`, provisionado por infraestrutura)
**Testing**: xUnit + Moq (projeto `NAuth.Test`), testes existentes em `NAuth.Test/Tenant/TenantTests.cs`
**Target Platform**: Linux container (Docker) em produção; Kestrel HTTPS
**Project Type**: Web service (REST API multi-tenant)
**Performance Goals**: manter SLAs existentes — adicionar um tenant não pode degradar os demais
(SC-002: 0 regressões)
**Constraints**:
- Segredo JWT MUST ter no mínimo 64 caracteres (regra já aplicada na plataforma).
- Credenciais NUNCA podem ser versionadas (Princípio V da constituição + SC-003).
- Mudanças de runtime/código estão fora do escopo — apenas configuração.
- Docker não é executável localmente (Princípio II); validação em produção ocorre pós-deploy.
**Scale/Scope**: +1 tenant sobre os 5 existentes; nenhuma mudança em esquema de DB ou endpoints.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aderência | Nota |
|---|---|---|
| I. Skills Obrigatórias | ✅ | Feature referencia `dotnet-multi-tenant` como fonte do padrão; não há código novo de entidade — skill `dotnet-architecture` não se aplica. |
| II. Stack Tecnológica Fixa | ✅ | Zero mudança de stack; apenas config PostgreSQL + JWT secret. Nenhum comando Docker será executado localmente. |
| III. Convenções de Código .NET | ✅ | Nenhum código .NET novo é produzido. |
| IV. Convenções de Banco PostgreSQL | ✅ | Banco `fortuno_db` segue naming snake_case; esquema é idêntico ao dos demais tenants (sem migração diferencial). |
| V. Autenticação e Segurança | ✅ | Segredo JWT ≥ 64 chars; credenciais vivem em env vars (`FORTUNO_CONNECTION_STRING`, `FORTUNO_JWT_SECRET`); nenhum valor real é versionado. |

**Resultado**: PASS — nenhum gate violado. Sem entradas em Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/001-fortuno-tenant/
├── plan.md              # Este arquivo
├── research.md          # Phase 0 — decisões técnicas e alternativas
├── data-model.md        # Phase 1 — entidade "tenant fortuno" (config-only)
├── quickstart.md        # Phase 1 — passos de provisionamento/validação
├── contracts/
│   └── tenant-config.md # Phase 1 — contrato das chaves de configuração
├── checklists/
│   └── requirements.md  # Gerado por /speckit.specify
└── tasks.md             # Phase 2 — gerado por /speckit.tasks
```

### Source Code (repository root — arquivos afetados)

```text
NAuth/
├── .env.prod.example                      # +3 linhas (comentário + 2 vars)
├── docker-compose-prod.yml                # +4 linhas (2 comentários + 2 mapeamentos)
└── NAuth.API/
    └── appsettings.Production.json        # +5 linhas (bloco "fortuno" sob Tenants)
```

**Structure Decision**: Web service multi-tenant com Clean Architecture já estabelecida (API →
Application → Domain ← Infra + `NAuth` unificando DTO/ACL). Esta feature NÃO cria arquivos
novos de código — opera apenas nos três arquivos de configuração de produção. A infraestrutura
multi-tenant (`NAuth.API/Middlewares/TenantMiddleware.cs`, `NAuth.API/Services/TenantResolver.cs`,
`NAuth.API/Services/TenantDbContextFactory.cs`, `NAuth.API/Handlers/MultiTenantHandler.cs`,
`NAuth.Application/Initializer.cs`) já está presente desde o commit `4e8c73e` (feat: add multi
tenant) e é reutilizada integralmente.

## Complexity Tracking

> Nenhuma violação de constituição. Seção intencionalmente vazia.
