# Prompt: Implementação de Multi-Tenant Pattern em API .NET

## Contexto do Projeto

Implemente o padrão **Multi-Tenant** em uma API .NET com as seguintes características:

- A solução possui uma **camada ACL (Anti-Corruption Layer)** responsável por centralizar a resolução do `TenantId`
- A API possui endpoints **autenticados** e **não autenticados**
- O projeto utiliza **JWT Bearer Authentication**
- O isolamento entre tenants é feito por **banco de dados separado por tenant** (via ConnectionString dinâmica)
- Cada tenant possui seu próprio **JwtSecret** para assinar e validar tokens
- **Não devem ser feitas alterações em Models, DTOs ou na estrutura do DbContext existente**

---

## Regras de Resolução do TenantId

### Na Camada ACL

| Cenário | Fonte do TenantId |
|---|---|
| Todos os métodos | `appsettings.json` → chave `Tenant:DefaultTenantId` |

> O `TenantId` na ACL **nunca** é passado como parâmetro nos métodos. Ele é lido do `appsettings.json` e injetado automaticamente como header `X-Tenant-Id` em **todas** as requisições feitas pelo `HttpClient` da ACL, via `DelegatingHandler`.

### Na API

| Cenário | Fonte do TenantId |
|---|---|
| Endpoints **não autenticados** | Header HTTP → `X-Tenant-Id` |
| Endpoints **autenticados** | Token JWT → claim `tenant_id` |

---

## Configuração por Tenant no appsettings.json

Cada tenant possui uma entrada com `ConnectionString` e `JwtSecret` próprios:

```json
{
  "Tenant": {
    "DefaultTenantId": "tenant-a"
  },
  "Tenants": {
    "tenant-a": {
      "ConnectionString": "Server=srv1;Database=TenantA_DB;User Id=sa;Password=***;",
      "JwtSecret": "super-secret-key-tenant-a-256bits"
    },
    "tenant-b": {
      "ConnectionString": "Server=srv2;Database=TenantB_DB;User Id=sa;Password=***;",
      "JwtSecret": "super-secret-key-tenant-b-256bits"
    },
    "tenant-c": {
      "ConnectionString": "Server=srv1;Database=TenantC_DB;User Id=sa;Password=***;",
      "JwtSecret": "super-secret-key-tenant-c-256bits"
    }
  }
}
```

---

## Regras de Resolução das Configurações de Tenant

O `ITenantResolver` (camada ACL) deve expor as seguintes propriedades:

| Propriedade | Origem |
|---|---|
| `string TenantId` | `Tenant:DefaultTenantId` do `appsettings.json` |
| `string ConnectionString` | `Tenants:{tenantId}:ConnectionString` |
| `string JwtSecret` | `Tenants:{tenantId}:JwtSecret` |

- Lance `InvalidOperationException` descritiva para qualquer valor ausente

---

## Camada ACL — Propagação Automática do TenantId via HttpClient

O `TenantId` **não deve ser passado como parâmetro** em nenhum método da ACL. Em vez disso, deve ser injetado automaticamente como header em todas as requisições HTTP realizadas pelo `HttpClient`.

### DelegatingHandler — TenantHeaderHandler

Crie um `DelegatingHandler` chamado `TenantHeaderHandler`:

- Injete `IConfiguration`
- No método `SendAsync`, antes de encaminhar a requisição:
  1. Leia o `TenantId` de `IConfiguration["Tenant:DefaultTenantId"]`
  2. Adicione o header `X-Tenant-Id: {tenantId}` à requisição
- Repasse a requisição com `base.SendAsync(...)`

```csharp
public class TenantHeaderHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;

    public TenantHeaderHandler(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tenantId = _configuration["Tenant:DefaultTenantId"];
        if (!string.IsNullOrEmpty(tenantId))
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);

        return await base.SendAsync(request, cancellationToken);
    }
}
```

