# Quickstart: Fortuno Tenant Onboarding

**Feature**: 001-fortuno-tenant
**Date**: 2026-04-17

Guia passo-a-passo para um operador provisionar o tenant "fortuno" em produção. O objetivo é
cumprir SC-004 da especificação: completar o processo em ≤15 min.

---

## Pré-requisitos

- Acesso de escrita aos seguintes arquivos deste repositório:
  - `.env.prod.example`
  - `NAuth.API/appsettings.Production.json`
  - `docker-compose-prod.yml`
- Acesso ao arquivo `.env.prod` do ambiente de produção (fora do repositório).
- Credenciais da Fortuno fornecidas pela equipe de infraestrutura:
  - **Connection string PostgreSQL** apontando para `fortuno_db` exclusivo.
  - **Segredo JWT** com ≥ 64 caracteres.
- Banco `fortuno_db` já provisionado com o esquema idêntico ao dos tenants existentes (via
  `createContext.ps1` ou restauração de dump).

---

## Passo 1 — Atualizar `.env.prod.example` (arquivo versionado, só placeholders)

Adicione ao final do arquivo, após o bloco `monexup`:

```dotenv

# Tenant: fortuno
FORTUNO_CONNECTION_STRING=Host=your_db_host;Port=5432;Database=fortuno_db;Username=your_user;Password=your_password
FORTUNO_JWT_SECRET=your_fortuno_jwt_secret_at_least_64_characters_long
```

> ⚠️ Placeholders apenas. Nunca commite valores reais.

---

## Passo 2 — Atualizar `NAuth.API/appsettings.Production.json`

Adicione o objeto `"fortuno"` ao final da seção `Tenants`, após `"monexup"`:

```json
"monexup": {
  "ConnectionString": "",
  "JwtSecret": "",
  "BucketName": "Monexup"
},
"fortuno": {
  "ConnectionString": "",
  "JwtSecret": "",
  "BucketName": "Fortuno"
}
```

> Note a vírgula no final do bloco `monexup` pré-existente.

---

## Passo 3 — Atualizar `docker-compose-prod.yml`

No serviço `nauth-api`, seção `environment`, adicione após o bloco `monexup`:

```yaml
# Maps to appsettings: Tenants.fortuno.ConnectionString
Tenants__fortuno__ConnectionString: ${FORTUNO_CONNECTION_STRING}
# Maps to appsettings: Tenants.fortuno.JwtSecret
Tenants__fortuno__JwtSecret: ${FORTUNO_JWT_SECRET}
```

---

## Passo 4 — Validar localmente (sem Docker)

```bash
dotnet build
dotnet test
```

Ambos devem continuar verdes — esta feature é puramente configuracional.

---

## Passo 5 — Provisionar `.env.prod` no servidor de produção

No servidor de produção, adicione ao `.env.prod` (não versionado) as duas variáveis com os valores
reais fornecidos pela infraestrutura:

```dotenv
FORTUNO_CONNECTION_STRING=Host=<host-real>;Port=5432;Database=fortuno_db;Username=<user-real>;Password=<senha-real>
FORTUNO_JWT_SECRET=<segredo-com-pelo-menos-64-caracteres>
```

---

## Passo 6 — Deploy e smoke test

Realize o deploy pelo pipeline de produção padrão. Após o container subir, valide os três
cenários de aceitação do spec com a coleção Bruno (`bruno-collection/`):

### 6.1 Login como tenant fortuno

```http
POST https://<host-prod>/api/user/login
X-Tenant-Id: fortuno
Content-Type: application/json

{ "email": "<usuario-fortuno-real>", "password": "<senha>" }
```

**Esperado**: HTTP 200 com token JWT. Decodifique o token e confirme `tenant_id: "fortuno"`.

### 6.2 Validação de token autenticado

Use o token retornado em uma rota protegida:

```http
GET https://<host-prod>/api/user/me
Authorization: Basic <token>
```

**Esperado**: HTTP 200 com dados do usuário da base Fortuno.

### 6.3 Não-regressão dos tenants existentes

Repita o login para pelo menos um tenant pré-existente (ex.: `X-Tenant-Id: emagine`) e confirme
que responde normalmente.

### 6.4 Rejeição cross-tenant

Envie o token obtido em 6.1 em uma requisição com `X-Tenant-Id: emagine`. **Esperado**: HTTP 401
— a validação do JWT deve falhar porque o segredo da Fortuno não assinou o token no contexto da
emagine.

---

## Troubleshooting

| Sintoma | Causa provável | Ação |
|---|---|---|
| Startup da API falha com erro de segredo ausente | `FORTUNO_JWT_SECRET` não exportado no `.env.prod` | Conferir env var no servidor |
| Login retorna 500 com "connection refused" | `fortuno_db` inacessível ou credenciais erradas | Conferir `FORTUNO_CONNECTION_STRING` |
| Login retorna 401 mesmo com senha correta | Segredo JWT com menos de 64 caracteres | Ajustar para ≥ 64 chars e redeploy |
| Tenant pré-existente quebra após deploy | Ordem do JSON corrompida ou vírgula faltando | Reverter commit; validar `jq .` no JSON |

---

## Checklist final

- [ ] Os 3 arquivos versionados foram atualizados com exatamente as entradas descritas.
- [ ] `dotnet build` e `dotnet test` passam localmente.
- [ ] `.env.prod` em produção contém valores reais das duas variáveis Fortuno.
- [ ] Smoke test 6.1 a 6.4 executados com sucesso.
- [ ] Nenhum valor real de secret/connection string foi versionado.
