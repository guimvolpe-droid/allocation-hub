# AllocationHub — Guia de Estudo (entrevista técnica Lyncas)

> Documento para você **estudar e falar com autoridade** sobre o projeto. Cobre: (1) o que é e por que
> existe, (2) a arquitetura atual em detalhe, (3) os 4 recursos v2 que estamos adicionando, (4) um banco de
> falas prontas e perguntas prováveis. Regra da casa: **verdade sem inflar** — você construiu isto, é seu.

---

## 0. TL;DR para os primeiros 2 minutos

O **AllocationHub** é um gerenciador de **alocação de consultores para uma software house**: cadastra
consultores, clientes e demandas, e faz o **match** do consultor certo para a demanda certa por **skill,
senioridade e disponibilidade**. É **Angular 18 + .NET 8 + Clean Architecture**. A regra de match é **pura,
determinística e testada** no núcleo (sem banco, sem framework); a explicação em linguagem natural fica
**atrás de uma interface**, então a IA **nunca entra no caminho crítico** e a regra nunca acopla a um
fornecedor. Foi construído **Spec-Driven**, com revisão crítica de cada linha que a IA gerou.

**A frase-mãe:** *"É literalmente o negócio de vocês (alocar consultores), na stack de vocês (.NET + Angular),
com a postura de IA de vocês (Spec-Driven, determinístico, sem hype)."*

---

## 1. O problema e a decisão de design central

Uma fábrica de software tem **consultores** (cada um com skills, senioridade e disponibilidade) e recebe
**demandas de clientes** (cada uma exigindo certas skills e uma senioridade mínima). A pergunta de negócio é:
*qual consultor alocar em qual demanda?*

A decisão de design que sustenta o projeto inteiro:

> **A recomendação tem que ser explicável.** Em staffing, um match que você não consegue justificar para o
> cliente ou para o gestor é inútil. Por isso o **score é determinístico** (mesma entrada → mesma saída,
> sempre) e a **IA só reescreve a explicação em linguagem natural** — ela nunca decide o resultado.

Isso é o oposto de "joga tudo num LLM e reza". É a postura que a vaga pede: **IA como ferramenta de
produtividade com governança**, não piloto automático.

---

## 2. Arquitetura atual (o que já está pronto e testado)

### 2.1 Clean Architecture — as dependências apontam para dentro

```
┌─────────────────────────────────────────────────────────┐
│  AllocationHub.Api  (ASP.NET Core — controllers, DI,     │
│                      JWT, Swagger, CORS)                  │
│        │ depende de                                       │
│        ▼                                                  │
│  AllocationHub.Infrastructure  (EF Core + SQLite,        │
│        │              BCrypt, JWT, explicação de match)   │
│        ▼ depende de                                       │
│  AllocationHub.Core  ◄─── NÃO depende de nada             │
│     (domínio puro: entidades, enums, DTOs, e o           │
│      MatchingService — a regra de negócio)               │
└─────────────────────────────────────────────────────────┘
   AllocationHub.Tests → referencia SÓ o Core (testa a regra pura)
```

Por que importa: o **Core** não conhece banco nem web. Você poderia trocar SQLite por SQL Server, ou a API
REST por gRPC, **sem tocar na regra de negócio**. Isso é o que "framework e banco nas bordas" significa na
prática — e é exatamente o que você diz quando perguntarem "como você desenha uma arquitetura?".

### 2.2 Entidades do domínio (`Core/Domain`)

- **User** — admin do sistema (login). Papel único `Admin` no MVP.
- **Consultant** — `Name, Email, Seniority, Location, Availability, HourlyRate, Skills: List<string>`.
- **Client** — `Name, Industry, ContactName, Demands`.
- **Demand** — `Title, Description, RequiredSeniority, RequiredSkills: List<string>, Status, Client`.
- **Allocation** — liga um Consultant a uma Demand, com `StartDate/EndDate/Status`.
- **MatchingSettings** — os pesos do match (persistidos, editáveis pelo admin, auditados).
- **AuditLog** — quem fez o quê (Create/Update/Delete/mudança de pesos).

