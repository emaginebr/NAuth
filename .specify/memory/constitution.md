<!--
Sync Impact Report
==================
Version change: (template / unpopulated) → 1.0.0
Rationale: Initial ratification of concrete principles replacing all placeholder tokens.

Modified principles:
  - [PRINCIPLE_1_NAME]              → I. Skills Obrigatórias (Clean Architecture)
  - [PRINCIPLE_2_NAME]              → II. Stack Tecnológica Fixa
  - [PRINCIPLE_3_NAME]              → III. Convenções de Código .NET
  - [PRINCIPLE_4_NAME]              → IV. Convenções de Banco de Dados (PostgreSQL)
  - [PRINCIPLE_5_NAME]              → V. Autenticação e Segurança

Added sections:
  - Restrições Adicionais (Variáveis de Ambiente + Tratamento de Erros)
  - Fluxo de Desenvolvimento (Checklist para Novos Contribuidores)
  - Governance (preenchida)

Removed sections: none (all placeholders replaced).

Templates requiring updates:
  - .specify/templates/plan-template.md       ⚠ pending — "Constitution Check" section is generic;
    consider mapping to Principles I–V gates (skill usage, stack, code conventions, DB conventions, auth).
  - .specify/templates/spec-template.md       ✅ no update required (no principle-driven mandatory
    sections conflict with the new constitution).
  - .specify/templates/tasks-template.md      ⚠ pending — sample tasks reference Python paths; when
    generating .NET tasks, align with Clean Architecture layers from `dotnet-architecture` skill.
  - .specify/templates/agent-file-template.md ✅ no update required.
  - .specify/templates/checklist-template.md  ✅ no update required.
  - CLAUDE.md                                 ✅ consistent with Principle II and IV already.

Follow-up TODOs: none.
-->

# NAuth Constitution

## Core Principles

### I. Skills Obrigatórias (Clean Architecture)

Toda criação ou modificação de entidades, services, repositories, DTOs, migrations e registro de DI
no backend **DEVE** ser realizada por meio da skill `dotnet-architecture` (`/dotnet-architecture`).
Essa skill encapsula as regras de:

- Estrutura de projetos e fluxo de dependência da Clean Architecture (API → Application → Domain ←
  Infra; Domain depende apenas de `Infra.Interfaces`).
- Repositórios genéricos, mapeamento manual entre Entity ↔ Model ↔ DTO e composição de DI
  centralizada em `NAuth.Application/Initializer.cs`.
- Configuração de `DbContext`, Fluent API e comandos `dotnet ef` para migrações.
- Nomeação de DTOs (`Info`, `InsertInfo`, `Result`) e chaves de resposta em português (`sucesso`,
  `mensagem`, `erros`).

**Rationale**: a skill é a fonte única de verdade para esses padrões. Reimplementar manualmente gera
divergência de convenções entre contribuidores e quebra consistência entre camadas.

### II. Stack Tecnológica Fixa

A stack backend é de adoção obrigatória e não pode ser substituída sem emenda formal a esta
constituição:

| Tecnologia | Versão | Finalidade |
|---|---|---|
| .NET | 8.0 | Runtime e framework |
| Entity Framework Core | 9.x | ORM e migrações |
| PostgreSQL | Latest | Banco de dados relacional |
| NAuth | Latest | Autenticação Basic token |
| zTools | Latest | Upload S3, e-mail (MailerSend), slugs |
| Swashbuckle | 8.x | Swagger / OpenAPI |

Regras inegociáveis:

- **NÃO** introduzir ORMs alternativos (Dapper, NHibernate, etc.). EF Core é o único ORM permitido.
- **NÃO** executar `docker` ou `docker compose` localmente — Docker não está acessível no ambiente
  de desenvolvimento.

**Rationale**: homogeneidade de stack reduz custo de manutenção, treina o time em um único conjunto
de ferramentas e evita divergência de comportamento entre ambientes.

### III. Convenções de Código .NET

Todo código .NET **DEVE** seguir estas convenções:

| Elemento | Convenção | Exemplo |
|---|---|---|
| Namespaces | PascalCase, file-scoped | `namespace NAuth.Domain.Services;` |
| Classes / Interfaces | PascalCase | `UserService`, `IUserRepository` |
| Métodos | PascalCase | `GetById()`, `MapToDto()` |
| Propriedades | PascalCase | `UserId`, `CreatedAt` |
| Campos privados | `_camelCase` | `_repository`, `_context` |
| Constantes | `UPPER_CASE` | `BUCKET_NAME` |

Todas as propriedades de DTOs **DEVEM** declarar `[JsonPropertyName("camelCase")]` para garantir
contrato estável da API independente do case do C#.

**Rationale**: convenções explícitas evitam revisões estilísticas repetitivas e produzem contratos
JSON previsíveis para consumidores externos.

### IV. Convenções de Banco de Dados (PostgreSQL)

Todo esquema PostgreSQL **DEVE** seguir as seguintes regras:

