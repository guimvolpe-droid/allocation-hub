# AllocationHub — Guia de Apresentação (entrevista técnica Lyncas · 16/07 17:00)

> Este é o SEU roteiro. Tudo que está aqui é verdade e está rodando — você construiu, pode falar com
> autoridade. Termos técnicos vêm explicados **entre parênteses** na primeira vez que aparecem.
> Leia de cima pra baixo uma vez; depois use o índice para revisar o que precisar.

---

## 1. O pitch (decore estas 4 frases)

> "Pra estudar o problema de vocês, eu construí o **AllocationHub**: um gerenciador de **alocação de
> consultores** para uma software house — cadastra consultores, clientes e demandas, e recomenda o
> consultor certo para cada demanda por **skill, senioridade e disponibilidade**.
>
> É **Angular + .NET 8 com Clean Architecture** (arquitetura em camadas onde a regra de negócio fica no
> centro, isolada de banco e framework). A regra do match é **determinística e testada** — sempre dá o
> mesmo resultado para a mesma entrada, e eu consigo explicar cada ponto do score.
>
> A IA entra **na borda, nunca no centro**: um LLM (modelo de linguagem, tipo o ChatGPT) reescreve a
> explicação do match em linguagem natural — e eu posso **trocar de provedor de IA ao vivo**, com
> guardrail e fallback. Se a IA cair, o sistema nem percebe.
>
> E é tudo de verdade: busca **desenvolvedores reais na API do GitHub**, grava num **PostgreSQL real
> (Supabase)**, roda em **Docker**, tem **CI** verde no GitHub e usa as **cores de vocês** — porque foi
> feito para esta conversa."

**Frase-mãe (se só puder dizer uma coisa):** *"É o negócio de vocês (alocar gente), na stack de vocês
(.NET + Angular), com a postura de IA de vocês (Spec-Driven, explicável, sem hype)."*

---

## 2. Roteiro da demo — clique a clique, com o que DIZER

> Antes de começar: API rodando (`:5080`), front rodando (`:4200`), aba do GitHub aberta no repo,
> aba do Supabase aberta no Table Editor. Login: `admin@demo.com` / `admin123`.

### Passo 1 — Login (30s)
**Clique:** abra http://localhost:4200.
**Diga:** "Autenticação com **JWT** (JSON Web Token — um 'crachá' digital assinado que o servidor emite
no login e o navegador apresenta a cada chamada; o servidor valida a assinatura sem precisar de sessão).
Senha guardada com **BCrypt** (algoritmo de hash feito para senhas — lento de propósito, para dificultar
ataque de força bruta; o banco nunca vê a senha em si)."
**Se o botão do Google estiver visível:** "Também fiz **OAuth com Google** (protocolo onde você delega o
login a outro provedor: o Google prova quem o usuário é e me entrega um token; eu valido esse token
criptograficamente e emito o MEU crachá). E repare: se o Google não estiver configurado, o botão nem
aparece — degradação graciosa."

### Passo 2 — Dashboard (30s)
**Diga:** "Visão operacional: quantos consultores, quantos disponíveis, demandas abertas. Reparem no
visual — usei a paleta de vocês, o roxo e o verde da Lyncas. Detalhe: se a API cair, essa tela mostra um
erro claro em vez de ficar num 'Loading' infinito — cuidado de demo E de produção."

### Passo 3 — Match interno (2min — o coração)
**Clique:** Demands → "Senior .NET integration engineer" → botão Matches.
**Diga:** "Aqui está o coração do sistema. Cada consultor ganha um **score determinístico**
(determinístico = mesma entrada, sempre a mesma saída; sem sorteio, sem IA decidindo): +50 se está
disponível, +10 por skill exigida que ele tem, +20 se a senioridade atende, −30 se já está alocado.
Esses pesos são **configuráveis pelo admin** numa tela, ficam gravados no banco e toda mudança é
**auditada** (fica registrado quem mudou e quando).
Por que determinístico? Porque em staffing uma recomendação que você não consegue **justificar** para o
cliente é inútil. O Gustavo tem 100: disponível, sênior, e as 3 skills. Está explicado — chip verde é
skill que casou, vermelho é o que falta."

