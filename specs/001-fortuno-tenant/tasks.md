---

description: "Task list for Fortuno Tenant Onboarding"
---

# Tasks: Fortuno Tenant Onboarding

**Input**: Design documents from `C:\repos\NAuth\NAuth\specs\001-fortuno-tenant\`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/tenant-config.md, quickstart.md

**Tests**: Não há tarefas de teste nesta feature — conforme Decision 3 do `research.md`, os testes
existentes em `NAuth.Test/Tenant/TenantTests.cs` cobrem o contrato do `TenantResolver` de forma
genérica e continuam válidos. Duplicar casos só para o nome "fortuno" adicionaria ruído sem valor.

**Organization**: Tasks agrupadas por user story para permitir implementação e validação
independentes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependências)
- **[Story]**: User story associada (US1, US2)
- Inclui caminhos absolutos/reais dos arquivos

## Path Conventions

Projeto .NET 8 Clean Architecture. Paths usam raiz do repositório
(`C:\repos\NAuth\NAuth\`). Os três arquivos tocados nesta feature estão em:

- `.env.prod.example` (raiz)
- `NAuth.API/appsettings.Production.json`
- `docker-compose-prod.yml` (raiz)

---

## Phase 1: Setup (Shared Prerequisites)

**Purpose**: Garantir que o operador possui tudo o que é necessário antes de editar arquivos.

- [X] T001 Confirmar branch ativa `001-fortuno-tenant` via `git status`; se estiver em outra branch, fazer checkout antes de prosseguir
- [ ] T002 (DEFERRED — external) Obter da equipe de infraestrutura (fora do repo) a connection string PostgreSQL do banco `fortuno_db` e o segredo JWT da Fortuno (≥ 64 caracteres); armazená-los temporariamente em local seguro fora do repositório

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Validar que o estado atual do repositório corresponde à referência esperada (padrão monexup) antes de editar.

**⚠️ CRITICAL**: Nenhuma tarefa de user story pode iniciar antes que esta fase conclua.

- [X] T003 [P] Ler `C:\repos\NAuth\NAuth\.env.prod.example` e confirmar a presença do bloco `# Tenant: monexup` com as variáveis `MONEXUP_CONNECTION_STRING` e `MONEXUP_JWT_SECRET` como referência de estilo
- [X] T004 [P] Ler `C:\repos\NAuth\NAuth\NAuth.API\appsettings.Production.json` e confirmar o objeto `"monexup"` sob `Tenants` como referência estrutural
- [X] T005 [P] Ler `C:\repos\NAuth\NAuth\docker-compose-prod.yml` e confirmar os mapeamentos `Tenants__monexup__ConnectionString` e `Tenants__monexup__JwtSecret` como referência de padrão

**Checkpoint**: Padrão de referência absorvido — edições podem começar.

---

## Phase 3: User Story 1 - Onboard Fortuno as an Isolated Tenant (Priority: P1) 🎯 MVP

**Goal**: Registrar o tenant "fortuno" em produção para que a plataforma o reconheça, acesse a base exclusiva `fortuno_db` e assine/valide tokens com o segredo próprio — sem impactar os 5 tenants existentes.

**Independent Test**: Após o deploy, enviar `POST /api/user/login` com header `X-Tenant-Id: fortuno` e credenciais reais → esperado HTTP 200 com token contendo `tenant_id: "fortuno"`. Enviar login com `X-Tenant-Id: emagine` → esperado continuar funcionando normalmente.

### Implementation for User Story 1

