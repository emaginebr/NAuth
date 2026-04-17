# Phase 0 Research: Fortuno Tenant Onboarding

**Feature**: 001-fortuno-tenant
**Date**: 2026-04-17

Nenhum `NEEDS CLARIFICATION` foi levantado no Technical Context — o padrão de adição de tenant está
totalmente determinado pelas iterações anteriores (bazzuca, monexup). Esta seção registra as
decisões, as referências usadas e as alternativas avaliadas.

---

## Decision 1 — Replicar o padrão monexup (3 arquivos)

**Decisão**: A adição do tenant "fortuno" mexe exclusivamente em três arquivos:

1. `.env.prod.example` — adicionar bloco comentado `# Tenant: fortuno` com `FORTUNO_CONNECTION_STRING` e `FORTUNO_JWT_SECRET`.
2. `NAuth.API/appsettings.Production.json` — adicionar objeto `"fortuno"` sob `Tenants` com `ConnectionString`, `JwtSecret` e `BucketName: "Fortuno"`.
3. `docker-compose-prod.yml` — adicionar mapeamentos `Tenants__fortuno__ConnectionString` e `Tenants__fortuno__JwtSecret` apontando para as env vars acima.

**Rationale**: Esse é exatamente o diff do commit `f805fe7` (fix: add tenant monexup), já revisado
e mergeado em produção. Reusar o padrão garante SC-002 (0 regressões) e SC-004 (provisionamento em
até 15 min) sem surpresas de integração.

**Alternativas consideradas**:

- *Adicionar o tenant em `appsettings.json` (Development)* — Rejeitado. O padrão monexup/bazzuca
  não versiona tenants de produção no appsettings de desenvolvimento. O dev ambiente usa
  `default-tenant` + `devblog`; introduzir "fortuno" em dev não traz valor e cria ruído.
- *Estender `appsettings.Docker.json`* — Rejeitado. Esse arquivo mantém apenas o `default-tenant`
  parametrizável por env var; não lista tenants de produção. Padrão estabelecido.
- *Criar nova migração ou seed para "fortuno"* — Rejeitado. O esquema é idêntico entre tenants
  (decisão arquitetural documentada em `docs/MULTI_TENANT_API.md`). Provisionamento do DB cabe à
  infraestrutura, fora do escopo desta feature (Assumption do spec).

---

## Decision 2 — Nome canônico e bucket

**Decisão**: Identificador de tenant em minúsculas `"fortuno"`; `BucketName` em PascalCase
`"Fortuno"`.

**Rationale**: Todos os tenants existentes seguem essa convenção (`"emagine"` → `"Emagine"`,
`"bazzuca"` → `"Bazzuca"`, `"monexup"` → `"Monexup"`). `TenantResolver` casa a chave exatamente como
declarada em `Tenants:{id}`. `BucketName` é usado por `zTools` para prefixar uploads S3.

**Alternativas consideradas**:

- *ID em caixa mista ou com hífen* — Rejeitado. Quebra consistência e exigiria normalização no
  `TenantMiddleware`, hoje case-sensitive.

---

## Decision 3 — Estratégia de validação sem Docker local

**Decisão**: Validação pós-deploy em ambiente de produção (ou staging idêntico à produção), usando
coleções Bruno (`bruno-collection/`) com header `X-Tenant-Id: fortuno`.

**Rationale**: Princípio II da constituição proíbe `docker compose` local. O projeto já adota
testes manuais via Bruno para cenários multi-tenant (ex.: `bruno-collection/User/Login With
Email.bru` referencia identificadores de tenant). Testes unitários existentes em
`NAuth.Test/Tenant/TenantTests.cs` cobrem o resolver de tenant de forma agnóstica ao nome; eles
continuam verdes sem modificações.

**Alternativas consideradas**:

- *Adicionar caso de teste xUnit específico para "fortuno"* — Rejeitado. Os testes existentes
  testam o contrato do `TenantResolver`/`TenantMiddleware` genericamente; duplicar o caso apenas
  para "fortuno" fere a regra de não adicionar cobertura redundante (CLAUDE.md: "a bug fix doesn't
  need surrounding cleanup").
- *Provisionar container PostgreSQL local* — Rejeitado. Fere Princípio II (Docker local proibido).

---

## Decision 4 — Comprimento mínimo do segredo JWT

**Decisão**: Segredo JWT do tenant "fortuno" deve ter no mínimo 64 caracteres, alinhado ao padrão
documentado em `CLAUDE.md` ("Secret must be minimum 64 characters").

**Rationale**: A plataforma emite tokens com HMAC-SHA256. Abaixo de 64 caracteres, a entropia cai
para níveis inseguros e o `IssuerSigningKeyResolver` pode rejeitar a chave. O placeholder em
`.env.prod.example` já usa a frase `at_least_64_characters_long`.

**Alternativas consideradas**:

- *Permitir segredo ≥ 32 chars* — Rejeitado. Conflita com regra interna e enfraquece segurança
  sem ganho operacional.

---

## Referências

- `docs/MULTI_TENANT_API.md` — design multi-tenant da plataforma.
- Commit `f805fe7` (fix: add tenant monexup) — diff de referência.
- Commit `2f13be1` (fix: add tenant bazzuca) — diff de referência anterior.
- `.claude/skills/dotnet-multi-tenant/SKILL.md` — padrão técnico completo da skill.
- `CLAUDE.md` — instruções do projeto (ver seção JWT).

## Saídas

Todas as decisões técnicas foram resolvidas. `research.md` não possui `NEEDS CLARIFICATION`
pendentes. Pronto para Phase 1.