### Passo 4 — Trocar a IA ao vivo (2min — a pimenta nº 1)
**Clique:** no seletor "Explanation by", troque de "Deterministic (offline)" para **groq**.
**Diga:** "Agora a explicação foi **reescrita por um LLM de verdade** (modelo de linguagem — aqui o
Llama 3.1 rodando na Groq, um provedor de nuvem). Três coisas importantes:
1. **O score não mudou.** A IA só reescreve o texto — ela **nunca decide** o match.
2. É **multi-LLM**: um único cliente fala com Groq, OpenRouter, OpenAI e Ollama local, porque todos
   usam o mesmo protocolo (o formato de API que a OpenAI criou virou padrão de mercado). Trocar de
   provedor é configuração, não código.
3. Toda resposta do modelo passa por um **guardrail** (validador de saída): se o modelo inventar uma
   skill que não existe no candidato — **alucinação** (quando o modelo afirma algo falso com confiança)
   — ou ecoar tentativa de **prompt injection** (texto malicioso vindo de fora tentando dar ordens ao
   modelo), a resposta é descartada e volta o texto determinístico. E esse guardrail roda como teste no
   CI — eu testo o modo de FALHA da IA, não só o caminho feliz."
**Se a Groq estiver lenta/fora:** "Perfeito, olhem — caiu no fallback determinístico sem quebrar nada.
Era exatamente isso que eu queria mostrar." *(Falha vira demonstração. Você não tem como perder.)*

### Passo 5 — Buscar devs REAIS no GitHub (2min — a pimenta nº 2)
**Clique:** role até "Source real candidates · GitHub", digite `Brazil`, clique **Search GitHub**.
**Diga:** "Isso é dado **real**, ao vivo: a **API oficial do GitHub** (interface pública que o GitHub
oferece para programas consultarem dados). Busco desenvolvedores pela linguagem exigida na demanda,
leio os repositórios de cada um, **infiro as skills do que a pessoa realmente mantém** no GitHub, e uma
heurística determinística estima a senioridade (idade da conta, volume de repositórios, seguidores).
E o ranking? **É o MESMO motor de match** — o candidato externo entra na mesma régua do interno.
Por que GitHub e não LinkedIn? Porque o LinkedIn **não tem API pública de busca de pessoas** e raspar o
site deles (scraping) viola os termos de uso — numa fábrica que atende banco, compliance importa. Então
desenhei uma **interface `ICandidateSource`** (contrato de código: quem quiser ser fonte de candidatos
precisa saber 'buscar'; de onde vem é detalhe): o GitHub é a primeira fonte; LinkedIn, se um dia houver
acesso legítimo, pluga sem tocar no motor."
**Clique nos links:** "São perfis reais — o André Baltieri, por exemplo, referência .NET no Brasil."

### Passo 6 — Importar para o banco AO VIVO (1min — fecha o ciclo)
**Clique:** botão **Import** num candidato → depois Consultants no menu.
**Diga:** "Um clique e o dev real virou um consultor **persistido no banco** — e olhem o ranking
interno: ele já aparece pontuado. O import é **idempotente** (pode clicar duas vezes: a segunda não
duplica, devolve o mesmo registro) e **auditado**."
**Troque para a aba do Supabase (Table Editor):** "E aqui está a linha dele no **PostgreSQL de
verdade**, gerenciado pelo Supabase (plataforma que hospeda Postgres na nuvem). Não é arquivo local —
é banco de produção, conectado pelo **pooler** (intermediário que gerencia um pool de conexões — abrir
conexão de banco é caro; o pooler reusa)."