- [X] T006 [US1] Editar `C:\repos\NAuth\NAuth\NAuth.API\appsettings.Production.json` adicionando o objeto `"fortuno": { "ConnectionString": "", "JwtSecret": "", "BucketName": "Fortuno" }` sob `Tenants`, logo após o objeto `"monexup"` (incluir vírgula após o `}` do monexup)
- [X] T007 [US1] Editar `C:\repos\NAuth\NAuth\docker-compose-prod.yml` adicionando no bloco `environment` do serviço `nauth-api`, logo após os mapeamentos do monexup, as linhas: `# Maps to appsettings: Tenants.fortuno.ConnectionString` / `Tenants__fortuno__ConnectionString: ${FORTUNO_CONNECTION_STRING}` / `# Maps to appsettings: Tenants.fortuno.JwtSecret` / `Tenants__fortuno__JwtSecret: ${FORTUNO_JWT_SECRET}`
- [X] T008 [US1] Rodar `dotnet build` na raiz do repositório e confirmar build verde (sem novos warnings de config)
- [X] T009 [US1] Rodar `dotnet test` na raiz do repositório e confirmar que toda a suíte xUnit continua verde, especialmente `NAuth.Test/Tenant/TenantTests.cs`

**Checkpoint**: Infraestrutura de tenant "fortuno" declarada em produção. Build/test verdes localmente. Deploy e smoke test cobertos no Phase 5.

---

## Phase 4: User Story 2 - Operar Fortuno com Configuração Externa Segura (Priority: P2)

**Goal**: Garantir que as credenciais do tenant "fortuno" sejam providas exclusivamente via variáveis de ambiente, nunca versionadas em arquivos commitados.

**Independent Test**: Rodar `git grep` por padrões de segredo nos arquivos alterados → esperado encontrar apenas placeholders (`your_..._password`, `your_..._jwt_secret_at_least_64_characters_long`), nunca valores reais.

### Implementation for User Story 2

- [X] T010 [US2] Editar `C:\repos\NAuth\NAuth\.env.prod.example` adicionando, ao final do arquivo (após o bloco `# Tenant: monexup`), um novo bloco com os placeholders: `# Tenant: fortuno` / `FORTUNO_CONNECTION_STRING=Host=your_db_host;Port=5432;Database=fortuno_db;Username=your_user;Password=your_password` / `FORTUNO_JWT_SECRET=your_fortuno_jwt_secret_at_least_64_characters_long`
- [X] T011 [US2] Auditar o diff desta branch (`git diff main --stat`) e confirmar que apenas 3 arquivos foram modificados: `.env.prod.example`, `NAuth.API/appsettings.Production.json`, `docker-compose-prod.yml`
- [X] T012 [US2] Inspecionar `git diff main` e confirmar que nenhum valor real de connection string ou JWT secret aparece nas linhas adicionadas (somente strings vazias em `appsettings.Production.json` e placeholders `your_*` em `.env.prod.example`)

**Checkpoint**: Credenciais reais exclusivamente em env vars externas; repositório limpo de secrets.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Validação final, deploy e smoke test em produção (conforme `quickstart.md`).

- [ ] T013 [P] (DEFERRED — requires prod server access) Provisionar no servidor de produção o arquivo `.env.prod` com valores reais de `FORTUNO_CONNECTION_STRING` e `FORTUNO_JWT_SECRET` fornecidos em T002
- [ ] T014 (DEFERRED — requires user authorization) Commitar as mudanças com mensagem `fix: add tenant fortuno` (seguindo a convenção semântica do projeto — prefixo `fix:` = patch; usado nos commits anteriores `fix: add tenant bazzuca` e `fix: add tenant monexup`) e abrir PR para `main`
- [ ] T015 (DEFERRED — post-deploy) Após merge e deploy de produção, executar smoke test do `quickstart.md` seção 6.1 (login Fortuno retorna HTTP 200 com `tenant_id: "fortuno"` no token decodificado)
- [ ] T016 [P] (DEFERRED — post-deploy) Após merge e deploy, executar smoke test `quickstart.md` seção 6.3 (login em `X-Tenant-Id: emagine` continua HTTP 200 — não-regressão)
- [ ] T017 [P] (DEFERRED — post-deploy) Após merge e deploy, executar smoke test `quickstart.md` seção 6.4 (token Fortuno enviado com `X-Tenant-Id: emagine` → HTTP 401)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sem dependências — pode começar imediatamente.
- **Foundational (Phase 2)**: Depende da conclusão de Phase 1 (credenciais em mãos, branch correta).
- **User Story 1 (Phase 3)**: Depende de Phase 2 (padrão de referência absorvido).
- **User Story 2 (Phase 4)**: Depende de Phase 2; toca um arquivo diferente (`.env.prod.example`) do Phase 3, então T010 pode ser paralelizada com T006/T007 se executor diferente; T011/T012 dependem dos commits acumulados dos dois phases.
- **Polish (Phase 5)**: T013 depende de T002 (valor real) e pode rodar em paralelo com T008–T012; T014 depende de T006–T012 concluídas; T015/T016/T017 dependem de T014 + deploy externo.

