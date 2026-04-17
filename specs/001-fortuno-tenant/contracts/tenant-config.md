# Contract: Fortuno Tenant Configuration

**Feature**: 001-fortuno-tenant
**Date**: 2026-04-17

Esta feature não expõe novos endpoints HTTP/REST — todos os contratos da API (Login, Register,
Password Recovery, etc.) são idênticos aos dos tenants existentes; mudam apenas as credenciais
resolvidas em runtime. O único "contrato" novo é o **contrato de configuração** que as três
alterações precisam respeitar para que a plataforma reconheça o tenant "fortuno".

---

## 1. Contrato do arquivo `.env.prod.example`

Duas variáveis **devem** ser declaradas, após o bloco do tenant `monexup`, com o formato
exatamente igual:

```dotenv
# Tenant: fortuno
FORTUNO_CONNECTION_STRING=Host=your_db_host;Port=5432;Database=fortuno_db;Username=your_user;Password=your_password
FORTUNO_JWT_SECRET=your_fortuno_jwt_secret_at_least_64_characters_long
```

**Regras**:
- Ambas as chaves em UPPER_CASE com prefixo `FORTUNO_`.
- Placeholders textuais (não valores reais).
- Ordem: connection string primeiro, JWT secret depois (consistente com os demais tenants).

---

## 2. Contrato do arquivo `NAuth.API/appsettings.Production.json`

Adicionar objeto `"fortuno"` sob `Tenants`, após o objeto `"monexup"`:

```json
"fortuno": {
  "ConnectionString": "",
  "JwtSecret": "",
  "BucketName": "Fortuno"
}
```

**Regras**:
- Chave em minúsculas exatamente igual ao `TenantId` canônico.
- `ConnectionString` e `JwtSecret` começam vazios (serão sobrescritos em runtime pelas env vars
  mapeadas no `docker-compose-prod.yml`).
- `BucketName` em PascalCase com literal `"Fortuno"`.
- Nenhuma chave existente (`emagine`, `viralt`, `devblog`, `bazzuca`, `monexup`) pode ser
  modificada ou reordenada.

---

## 3. Contrato do arquivo `docker-compose-prod.yml`

Adicionar, no serviço `nauth-api`, dois mapeamentos após o bloco `monexup`:

```yaml
# Maps to appsettings: Tenants.fortuno.ConnectionString
Tenants__fortuno__ConnectionString: ${FORTUNO_CONNECTION_STRING}
# Maps to appsettings: Tenants.fortuno.JwtSecret
Tenants__fortuno__JwtSecret: ${FORTUNO_JWT_SECRET}
```

**Regras**:
- Prefixo `Tenants__fortuno__` (duplo underscore = separador de seção no ASP.NET Core).
- Comentário explicativo em linha acima de cada mapeamento (padrão dos tenants existentes).
- Valores resolvidos via env vars (`${FORTUNO_CONNECTION_STRING}` e `${FORTUNO_JWT_SECRET}`).
- Nenhum outro serviço, rede ou volume pode ser alterado.

---

## 4. Contrato de runtime (sem mudança — herdado da plataforma)

Consumidores externos (clientes REST) **não** precisam alterar nada nos seus contratos. Basta
enviar o header `X-Tenant-Id: fortuno` em rotas não autenticadas ou obter um token JWT com claim
`tenant_id: "fortuno"` em rotas autenticadas. Todos os endpoints existentes
(`/api/user/*`, `/api/role/*`, etc.) funcionam automaticamente para o novo tenant após o deploy.

### Exemplos

**Login como tenant fortuno (não autenticado — header)**:

```http
POST /api/user/login
X-Tenant-Id: fortuno
Content-Type: application/json

{
  "email": "user@fortuno.com",
  "password": "..."
}
```

**Chamada autenticada (JWT claim resolve tenant automaticamente)**:

```http
GET /api/user/me
Authorization: Basic {token-assinado-com-FORTUNO_JWT_SECRET}
```

---

## Critérios de aceite do contrato

- [ ] Os três arquivos contêm as entradas descritas acima, com sintaxe válida (JSON parseável,
      YAML válido, `.env` sem espaços extras).
- [ ] Nenhum valor real de secret aparece em nenhum dos três arquivos.
- [ ] Ordem e estilo (comentários, indentação) replicam fielmente o diff do tenant monexup
      (commit `f805fe7`).
- [ ] `dotnet build` continua verde.
- [ ] `dotnet test` continua verde (nenhum teste existente quebra; nenhum teste novo é
      necessário — ver Decision 3 do `research.md`).
