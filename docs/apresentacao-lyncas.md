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

### Passo 1 — Login + OAuth Google (1min)
**Clique:** abra http://localhost:4200 → **entre com o botão do Google** (é mais impressionante que a senha).
**Diga:** "Dois caminhos de login. O de senha usa **BCrypt** (algoritmo de hash feito para senhas — lento
de propósito, para dificultar força bruta; o banco nunca vê a senha). E tem **OAuth com Google**
(protocolo onde você delega o login a outro provedor: o Google prova quem é o usuário e me entrega um
token assinado; eu valido esse token e emito o **meu** crachá).
Em ambos os casos, o que circula depois é o **JWT** (JSON Web Token — um crachá assinado que o navegador
apresenta a cada chamada; o servidor valida a assinatura sem guardar sessão). **O resto do app não sabe
como você entrou** — os dois caminhos convergem no mesmo `AuthResponse`."

**Os 3 detalhes do OAuth que valem citar (escolha 1 ou 2):**
1. *"Não uso **client secret**. O fluxo é ID token validado no backend contra as chaves públicas do
   Google, com checagem de **audience** (confirmo que o token foi emitido pro MEU app, não pra qualquer
   um). Menos segredo guardado é menos superfície de ataque."*
2. *"O botão só aparece se o backend disser que está configurado — tem um `GET /api/auth/config`. Sem
   Client ID, a tela some o botão e o login por senha continua. Mesma degradação graciosa do multi-LLM."*
3. *"Tem **allowlist** por e-mail. Sem ela seria **fail-open**: qualquer conta Google do mundo viraria
   Admin. Foi uma revisão adversarial que eu rodei no próprio código que pegou isso."*

**Se perguntarem da conta criada pelo Google:** *"Ela nasce **sem hash de senha** — e o login por senha
bloqueia hash vazio antes de chamar o BCrypt. Detalhe fino, mas sem isso vira brecha."*

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
**Clique:** role até "Source real candidates · GitHub", digite `São Paulo`, deixe **Results: 10**, clique
**Search GitHub**. *(Repare no loader: o botão vira "Searching GitHub…" e explica o que está fazendo.)*
**Diga:** "Isso é dado **real**, ao vivo: a **API oficial do GitHub** (interface pública que o GitHub
oferece para programas consultarem dados). Busco desenvolvedores pela linguagem exigida na demanda,
leio os repositórios de cada um, **infiro as skills do que a pessoa realmente mantém**, e uma heurística
determinística estima a senioridade (idade da conta, volume de repositórios, seguidores).
Por que GitHub e não LinkedIn? Porque o LinkedIn **não tem API pública de busca de pessoas** e raspar o
site deles (scraping) viola os termos de uso — numa fábrica que atende banco, compliance importa. Então
desenhei uma **interface `ICandidateSource`** (contrato de código: quem quiser ser fonte precisa saber
'buscar'; de onde vem é detalhe): o GitHub é a primeira fonte; LinkedIn, se um dia houver acesso
legítimo, pluga sem tocar no motor."
**Clique nos links:** "São perfis reais — André Baltieri, Eduardo Pires, Renato Groffe: referências .NET
no Brasil."

> **⭐ Se perguntarem "como funciona a régua?" — a resposta que separa você:** *"São **duas** réguas.
> A de **sourcing** decide quem o GitHub me devolve: mapeio a skill da demanda pra uma linguagem do
> GitHub — `.NET` não existe lá, então busco `C#` — filtro `type:user` pra excluir organizações,
> `repos:>5` pra cortar conta fantasma, localização opcional, ordenado por seguidores. A de **ranking**
> é o **mesmo `MatchingService`** dos consultores internos: o candidato externo é projetado num
> Consultant transitório e passa pelo mesmo score. **Se eu tivesse duas réguas de pontuação, teria duas
> verdades.**"*

> **Se comentarem do número de resultados:** *"É configurável porque tem trade-off real: cada candidato
> custa **2 chamadas extras** — perfil e repositórios, porque infiro skill do que a pessoa publica.
> 15 candidatos = 31 chamadas. Com token são 5000/h; sem token, 60/h. E o teto de 15 protege também o
> limite **secundário** do GitHub, o de concorrência — que não aparece na cota e é o que morde quando
> você paraleliza."*

### Passo 6 — Importar para o banco AO VIVO (1min — fecha o ciclo)
**Clique:** botão **Import** num candidato → depois **busque de novo** → ele volta marcado
**"On the bench"** → depois Consultants no menu.
**Diga:** "Um clique e o dev real virou um consultor **persistido no banco** — e olhem o ranking
interno: ele já aparece pontuado. Buscando de novo, ele volta **marcado como já na bench**, sem botão de
import: a busca pergunta ao servidor quem já existe. O import é **idempotente** (clicar duas vezes não
duplica, devolve o mesmo registro) e **auditado**."