Enums comparáveis: `Seniority { Junior=0, Mid=1, Senior=2, Lead=3 }` — o `>=` do C# compara direto
(consultor atende se `Seniority >= RequiredSeniority`). `Availability { Available, Allocated, Unavailable }`.

### 2.3 O coração: `MatchingService` (puro e determinístico)

Arquivo `Core/Matching/Matching.cs`. Fórmula de pontuação (pesos default, todos configuráveis):

| Regra | Pontos | Condição |
|---|---:|---|
| Consultor **disponível** | **+50** | `Availability == Available` |
| Cada **skill** exigida que ele tem | **+10** cada | match **case-insensitive** (".net" == ".NET") |
| **Senioridade** atende | **+20** | `Seniority >= RequiredSeniority` |
| Consultor **já alocado** | **−30** | `Availability == Allocated` (penalidade) |

`Score(demand, consultant, weights)` devolve um **`MatchScore`** transparente: o número, as skills casadas,
as faltantes, e se atende senioridade. `Rank(...)` ordena todos os candidatos por score desc, desempatando
por senioridade e depois nome. **É a única coisa que precisa de teste unitário** — e tem 6 testes cobrindo
os casos (full match = 100, penalidade de alocado, gate de senioridade, case-insensitive preservando o
casing original, ordenação, e pesos customizados mudando o resultado).

**Exemplo concreto para você recitar:** demanda "Senior .NET integration engineer" exige Senior +
`[.NET, Kafka, SQL Server]`. Um consultor Senior, disponível, com `[.NET, Kafka, SQL Server, Azure]` faz
**+50 (disponível) +20 (senioridade) +30 (3 skills) = 100**, zero skills faltantes.

### 2.4 O hook de IA já desenhado: `IMatchExplanationService`

No mesmo arquivo do Core:

```csharp
public interface IMatchExplanationService
{
    string Explain(Demand demand, Consultant consultant, MatchScore score);
}
```

Hoje há **uma** implementação, a `DeterministicMatchExplanationService` (Infrastructure): transforma o score
numa frase — *"Full technical fit, seniority meets the requirement, and available now. Missing: Kafka."* —
**sem chamar nada externo**. É a linha-base honesta: a demo **nunca depende de um modelo estar no ar**.

O comentário no `Program.cs` já diz, literalmente: *"Swap this single line for an LLM impl (behind the same
interface) to get model-written explanations — without touching the matching rule."* → **é aqui que o
multi-LLM da v2 entra.**

### 2.5 Persistência (`Infrastructure/Data`)

EF Core + **SQLite** (zero-setup; trocar para SQL Server é uma linha de connection string + provider). O
`List<string>` de skills é gravado como string delimitada por `|` via **value converter** (SQLite não tem
tipo array). O banco é criado e **populado com dados realistas** no startup (8 consultores, 3 clientes
fintech/health/logística, 4 demandas, 2 alocações). Login demo: `admin@demo.com` / `admin123`.

### 2.6 API e segurança (`Api`)

Controllers REST para Auth, Consultants, Clients, Demands, Allocations, Dashboard, Settings, Audit.
Autenticação **JWT** (BCrypt para hash de senha, token assinado com chave simétrica). Enums serializados
como **string** no JSON (contrato mais legível pro Angular). **Swagger** com suporte a Bearer. CORS liberado
para o dev server do Angular. O endpoint-estrela: **`GET /api/demands/{id}/matches`** — lê os pesos
persistidos, roda o `MatchingService` e devolve o ranking explicado.

### 2.7 Frontend (`web/` — Angular 18, standalone + Material)