### Registro do HttpClient com o Handler

Todo `HttpClient` registrado na ACL deve incluir o `TenantHeaderHandler` na pipeline:

```csharp
services.AddTransient<TenantHeaderHandler>();

services.AddHttpClient<IMinhaAclService, MinhaAclService>()
    .AddHttpMessageHandler<TenantHeaderHandler>();
```

> Registre o `TenantHeaderHandler` para **todos** os `HttpClient` da ACL, sem exceção.

---

## JwtSecret por Tenant — Impacto na Autenticação

Como cada tenant tem seu próprio `JwtSecret`, a validação do JWT **não pode usar uma chave estática**. Use `IssuerSigningKeyResolver`:

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var tenantId = jwt.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;

        var secret = configuration[$"Tenants:{tenantId}:JwtSecret"];
        if (string.IsNullOrEmpty(secret))
            throw new SecurityTokenException($"JwtSecret not found for tenant: {tenantId}");

        return new[] { new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)) };
    },
    ValidateIssuer = false,
    ValidateAudience = false
};
```

### Geração de Token (Login)

1. Resolva o `TenantId` do header `X-Tenant-Id`
2. Leia o `JwtSecret` via `IConfiguration["Tenants:{tenantId}:JwtSecret"]`
3. Inclua a claim `tenant_id` no payload
4. Assine com o `JwtSecret` do tenant

---

## O que deve ser implementado

### 1. Interface e Serviço de Contexto do Tenant

Crie `ITenantContext` e `TenantContext`:

```
- string TenantId { get; }
- Registrado como Scoped
- Na API: resolve via header (não autenticado) ou JWT claim (autenticado)
```

### 2. Middleware de Resolução do Tenant (na API)

Crie `TenantMiddleware`:

- Executa **antes** do pipeline de autenticação
- Endpoints não autenticados: lê `X-Tenant-Id` do header → armazena em `HttpContext.Items["TenantId"]`
- Retorna `400 Bad Request` se ausente onde obrigatório

### 3. Camada ACL — TenantResolver

Implemente `ITenantResolver` / `TenantResolver`:

- Lê `TenantId` exclusivamente de `IConfiguration["Tenant:DefaultTenantId"]`
- Expõe `ConnectionString` e `JwtSecret` a partir da seção `Tenants:{tenantId}`
- Lance exceção descritiva para qualquer valor ausente

### 4. Camada ACL — TenantHeaderHandler

Implemente o `DelegatingHandler` conforme descrito na seção anterior:

- Lê `TenantId` do `appsettings.json`
- Injeta `X-Tenant-Id` em todas as requisições HTTP da ACL
- Registrado em todos os `HttpClient` da ACL via `AddHttpMessageHandler<TenantHeaderHandler>()`

### 5. Configuração Dinâmica do JWT

- `IssuerSigningKeyResolver` no `AddJwtBearer` resolve o `JwtSecret` por tenant
- Claim `tenant_id` incluída em todos os tokens gerados
- Endpoint de login assina com o `JwtSecret` do tenant correto

### 6. Factory de DbContext por Tenant

Crie `ITenantDbContextFactory` / `TenantDbContextFactory`:

- Injete `ITenantResolver`
- Construa `DbContextOptions<AppDbContext>` em runtime com a `ConnectionString` do tenant
- **Não modifique** o `AppDbContext` existente

### 7. Registro de Dependências (DI)

```
- IHttpContextAccessor              → AddHttpContextAccessor()
- ITenantContext                    → Scoped  → TenantContext
- ITenantResolver (ACL)             → Scoped  → TenantResolver
- TenantHeaderHandler               → Transient
- ITenantDbContextFactory           → Scoped  → TenantDbContextFactory
- AppDbContext                      → Scoped  → via ITenantDbContextFactory
- TenantMiddleware                  → UseMiddleware<TenantMiddleware>()
- AddJwtBearer                      → com IssuerSigningKeyResolver dinâmico
- Todos HttpClients da ACL          → .AddHttpMessageHandler<TenantHeaderHandler>()
```

---

## Estrutura de Pastas Esperada

```
src/
├── Api/
│   ├── Middlewares/
│   │   └── TenantMiddleware.cs
│   └── Program.cs
├── ACL/
│   ├── Handlers/
│   │   └── TenantHeaderHandler.cs
│   ├── Interfaces/
│   │   ├── ITenantResolver.cs
│   │   └── ITenantDbContextFactory.cs
│   └── Services/
│       ├── TenantResolver.cs
│       └── TenantDbContextFactory.cs
└── Application/
    ├── Interfaces/
    │   └── ITenantContext.cs
    └── Services/
        └── TenantContext.cs