> **A pergunta cascata provável — "como você sabe que é a mesma pessoa?"** (é ouro, prepare):
> *"Não existe e-mail corporativo de um candidato do GitHub, então derivo uma **identidade sintética**
> estável do id externo: `github:andrebaltieri` vira `andrebaltieri@github.import`. É isso que torna o
> import idempotente E permite a busca marcar quem já está na bench. E essa regra mora **no Core, num
> lugar só** — porque se o import e a busca discordassem do formato, a mesma pessoa entraria duas vezes."*
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

### 3.3.1 As DUAS réguas do sourcing (memorize esta separação)

Quando alguém perguntar "como você traz as pessoas?", a resposta forte é que **são duas réguas
distintas** — e a segunda é compartilhada:

| | **Régua de SOURCING** (quem vem) | **Régua de RANKING** (quanto vale) |
|---|---|---|
| Onde vive | `GitHubCandidateSource` (Infrastructure) | `MatchingService` (Core) |
| O que faz | monta a query do GitHub | dá o score |
| Critérios | `language:C#` (mapeado de `.NET`), `type:user`, `repos:>5`, `location`, ordenado por seguidores | +50 disponível, +10/skill, +20 senioridade, −30 alocado |
| É compartilhada? | não — é específica da fonte | **SIM — a mesma dos consultores internos** |

**O ponto:** o candidato externo é projetado num `Consultant` transitório (`ToConsultant()`) e passa
pela **mesma** régua de pontuação. *"Se eu tivesse duas réguas de score, teria duas verdades."*

**De onde saem as skills dele:** das **linguagens dos repositórios não-fork** + tópicos reconhecidos —
com um mapa de sinônimos, porque o GitHub não tem "linguagem .NET", tem `C#`. E a senioridade sai de
uma **heurística determinística** (idade da conta, nº de repos, seguidores).

### 3.4 Segurança (o essencial pra conversar)

- **JWT**: crachá assinado. O servidor emite no login e valida a assinatura a cada chamada.
- **BCrypt**: hash de senha lento de propósito (dificulta força bruta).
- **OAuth (Google)**: o usuário loga NO GOOGLE; o Google me dá um **ID token** (JWT assinado pelo
  Google dizendo "este é fulano@gmail"); eu valido a assinatura E o **audience** (que o token foi
  emitido para o MEU app, não para outro qualquer) e então emito o meu próprio crachá. Conta criada
  via Google fica **sem senha** — e o login por senha bloqueia hash vazio (detalhe fino: nunca passar
  hash vazio pro BCrypt).
  - **Por que NÃO uso client secret:** existem dois fluxos OAuth. O de *authorization code* troca um
    código por token no servidor — esse precisa de secret. O que usei é o de **ID token**: o Google já
    entrega o token assinado ao navegador e eu só **valido** no backend (assinatura + audience). Menos
    segredo para guardar, vazar ou rotacionar.
  - **Allowlist por e-mail:** sem ela o fluxo seria **fail-open** — qualquer conta Google do mundo
    viraria Admin.
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

**"A inferência de skill do GitHub não erra?"** *(erra — e assumir isso é o ponto)*
→ "Erra pra menos. O Giovanni Bassi é referência .NET e sai com skill vazia, porque os repos recentes
não-fork dele são Rust e Nix. É deliberado: **não confio no que a pessoa diz, confio no que ela
publica** — mas é uma limitação real. A evolução seria ponderar por estrelas e histórico, não só a
linguagem do repo recente."

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

**A do 'já importado' (a melhor de causa raiz — conte essa se der):**
> "Essa eu achei **usando o próprio produto**: importei um dev e ele continuava aparecendo na busca com
> botão de import, como se nada tivesse acontecido. O sintoma era visual, mas a causa era de
> arquitetura: o estado 'já importado' vivia **na memória do componente** — ou seja, virava mentira
> depois de um reload. A verdade morava no servidor e a tela não perguntava.
> E consertando, vi o risco maior: a regra da identidade sintética existia **só dentro do import**. Se a
> busca inventasse a dela, a mesma pessoa entraria duas vezes. Extraí pro Core — uma regra, um lugar. E
> resolvi o lote numa **query só** (`WHERE Email IN (...)`), porque marcar N candidatos com N consultas
> seria trocar um bug por um gargalo."