Componentes standalone, lazy-loaded: login, dashboard (contadores + top demandas), CRUDs de
consultants/clients/demands, **match** (ranking com score colorido + chips de skills casadas/faltantes +
explicação + botão Alocar), allocations, settings (form de pesos), audit. Auth com **Angular Signals**,
interceptor que injeta o Bearer e trata 401, guard de rota. `ApiService` é um wrapper tipado — um único
lugar que conhece os endpoints.

---

## 3. Os 4 recursos da v2 (a "pimenta") — o que muda e por quê

> Princípio: **tudo novo respeita a Clean Architecture existente.** O `Core` continua sem dependência de
> rede; GitHub e LLM vivem na `Infrastructure`, atrás de interfaces declaradas no `Core`. A regra de match
> **não é tocada**.

### 3.1 Busca de candidatos com **dados reais** (API do GitHub)

**O quê:** a partir de uma demanda, buscar **desenvolvedores reais no GitHub** cujas skills (linguagens e
tópicos dos repositórios que eles realmente mantêm) casam com a demanda, e ranqueá-los pelo **mesmo**
`MatchingService`.

**Por que GitHub e não LinkedIn (fala honesta e importante):** o LinkedIn **não tem API pública de busca de
pessoas**, e scraping viola o ToS deles — seria um red flag numa entrevista de engenharia. Para um sistema
de staffing de **desenvolvedores**, a fonte real, gratuita e compliant é a **API oficial do GitHub**: perfis
reais, skills inferidas dos repos reais. E o mais importante em termos de arquitetura: coloquei isso atrás de
uma interface **`ICandidateSource`** — LinkedIn, Stack Overflow, ou qualquer provider plugam depois **sem
tocar no motor de match**.

**Como:** interface `ICandidateSource` no Core; `GitHubCandidateSource` na Infra usa `IHttpClientFactory`,
busca usuários (`/search/users?q=language:...+location:...`), lê o perfil e os repos, infere skills e uma
senioridade por heurística determinística. Novo endpoint `GET /api/demands/{id}/external-matches`. No front,
um botão "Buscar no GitHub" mostra cartões com **foto, nome e link real** do perfil, score e explicação.

**Fala de ouro:** *"LinkedIn não tem people-search compliant, então para um staffing de devs eu busco
candidatos reais na API do GitHub — atrás de um `ICandidateSource`, de forma que a regra de match nem sabe de
onde o candidato veio."*

### 3.2 **Multi-LLM** parametrizável e trocável

**O quê:** a explicação do match (e o enriquecimento do perfil do GitHub) escrita por um **LLM real**, com
**vários provedores configuráveis e trocáveis em runtime**: Groq, OpenRouter, OpenAI e **Ollama** (local).

**Como (o truque elegante):** todos esses provedores falam o **mesmo protocolo OpenAI-compatível**
(`POST /chat/completions`). Então há **um** cliente HTTP (`OpenAiCompatibleChatClient`) que serve todos —
muda só `BaseUrl`, `Model` e a `ApiKey`. Um `ILlmProviderRegistry` lê a config (uma **lista** de provedores),
instancia um cliente por provider habilitado e resolve por nome. A `LlmMatchExplanationService` implementa a
`IMatchExplanationService` que **já existia** e, **em qualquer erro ou timeout, cai no determinístico** — a
lista e o score nunca quebram.

**Parametrização por API key:** as chaves vêm **só de variável de ambiente** (`.env`, fora do git); o
`appsettings` guarda apenas o *nome* da env. Zero segredo no repositório.

**A prova visível do "multi-LLM":** um seletor na tela ("Explicação por: [Groq ▾]") — troca o provider e a
explicação é **regerada por outro modelo**, ao vivo, sem tocar na regra de negócio.

**Fala de ouro:** *"É multi-LLM de verdade: um cliente OpenAI-compatível serve Groq, OpenRouter, OpenAI e
Ollama local; troco de modelo em runtime e, se o modelo cair, o sistema degrada pro determinístico. A regra
de match nunca dependeu de nenhum fornecedor."*

### 3.3 **Docker**