```

---

## Restrições Obrigatórias

1. **Não altere** Models, DTOs, `DbSet`s ou `OnModelCreating` do `AppDbContext`
2. **Não use** `AddDbContext` estático — use a factory Scoped
3. **Não use** `JwtSecret` global/estático — cada tenant tem o seu próprio
4. **Na ACL, nunca passe o `TenantId` como parâmetro de método** — ele deve ser propagado exclusivamente via header `X-Tenant-Id` pelo `TenantHeaderHandler`
5. **Segurança**: nunca aceite `TenantId` do body — somente do header ou JWT
6. **Segurança**: no `IssuerSigningKeyResolver`, use apenas leitura do token para extrair `tenant_id` — a validação de assinatura ocorre logo após com a chave resolvida
7. **ACL**: não referencie diretamente projetos de infraestrutura — use interfaces
8. **Scoped / Transient**: serviços de tenant são `Scoped`; o `TenantHeaderHandler` é `Transient`

---

## Exemplo de Uso Esperado

```csharp
// ACL Service — sem tenant como parâmetro; header injetado automaticamente
public class ProdutoAclService : IProdutoAclService
{
    private readonly HttpClient _httpClient;

    public ProdutoAclService(HttpClient httpClient)
    {
        _httpClient = httpClient; // TenantHeaderHandler já adiciona X-Tenant-Id
    }

    public async Task<List<ProdutoDto>> GetAllAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<ProdutoDto>>("api/produtos");
    }
}

// API — endpoint não autenticado
[AllowAnonymous]
[HttpGet("public/produtos")]
public async Task<IActionResult> GetPublicProdutos()
{
    // TenantId resolvido do header X-Tenant-Id
    var data = await _service.GetAllAsync();
    return Ok(data);
}

// API — endpoint autenticado
[Authorize]
[HttpGet("private/produtos")]
public async Task<IActionResult> GetPrivateProdutos()
{
    // TenantId resolvido do JWT claim
    var data = await _service.GetAllAsync();
    return Ok(data);
}
```

---

## Entregáveis Esperados

- [ ] `TenantMiddleware.cs`
- [ ] `ITenantContext.cs` e `TenantContext.cs`
- [ ] `ITenantResolver.cs` e `TenantResolver.cs` com `TenantId`, `ConnectionString` e `JwtSecret`
- [ ] `TenantHeaderHandler.cs` — DelegatingHandler que injeta `X-Tenant-Id` em todos os HttpClients da ACL
- [ ] `ITenantDbContextFactory.cs` e `TenantDbContextFactory.cs`
- [ ] Configuração do `AddJwtBearer` com `IssuerSigningKeyResolver` dinâmico
- [ ] Serviço de geração de token (`ITokenService` / `TokenService`) assinando com o `JwtSecret` do tenant
- [ ] Registro completo no `Program.cs` incluindo `AddHttpMessageHandler<TenantHeaderHandler>()` em todos os HttpClients da ACL
- [ ] `appsettings.json` com estrutura `Tenants` unificada
- [ ] Exemplo de teste unitário mockando `ITenantResolver` e `TenantHeaderHandler`