| Elemento | Convenção | Exemplo |
|---|---|---|
| Tabelas | snake_case plural | `users`, `user_roles` |
| Colunas | snake_case | `user_id`, `created_at` |
| Primary Keys | `{entidade}_id`, `bigint` identity | `user_id bigint PK` |
| Constraint PK | `{tabela}_pkey` | `users_pkey` |
| Foreign Keys | `fk_{pai}_{filho}` | `fk_user_role` |
| Delete behavior | `ClientSetNull` | Nunca `Cascade` |
| Timestamps | `timestamp without time zone` | Sem timezone |
| Strings | `varchar` com `MaxLength` | `varchar(260)` |
| Booleans | `boolean` com `default` | `DEFAULT true` |
| Status/Enums | `integer` | `DEFAULT 1` |

Configuração de `DbContext`, Fluent API e comandos de migração (`dotnet ef`) são detalhados na skill
`dotnet-architecture` e não devem ser reimplementados manualmente.

**Rationale**: padronização do esquema garante migrações previsíveis, evita deleções em cascata não
intencionais e preserva compatibilidade com ferramentas de scaffold do projeto.

### V. Autenticação e Segurança

Toda API **DEVE** adotar os seguintes padrões de autenticação e segurança:

| Aspecto | Padrão |
|---|---|
| Esquema | Basic Authentication via NAuth |
| Header | `Authorization: Basic {token}` |
| Handler | `NAuthHandler` registrado no DI |
| Proteção de rotas | Atributo `[Authorize]` nos controllers |

Regras inegociáveis:

- **NUNCA** expor connection strings ou secrets em respostas da API, logs públicos ou mensagens de
  erro retornadas ao cliente.
- Controllers que manipulam dados sensíveis **DEVEM** declarar `[Authorize]` explicitamente.
- `AllowAnyOrigin` em CORS é permitido **apenas** no ambiente `Development`.

**Rationale**: segurança é uma garantia de correção, não uma feature. Qualquer rota sensível sem
`[Authorize]` é um defeito crítico independentemente do comportamento observado.

## Restrições Adicionais

### Variáveis de Ambiente

As seguintes variáveis de ambiente são obrigatórias para execução:

| Variável | Obrigatória | Descrição |
|---|---|---|
| `ConnectionStrings__<nome-do-projeto>Context` | Sim | Connection string PostgreSQL |
| `ASPNETCORE_ENVIRONMENT` | Sim | Valores permitidos: `Development`, `Docker`, `Production` |

Secrets e credenciais **DEVEM** ser fornecidos via variáveis de ambiente ou arquivos `.env`
não versionados — nunca embutidos em `appsettings.json` versionado.

### Padrão de Tratamento de Erros

Controllers **DEVEM** envolver chamadas a services em blocos `try/catch` retornando `StatusCode 500`
com a mensagem da exceção:

```csharp
try { /* lógica */ }
catch (Exception ex) { return StatusCode(500, ex.Message); }
```

Exceções de domínio conhecidas devem ser tratadas em blocos específicos com status apropriados
(`400`, `401`, `404`, etc.) antes do `catch (Exception ex)` genérico.

## Fluxo de Desenvolvimento

### Checklist para Novos Contribuidores

Antes de submeter qualquer PR, o contribuidor **DEVE** confirmar:

- [ ] Utilizou a skill `dotnet-architecture` para novas entidades backend.
- [ ] Tabelas e colunas seguem `snake_case` no PostgreSQL.
- [ ] Controllers com dados sensíveis possuem o atributo `[Authorize]`.
- [ ] Convenções de código .NET (Principle III) foram aplicadas.
- [ ] DTOs declaram `[JsonPropertyName("camelCase")]` em todas as propriedades.
- [ ] Nenhum secret ou connection string foi adicionado a arquivos versionados.

Revisores **DEVEM** rejeitar PRs que violem qualquer item do checklist sem justificativa registrada
na descrição do PR.

## Governance

Esta constituição sobrepõe-se a qualquer outra prática não documentada do projeto. Em caso de
conflito entre um arquivo guia (ex.: `CLAUDE.md`) e esta constituição, **prevalece esta
constituição** até que uma emenda formal concilie ambos.

Emendas:

- Requerem atualização deste arquivo (`.specify/memory/constitution.md`) com nova versão,
  justificativa do bump e propagação para templates dependentes em `.specify/templates/`.
- Versionamento semântico:
  - **MAJOR**: remoção ou redefinição incompatível de princípio ou regra de governança.
  - **MINOR**: adição de novo princípio/seção ou expansão material de orientação existente.
  - **PATCH**: esclarecimentos, correção de redação, refinamentos não semânticos.
- Todo PR que altere arquitetura, stack, convenções de código/banco ou autenticação **DEVE**
  declarar explicitamente se aciona emenda à constituição.

Compliance:

- Todas as revisões de PR **DEVEM** verificar aderência aos princípios I–V.
- Complexidade ou divergência de padrão **DEVE** ser justificada na seção "Complexity Tracking" do
  `plan.md` da feature correspondente.
- Para orientação de execução em tempo de desenvolvimento, consulte `CLAUDE.md` (visão geral do
  projeto) e as skills registradas em `.claude/skills/`.

**Version**: 1.0.0 | **Ratified**: 2026-04-02 | **Last Amended**: 2026-04-02