**O quê:** `docker compose up` sobe o sistema inteiro (a Lyncas usa Docker). Dockerfile multi-stage para a
API (.NET SDK builda e testa → runtime enxuto do ASP.NET), Dockerfile para o Angular (Node builda → **nginx**
serve o estático). O nginx também faz **proxy `/api` → container da API**, o que elimina o CORS em produção e
o problema da URL hardcoded. `docker-compose.yml` orquestra os dois na mesma rede, com volume pro SQLite e as
chaves via `.env`. (O Ollama fica como serviço opcional/comentado.)

**Fala:** *"Multi-stage pra imagem pequena: o SDK builda e roda os testes no primeiro estágio, o runtime só
carrega o publish. O front é servido por nginx, que ainda reverse-proxeia a API — um domínio só, sem CORS
em produção."*

### 3.4 **CI/CD** (GitHub Actions)

**O quê:** pipeline verde para apresentar. Um job builda e **testa** o backend (.NET, com o flag de
globalização invariante que este ambiente exige), outro builda o frontend (`npm ci` + `ng build`), e um
terceiro valida que as **imagens Docker sobem**. Badge de status no README.

**Fala:** *"Qualidade é rede pra mudar rápido, não burocracia. Meu CI builda, testa e valida as imagens a
cada push — se ficar vermelho, não mergeia."*

---

## 4. Banco de perguntas prováveis (técnicas) + respostas curtas

**"Por que o match é determinístico e não um LLM?"** → Explicabilidade. Em staffing, preciso justificar por
que o consultor X foi recomendado. O score é auditável; a IA só verbaliza. E mantém a regra testável e sem
acoplar a fornecedor.

**"Como você garantiria isso em escala / SQL Server?"** → A regra é pura e não toca banco, então escala é
questão de infra: trocar SQLite por SQL Server (uma linha), indexar as colunas de filtro (skills numa tabela
normalizada quando precisar consultar/analytics), paginar a busca, e cachear resultados de match por demanda.
Hoje o `List<string>` é string delimitada — o próximo passo natural é uma tabela `Skill` normalizada.

**"E se o LLM alucinar na explicação?"** → Ele não decide nada — só reescreve um score que já está correto.
Se cair ou vier ruim, o fallback determinístico assume. O pior caso é uma frase menos bonita, nunca uma
recomendação errada.

**"Como você testaria a busca do GitHub?"** → A parte pura (mapear candidato→consultor, heurística de
senioridade) tem teste unitário. A camada HTTP eu isolo atrás da `ICandidateSource` e testaria com um handler
HTTP fake/gravado (sem bater na rede real no CI); no MVP, a chamada real é exercitada na demo.

**"Clean Architecture não é over-engineering pra um app desse tamanho?"** → É a menor separação que me dá o
que importa: a regra de negócio testável e independente de banco/web. Não fiz microserviço, não fiz CQRS, não
fiz event sourcing — cortei tudo isso de propósito (está escrito na spec). Divido quando a dor justifica.

**"Como os pesos do match são mudados sem deploy?"** → Estão persistidos numa tabela `MatchingSettings`,
editáveis pelo admin numa tela, auditados (quem mudou, quando). O `MatchingService` recebe os pesos como
**input** (value object), então a regra continua pura — reconfigura o algoritmo sem mudar código.

**"Spec-Driven na prática?"** → Primeiro escrevo a spec (o que o incremento faz, invariantes, testes),
depois a IA gera contra a spec, e **eu reviso cada linha e assino embaixo**. Testes verdes + revisão são o
gate, não a origem do código. A regra de match eu mantive determinística e testada justamente pra IA não
sentar no que decide o resultado.

---

## 5. Roteiro de demo (5 minutos)

1. **Login** (`admin@demo.com` / `admin123`) → **Dashboard** (contadores, top demandas).
2. Abrir a demanda **"Senior .NET integration engineer"** → **Ver matches**: ranking interno, Gustavo 100
   (full fit), explicação em linguagem natural.