### User Story Dependencies

- **US1 (P1)** — MVP. É o foco principal. Após T009 verde, o tenant está tecnicamente registrado.
- **US2 (P2)** — Pode ser executada em paralelo com US1 por quem tocar `.env.prod.example`; as auditorias T011/T012 validam o conjunto completo e devem rodar após US1 terminar.

### Within Each User Story

- US1: T006 → T007 → T008 → T009 (ordem sequencial porque T008/T009 validam o efeito de T006+T007).
- US2: T010 → T011 → T012.

### Parallel Opportunities

- Phase 2: T003, T004, T005 em paralelo (leituras de 3 arquivos distintos).
- Phase 3 vs Phase 4: T006+T007 (US1) e T010 (US2) tocam arquivos diferentes — podem ser feitas em paralelo por dois executores.
- Phase 5: T016 e T017 rodam em paralelo após o deploy; T013 (provisionar env vars no servidor) pode ser preparada em paralelo com a codificação (T006–T012).

---

## Parallel Example: Phase 2 Readings

```text
# Três leituras independentes de referência, em paralelo:
Task: "Ler C:\\repos\\NAuth\\NAuth\\.env.prod.example e confirmar bloco monexup"
Task: "Ler C:\\repos\\NAuth\\NAuth\\NAuth.API\\appsettings.Production.json e confirmar objeto monexup"
Task: "Ler C:\\repos\\NAuth\\NAuth\\docker-compose-prod.yml e confirmar mapeamentos monexup"
```

## Parallel Example: US1 + US2 Edits

```text
# Dois executores podem editar arquivos distintos simultaneamente:
Executor A: T006 (appsettings.Production.json) → T007 (docker-compose-prod.yml)
Executor B: T010 (.env.prod.example)
# Depois convergem em T008/T009 (build/test) e T011/T012 (auditoria).
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Executar Phase 1 (Setup) — obter credenciais reais.
2. Executar Phase 2 (Foundational) — absorver padrão.
3. Executar Phase 3 (US1) — editar os 2 arquivos de runtime config (+ build/test local).
4. **STOP & VALIDATE**: confirmar que build e testes continuam verdes.
5. Pronto para deploy como MVP — o tenant "fortuno" é reconhecido mesmo sem ainda ter a env var documentada.

### Incremental Delivery

1. Setup + Foundational → referência absorvida.
2. US1 → tenant registrado → build/test verdes (MVP técnico).
3. US2 → documentação de env vars consolidada + auditoria de secrets.
4. Polish → deploy + smoke test em produção.

### Paralelização entre executores

Com dois operadores:

1. Executor A completa T001, T002, T003 e depois toca T006/T007/T008/T009.
2. Executor B completa T004, T005 em paralelo e depois toca T010.
3. Convergem em T011/T012 (auditoria) e T013–T017 (deploy + smoke).

---

## Notes

- [P] = arquivos distintos, sem dependência de task incompleta.
- [Story] = rastreabilidade da task até US1 ou US2.
- Esta feature **não cria código .NET novo** — todas as tasks são edições de configuração ou
  verificações. Isso explica a ausência de tasks de modelo/serviço/endpoint.
- Nenhum comando `docker`/`docker compose` é executado localmente (Princípio II da constituição).
- Commit único com prefixo `fix:` (patch bump do GitVersion) — mesmo padrão dos commits anteriores
  de onboarding de tenant.
- Avoid: inserir o bloco fortuno em posição diferente (fora do final da lista) — quebra a ordem
  consolidada dos diffs anteriores e dificulta revisão.