**A do loader (mostra que você pensa em quem usa):**
> "Cliquei em 'Buscar no GitHub' e não sabia se tinha funcionado. A barra de progresso existia — mas
> embaixo, fora de onde o olho estava. Movi o feedback pra **dentro do botão** e escrevi o que está
> acontecendo: 'consultando a API do GitHub, lendo perfis e repositórios'. A busca continua levando os
> mesmos segundos, mas **espera explicada não é espera ansiosa**. Ferramenta interna também tem usuário."

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
| Inferência de skill só por linguagem de repo recente | Simples e explicável | Ponderar estrelas, histórico e tópicos |
| Só 1 fonte externa (GitHub) | Prova a abstração `ICandidateSource` | Stack Overflow / LinkedIn (se houver acesso legítimo) |

*(Fala pronta: "é um MVP com cortes CONSCIENTES e documentados — a spec diz o que ficou de fora e
por quê. Prefiro núcleo sólido e honesto a superfície grande e frágil.")*

---

## 7. OAuth Google — ✅ ATIVO e testado

**Funcionando de verdade:** você logou com `guimvolpe@gmail.com` e o backend criou sua conta no Postgres
(o `INSERT INTO "Users"` está no log). Configuração ativa no ambiente da demo:
- `GOOGLE_CLIENT_ID` = o client `allocationhub-web` (origins `localhost:4200` e `localhost:8080`)
- `GOOGLE_ALLOWED_EMAILS` = `guimvolpe@gmail.com` → **só você entra** pelo Google

**Prova visual opcional:** Supabase → Table Editor → tabela `Users` → sua conta aparece com
`PasswordHash` **vazio** (é a conta Google-only).

> **Se der erro na hora** (ex.: "origin is not allowed"): não insista ao vivo. Diga *"o OAuth exige
> registro de origem no console do Google e propagação; em ambiente real isso já está no deploy"* e
> **entre com a senha** (`admin@demo.com` / `admin123`). O código continua sendo seu argumento.

⚠️ **Depois da entrevista:** delete a **client secret** (não é usada por nada aqui), **resete a senha do
banco** no Supabase e **revogue a chave da Groq** e o **token do GitHub** — todos passaram por chat.

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
  O GitHub tem também um limite **secundário**, de concorrência, que não aparece na cota.
- **Idempotente** — repetir a operação não muda o resultado (2º import devolve o mesmo registro).
- **Identidade sintética** — e-mail derivado do id externo (`github:fulano` → `fulano@github.import`);
  é o que torna o import idempotente e permite marcar quem já está na bench.
- **N+1** — clássico de performance: buscar N itens e fazer 1 consulta para cada. Aqui a marcação de
  "já importado" resolve o lote em **uma** query (`WHERE Email IN (...)`).
- **Fail-open / fail-closed** — quando algo falha, o sistema **libera** (fail-open, perigoso) ou
  **bloqueia** (fail-closed, seguro). A allowlist do Google evita o fail-open.
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

- [ ] API de pé (`http://localhost:5080/api/health` mostrando **PostgreSQL (Supabase)** e **8 consultores**).
- [ ] Front de pé (`:4200`), favicon roxo na aba, visual Lyncas conferido.
- [ ] Abas prontas: **app** · **Swagger** (`:5080/swagger`) · **GitHub** (repo + Actions verde) ·
      **Supabase Table Editor** · **este guia**.
- [ ] Ambiente da API tem: `GROQ_API_KEY` · `GITHUB_TOKEN` (5000/h) · `GOOGLE_CLIENT_ID` ·
      `GOOGLE_ALLOWED_EMAILS` · `SUPABASE_DB_CONNECTION`. *(Se reiniciar a API, tem que subir com TODAS —
      senão o botão do Google some e a busca cai pra 60/h.)*
- [ ] Ensaiar 1x: **Passo 4** (troca de LLM) e **Passo 6** (import → buscar de novo → "On the bench").
- [ ] Banco limpo: 8 consultores semeados, 0 importados (o import ao vivo é a primeira vez).
- [ ] Respirar: **tudo nesta demo é real e você construiu.** Falha de rede vira demonstração de
      fallback — você não tem como perder.

### Se precisar reiniciar a API na correria
Peça pro Claude, ou rode com todas as variáveis de ambiente de uma vez (elas estão no seu histórico
deste chat — nunca em arquivo do repo, por decisão de segurança).

---

## 10. Os 3 fatos que você NÃO pode esquecer

1. **A regra de match é pura, determinística e testada — a IA nunca decide.** Se você só disser uma
   coisa técnica, diga essa.
2. **Tudo é real:** devs reais do GitHub, LLM real trocável, Postgres real, OAuth real, CI real.
   Nenhum mock onde dava pra fazer de verdade.
3. **Você assume o que falta** (migrations, RBAC, refresh token, testes de integração) e sabe o próximo
   passo de cada um. Sênior não é quem não tem lacuna — é quem sabe onde elas estão e por quê.