3. **Trocar o LLM** no seletor (Groq → outro) → a explicação é **regerada por outro modelo**. Depois "derrubar"
   a chave para mostrar o **fallback determinístico** (o score não muda).
4. **Buscar no GitHub** → aparecem **devs reais** (links que abrem o perfil real), ranqueados pela mesma regra,
   com skills inferidas dos repos.
5. Mostrar o **Settings** (mudar um peso, re-rankear) e o **Audit log**.
6. Fechar no **Docker** (`docker compose up`) e no **CI verde** no GitHub Actions.

---

## 6. Trava de honestidade (não fure)

- Você **construiu** isto — Angular + .NET + Clean Architecture, Spec-Driven, com testes. É seu, fale com
  autoridade.
- A busca é do **GitHub** (real), **não** do LinkedIn — e o motivo (sem API compliant de people-search) é uma
  resposta forte, não uma desculpa. **Não** diga que integrou o LinkedIn.
- O LLM **enriquece**, não decide. Não venda "IA que faz o match" — venda "regra auditável + IA na borda".
- Se perguntarem "isso foi pra um cliente?": honesto — *"construí como projeto real pra dominar a engenharia
  de IA em produção; a experiência de cliente de grande porte eu trago do financeiro."*

---

## 7. Status v2 — está CONSTRUÍDO e provado (o que rodar e o que falta de você)

Os 4 recursos + guardrails/evals estão implementados, testados e commitados (branch
`feat/v2-multillm-github-docker`). **17 testes verdes** (regra de match + evals do guardrail do LLM).

**Provado ao vivo:**
- Busca real do GitHub retornando devs reais (ex.: Baltieri/Pires/Groffe para .NET; Sebastian Lague para
  Azure/.NET) — só pessoas (`type:user`), skills inferidas dos repos.
- Multi-LLM: com chave setada o provider aparece em `/api/llm/providers`; chamada inválida cai no
  determinístico sem erro (resiliência). Sem chave, a app roda 100% no determinístico.

**Como rodar (dev):**
```bash
# backend (localhost:5080)
cd src/AllocationHub.Api && DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet run
# frontend (localhost:4200)
cd web && npm install && npm start
```
**Como rodar (tudo em Docker):** `docker compose up --build` → http://localhost:8080.

**Passos SEUS para o LLM aparecer ao vivo (grátis, ~2 min) — Groq:**
1. Entrar em https://console.groq.com (login com Google/GitHub).
2. Menu esquerdo → **API Keys** → **Create API Key** → nome "allocationhub" → **Submit** → copiar (`gsk_...`).
3. No terminal, antes de subir a API: `export GROQ_API_KEY=gsk_...` (ou colocar num arquivo de ambiente
   local — **nunca** commitar). Suba a API de novo → o seletor "Explanation by: [groq]" aparece na tela de
   match; troque o provider e veja a explicação ser reescrita pelo modelo.

**(Opcional) Token do GitHub** (sobe o limite de 60 → 5000 buscas/h):
https://github.com/settings/tokens?type=beta → **Generate new token** (fine-grained) → *Public Repositories
(read-only)* → copiar (`github_pat_...`) → `export GITHUB_TOKEN=github_pat_...`.

**Falas novas para a entrevista técnica (além das da §4):**
- *"Coloquei um guardrail entre o LLM e a tela: a saída do modelo só é exibida se passar num eval que
  rejeita skill alucinada, eco de prompt-injection e tamanho fora do limite — senão volta pro determinístico.
  Esse mesmo eval roda no CI como quality gate."*
- *"Os provedores de LLM entram por uma lista de config; um cliente OpenAI-compatível serve todos. Um
  provider só é oferecido se a chave dele existir no ambiente — segredo nunca vai pro git nem pro modelo."*
- *"Docker multi-stage: o estágio de build roda os testes, então um commit quebrado não gera imagem. O
  front é servido por nginx que ainda reverse-proxeia a API — um domínio só, sem CORS em produção."*
```
