# Phase 1 Data Model: Fortuno Tenant Onboarding

**Feature**: 001-fortuno-tenant
**Date**: 2026-04-17

Esta feature **não introduz novas entidades de domínio nem altera o esquema de banco**. O único
"modelo de dados" relevante é o registro de configuração do tenant "fortuno" na estrutura
`Tenants` já consumida por `ITenantResolver`/`TenantResolver`.

Para completude e rastreabilidade, segue a especificação desse registro.

---

## Entidade de configuração: Tenant

**Onde reside**: `NAuth.API/appsettings.Production.json` (seção `Tenants.{tenantId}`) e,
indiretamente, em variáveis de ambiente injetadas via `docker-compose-prod.yml`.

**Lida por**: `NAuth.API/Services/TenantResolver.cs` via `IConfiguration`.

### Campos

| Campo | Tipo | Origem | Regra de validação | Observação |
|---|---|---|---|---|
| `TenantId` (chave JSON) | string | Literal em `appsettings.Production.json` | Identificador minúsculo, sem espaços. Único na seção `Tenants`. | Para esta feature: `"fortuno"`. |
| `ConnectionString` | string | Env var `FORTUNO_CONNECTION_STRING` (mapeada em `docker-compose-prod.yml`) | Não vazia em produção. Formato Npgsql (`Host=...;Port=...;Database=...;Username=...;Password=...`). | Nunca versionado com valor real. |
| `JwtSecret` | string | Env var `FORTUNO_JWT_SECRET` (mapeada em `docker-compose-prod.yml`) | Comprimento mínimo de **64 caracteres**. | HMAC-SHA256; nunca versionado. |
| `BucketName` | string | Literal em `appsettings.Production.json` | PascalCase, único entre tenants. | Para esta feature: `"Fortuno"`. |

### Relacionamentos

- `Tenant "fortuno"` **1:N** `Users` do banco `fortuno_db` (esquema idêntico ao dos demais
  tenants; ver `NAuth.Infra.NAuthContext`).
- `Tenant "fortuno"` **1:1** `JwtSecret` — usado por `IssuerSigningKeyResolver` no pipeline de
  autenticação.
- `Tenant "fortuno"` **1:1** `BucketName` — usado por `zTools` para prefixar uploads S3.

### Estados e transições

Não aplicável. Um tenant em `appsettings.Production.json` é uma configuração estática lida uma vez
no startup. Não há ciclo de vida dinâmico — atualização exige redeploy.

### Regras derivadas dos requisitos funcionais

- **FR-001**: `"fortuno"` presente como chave em `Tenants` → `TenantResolver` retorna
  `ConnectionString`/`JwtSecret` correspondentes quando `DefaultTenantId` resolver para
  `"fortuno"` ou quando requisição trouxer `X-Tenant-Id: fortuno`.
- **FR-002**: `ConnectionString` aponta para `fortuno_db` exclusivo → isolamento de dados.
- **FR-003**: `JwtSecret` único → assinatura/validação exclusivas.
- **FR-004**: `ConnectionString` e `JwtSecret` lidos apenas de env vars → nenhum literal em
  arquivos versionados.
- **FR-007**: Adicionar `"fortuno"` sob `Tenants` é aditivo; chaves pré-existentes permanecem
  intactas.
- **FR-008**: `BucketName: "Fortuno"` segue PascalCase do padrão.

## Banco de dados PostgreSQL (provisionamento externo)

**Fora do escopo de código desta feature.** Infraestrutura provisiona `fortuno_db` seguindo o
esquema idêntico dos tenants existentes (via dump/restauração ou execução do script
`createContext.ps1`/migrations). As convenções de banco (Princípio IV da constituição) seguem
automaticamente porque o esquema é produzido pelo mesmo `NAuthContext`.