### Passo 7 — Arquitetura + saúde (2min)
**Clique:** abra http://localhost:5080/api/health numa aba.
**Diga:** "Endpoint de saúde: qual banco está ativo, se conecta, quantas linhas. Primeiro lugar que se
olha quando um deploy não sobe. E um detalhe de arquitetura que me orgulho: para trocar de SQLite
(banco em arquivo, zero-setup, ótimo pra dev) para o Postgres do Supabase, eu mudei **uma linha de
configuração**. A regra de negócio **nunca soube** qual banco existe — isso é a Clean Architecture
pagando o aluguel."
**Abra o Swagger** (http://localhost:5080/swagger): "Toda a API documentada automaticamente
(**Swagger/OpenAPI** = padrão que descreve os endpoints e gera esta tela de teste)."

### Passo 8 — Docker + CI (1min)
**Clique:** aba do GitHub → repo → Actions (ou o badge verde no README).
**Diga:** "**CI** (integração contínua — a cada push, um robô compila, roda os testes e valida) com
três estágios: backend com testes, frontend, e o build das **imagens Docker** (pacote com o app e tudo
que ele precisa, que roda igual em qualquer máquina). O Dockerfile é **multi-stage** (constrói num
estágio pesado com SDK e copia só o resultado para uma imagem enxuta de runtime) — e **roda os testes
durante o build**: commit quebrado não vira imagem. Em produção, o Angular é servido por **nginx**
(servidor web) que também faz **reverse proxy** do `/api` (repassa as chamadas para o container da API
— um domínio só, sem dor de CORS)."
**Fechamento:** "`docker compose up` sobe o sistema inteiro com um comando."

> **Tempo total: ~11 min.** Sobra espaço para perguntas — que é onde você brilha com a seção 4.

---

## 3. Como funciona por dentro (teoria simples, para você INTERNALIZAR)

### 3.1 Clean Architecture em uma imagem

```
Api  (controllers — recebem HTTP)          ← camada de fora
  ↓ depende de
Infrastructure  (banco, GitHub, LLM, JWT)  ← camada do meio
  ↓ depende de
Core  (entidades + regra de match)         ← centro: NÃO depende de NADA
```

- **Regra de ouro: as dependências apontam para DENTRO.** O centro (Core) não conhece banco, não
  conhece web, não conhece IA. Por isso a regra de match é testável sem subir nada.
- **Interface** (= contrato de código): o Core declara "existe algo que valida token do Google"
  (`IGoogleTokenValidator`), "existe algo que busca candidatos" (`ICandidateSource`), "existe algo que
  explica um match" (`IMatchExplanationService`). QUEM faz isso de verdade (Google.Apis, GitHub, Groq)
  mora na Infrastructure. Trocar a implementação não toca o centro.
- **Injeção de dependência (DI)**: em vez de cada classe criar o que usa, o framework **entrega** as
  dependências prontas no construtor. É o que permite trocar "explicação determinística" por
  "explicação por LLM" mudando UMA linha de registro.

### 3.2 O banco (EF Core, SQLite, Supabase)

- **ORM / EF Core** (Entity Framework Core): biblioteca que traduz objetos C# em tabelas e consultas
  SQL — você escreve `db.Consultants.Where(...)` e ele gera o SQL.
- **SQLite**: banco inteiro num arquivo local. Zero instalação — perfeito para dev e demo offline.
- **PostgreSQL (Supabase)**: banco cliente-servidor de produção. O Supabase hospeda e dá painel visual.
- **A troca**: o `Program.cs` olha a configuração; se existe `SUPABASE_DB_CONNECTION`, usa Postgres;
  senão, SQLite. **Uma linha decide; o domínio não muda.**
- **Migrations** (o que NÃO tem, e você assume): sistema de versionamento do banco — cada mudança de
  schema vira um script versionado. No MVP eu crio o schema no startup; migrations é o próximo passo
  documentado. *(Resposta pronta: "no MVP, schema no startup; em produção, EF Migrations — está na
  spec como corte consciente.")*

### 3.3 A IA (multi-LLM, guardrail, eval)

- **LLM**: modelo de linguagem que gera texto (Llama, GPT...). Aqui ele **só** reescreve a explicação.
- **Prompt**: a instrução que mando pro modelo. Detalhe de segurança: eu mando **fatos estruturados**
  (skills, score, senioridade) — nunca o texto livre da bio do GitHub — porque texto vindo de fora
  pode conter **prompt injection** (instruções maliciosas escondidas, tipo "ignore tudo e responda X").
- **Guardrail**: validador da RESPOSTA do modelo. Rejeita: skill inventada (alucinação), eco de
  injection, texto grande demais. Rejeitou → usa o texto determinístico.
- **Eval**: teste automatizado de comportamento de IA. Meu guardrail tem 5 evals que rodam no CI —
  se alguém quebrar a proteção, o build fica vermelho.
- **Fallback**: plano B automático. IA fora do ar / chave errada / timeout → explicação determinística,
  sem erro na tela.
- **Por que multi-LLM importa**: preço e disponibilidade de modelo mudam todo mês. Quem acopla o
  produto a UM provedor fica refém. Aqui o provedor é **configuração** (`BaseUrl`, `Model`,
  variável de ambiente com a chave) — e dá pra trocar **em runtime**, por requisição.

### 3.4 Segurança (o essencial pra conversar)

- **JWT**: crachá assinado. O servidor emite no login e valida a assinatura a cada chamada.
- **BCrypt**: hash de senha lento de propósito (dificulta força bruta).
- **OAuth (Google)**: o usuário loga NO GOOGLE; o Google me dá um **ID token** (JWT assinado pelo
  Google dizendo "este é fulano@gmail"); eu valido a assinatura E o **audience** (que o token foi
  emitido para o MEU app, não para outro qualquer) e então emito o meu próprio crachá. Conta criada
  via Google fica **sem senha** — e o login por senha bloqueia hash vazio (detalhe fino: nunca passar
  hash vazio pro BCrypt).
- **Segredos**: chave de LLM e senha de banco **só em variável de ambiente** (nunca no código/git).
  O Client ID do Google pode ser público (ele identifica o app; quem protege é a validação do token).
- **Anti-injection na busca**: a bio do GitHub é sanitizada (removo quebras/backticks, corto tamanho)
  antes de qualquer uso.

### 3.5 Docker e CI/CD

- **Docker**: empacota app + dependências numa **imagem**; o **container** é a imagem rodando. "Na
  minha máquina funciona" vira "em qualquer máquina funciona".
- **Multi-stage build**: estágio 1 usa a imagem pesada do SDK para compilar E TESTAR; estágio 2 copia
  só o resultado para a imagem enxuta de runtime. Imagem final pequena e sem ferramenta de build.
- **docker compose**: descreve os serviços (api + web) num arquivo; um comando sobe tudo em rede.
- **nginx / reverse proxy**: servidor que entrega o Angular estático e repassa `/api` pro container da
  API. Um domínio só ⇒ sem **CORS** (mecanismo do navegador que bloqueia chamadas entre domínios
  diferentes sem permissão explícita).
- **CI/CD**: robô do GitHub que a cada push compila, testa, e constrói as imagens. Badge verde no
  README = o repositório se prova sozinho.

---

## 4. Perguntas prováveis + SUAS respostas (curtas, na 1ª pessoa)

**"Por que o match não é feito pela IA?"**
→ "Explicabilidade. Em staffing eu preciso justificar a recomendação pro cliente. O score é auditável
e testado; a IA reescreve a explicação. Se ela cair, o produto continua. IA no centro seria hype;
na borda é produtividade com governança."

**"E se o LLM alucinar?"**
→ "Ele não decide nada, então o pior caso seria um texto errado — e nem isso passa: o guardrail
rejeita skill que não existe no candidato e a resposta volta pro determinístico. E isso tem teste no CI."

**"Por que GitHub e não LinkedIn?"**
→ "LinkedIn não tem API pública de busca de pessoas e scraping viola o ToS — risco jurídico e de
conta. Para dev, o GitHub É a fonte oficial e gratuita. E ficou atrás de uma interface: qualquer fonte
futura pluga sem tocar no motor."

**"Como isso escala?"**
→ "A regra é pura e roda em memória — escala horizontal é replicar a API. O gargalo real seria a
busca de candidatos: paginação, cache por demanda, e normalizar skills numa tabela própria quando
houver analytics. Hoje skills são uma lista serializada — corte consciente de MVP, documentado."

**"Por que não microserviços?"**
→ "Porque a dor não justifica. Comecei pelas fronteiras de domínio dentro de um monólito limpo —
que é o que permite extrair um serviço DEPOIS, se precisar. Microserviço no dia 1 é complexidade
sem retorno."

**"Cadê as migrations?"**
→ "Escopo consciente: no MVP o schema nasce no startup. E tem história boa aqui: descobri na prática
que `EnsureCreated` não cria tabelas num Postgres gerenciado [ver §5]. Produção = EF Migrations, está
na spec."

**"Que segurança falta?"** *(assuma na frente, impressiona)*
→ "Pro MVP: RBAC (papéis além de Admin), refresh token (renovar o crachá sem relogar), rate limiting
próprio, mover o token do localStorage para cookie httpOnly, e o /health detalhado atrás de auth.
Tudo mapeado — MVP prova o núcleo, não finge ser produção."

**"Você usou IA pra construir isso?"** *(honesto e forte)*
→ "Usei — como ferramenta, do jeito que a vaga descreve: primeiro a spec (o que o incremento faz,
invariantes, testes), a IA gera contra a spec, e eu reviso criticamente cada linha e assino embaixo.
A regra de match eu mantive determinística e testada justamente pra IA nunca sentar no que decide.
E o código passou por revisão adversarial — os fixes estão no histórico de commits."

**"Quanto tempo levou?"**
→ "O MVP num dia de trabalho focado; a v2 (GitHub real, multi-LLM, Docker, CI, Supabase, OAuth) em
mais um. Spec antes de código e incrementos pequenos com teste verde — é assim que eu ando rápido
sem quebrar."

---

## 5. Histórias de guerra REAIS (conte 1 ou 2 — é o que mais soa sênior)

**A do banco gerenciado (a melhor):**
> "Quando liguei o Supabase, quebrou com 'relation Users does not exist'. Fui na causa raiz:
> `EnsureCreated` do EF só verifica se o **database** existe — no SQLite o arquivo não existia, então
> ele criava tudo; num Postgres gerenciado o database JÁ vem criado, então ele virava no-op silencioso
> e nunca criava as tabelas. Tentei `HasTables` — também mentia, porque o Supabase já vem com os
> schemas internos dele (auth, storage). A solução foi perguntar o que importa: 'a MINHA tabela
> existe?'. Três linhas, e o mesmo código roda nos dois bancos. Moral: comportamento de framework você
> valida no ambiente real, não na suposição."

**A do IPv6:**
> "A conexão direta do Supabase só resolve IPv6, e meu ambiente não tinha rota IPv6. Diagnóstico por
> camadas: DNS resolvia (só AAAA), TCP não alcançava. Caminho certo: o session pooler, que tem IPv4 —
> e descobri a região do pooler por eliminação, lendo o erro do Supavisor ('tenant not found' =
> autenticou no lugar errado, não credencial errada). Saber LER o erro economiza horas."

**A da Microsoft consultora:**
> "Na primeira busca do GitHub, apareceu 'Microsoft' como candidata — a busca devolve organizações
> também. Um qualifier (`type:user`) resolveu. Pequena, mas é o tipo de bug que só aparece quando você
> testa com dados REAIS — por isso eu não mocko o que dá pra fazer de verdade."

---

## 6. Limitações conhecidas + roadmap (assuma ANTES de perguntarem)

| O que falta | Por que ficou de fora | Próximo passo |
|---|---|---|
| EF **Migrations** | MVP cria schema no startup | `dotnet ef migrations` + pipeline |
| **RBAC** (papéis) | 1 papel (Admin) prova o núcleo | Roles + policies por endpoint |
| **Refresh token** | Sessão de 8h basta pra demo | Refresh + revogação |
| Skills **normalizadas** | Lista serializada é suficiente p/ match | Tabela Skill + N:N (habilita analytics) |
| **Testes de integração** | Unit no núcleo + evals de IA cobrem o crítico | WebApplicationFactory no CI |
| Token no **localStorage** | Simplicidade de SPA demo | Cookie httpOnly + CSRF |
| `/health` detalhado público | Prova de demo | Detalhe atrás de auth em produção |
| **Concorrência** de alocação | Fluxo único na demo | Transação + constraint de unicidade ativa |
| Paginação da API | Volume de demo | Skip/take + total |

*(Fala pronta: "é um MVP com cortes CONSCIENTES e documentados — a spec diz o que ficou de fora e
por quê. Prefiro núcleo sólido e honesto a superfície grande e frágil.")*

---

## 7. OAuth Google — status e SEU passo (se quiser mostrar ao vivo)

**Pronto no código:** botão "Sign in with Google" aparece automaticamente quando a API tem o Client ID;
valida o token com audience; cria o usuário no primeiro login; conta Google fica sem senha (e o login
por senha bloqueia hash vazio). Sem Client ID → tela fica como sempre (fallback = senha). 

**Para ativar (~5min, grátis):**
1. https://console.cloud.google.com → New Project → nome `allocationhub` → Create.
2. ☰ → APIs & Services → **OAuth consent screen** → External → nome `AllocationHub` + seu email → Save
   (se ficar "Testing", adicione seu email em Test users).
3. APIs & Services → **Credentials** → + Create Credentials → **OAuth client ID** → Web application →
   origins `http://localhost:4200` e `http://localhost:8080` → Create → copie o Client ID.
4. Suba a API com `GOOGLE_CLIENT_ID=<seu-client-id>` no ambiente → o botão aparece no login.

---

## 8. Dicionário de bolso (consulta rápida)

- **API** — porta de entrada de um sistema para outros programas conversarem com ele.
- **Endpoint** — um endereço específico da API (ex.: `GET /api/health`).
- **REST** — estilo de API sobre HTTP usando verbos (GET lê, POST cria, PUT edita, DELETE apaga).
- **DTO** — objeto simples usado só para transportar dados entre API e tela (não é a entidade do banco).
- **Entidade** — objeto de negócio persistido (Consultant, Demand, Allocation).
- **ORM / EF Core** — traduz objetos ↔ tabelas SQL.
- **Migration** — script versionado de mudança de schema do banco.
- **JWT** — crachá digital assinado; o servidor valida sem guardar sessão.
- **OAuth** — delegar o login a um provedor (Google) e receber um token que prova a identidade.
- **BCrypt** — hash lento e seguro para senhas.
- **DI (injeção de dependência)** — o framework entrega as dependências prontas; facilita trocar peças.
- **Interface** — contrato: "quem me implementar sabe fazer X"; esconde o COMO.
- **Determinístico** — mesma entrada ⇒ sempre a mesma saída.
- **Heurística** — regra prática aproximada (ex.: inferir senioridade por idade de conta/repos).
- **LLM / prompt / alucinação / prompt injection / guardrail / eval / fallback** — ver §3.3.
- **Rate limit** — teto de chamadas por hora que uma API impõe (GitHub: 60/h sem token, 5000/h com).
- **Idempotente** — repetir a operação não muda o resultado (2º import devolve o mesmo registro).
- **Auditoria** — registro de quem fez o quê e quando.
- **Docker / imagem / container / multi-stage / compose** — ver §3.5.
- **nginx / reverse proxy / CORS** — ver §3.5.
- **CI/CD / pipeline / badge** — robô que compila+testa a cada push; badge mostra o status no README.
- **Swagger/OpenAPI** — documentação viva e testável da API.
- **Pooler** — reaproveita conexões de banco (abrir conexão é caro).
- **SPA** — aplicação de uma página só (Angular): o navegador carrega uma vez e navega sem recarregar.
- **Clean Architecture** — camadas com dependência apontando para o centro; regra de negócio isolada.
- **Spec-Driven** — especificação (escopo, invariantes, testes) ANTES do código; IA gera contra a spec
  e o humano revisa criticamente cada linha.

---

## 9. Checklist 16:45 (15 min antes)

- [ ] API de pé com Supabase + Groq (aba `http://localhost:5080/api/health` mostrando **PostgreSQL**).
- [ ] Front de pé (`:4200`), login testado, visual Lyncas conferido.
- [ ] Abas prontas: app · Swagger · GitHub (repo/Actions) · Supabase Table Editor · este guia.
- [ ] `GITHUB_TOKEN` setado se for demonstrar a busca mais de ~5 vezes (rate limit 60/h sem token).
- [ ] Ensaiar 1x o Passo 4 (troca de LLM) e o Passo 6 (import) — são os momentos "uau".
- [ ] Respirar: **tudo nesta demo é real e você construiu.** Falha de rede vira demonstração de
      fallback — você não tem como perder.
