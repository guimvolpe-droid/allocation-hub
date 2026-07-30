# allocation-hub — Regras do repo

AllocationHub: gestor de alocação/staffing para software house — consultores, clientes, demandas
e **matching determinístico e explicável** (score transparente em `Core`, unit-testado sem banco).
Stack: Angular + Material (`web/`) · ASP.NET Core / .NET 8 · EF Core + SQLite · JWT · Clean
Architecture (`Api` → `Infrastructure` → `Core`). LLM só reescreve a explicação (Groq/OpenRouter/
OpenAI/Ollama via 1 cliente OpenAI-compatible), com guardrail + eval gate no CI — nunca no caminho
crítico. Peça de portfólio do carreira-os (contexto Lyncas: `docs/apresentacao-lyncas.md`).

## Comandos

- `docker compose up --build` → http://localhost:8080 (sistema inteiro).
- `scripts/run-local.sh` → API :5080 com secrets locais opcionais (gitignored).
- CI no GitHub Actions; testes .NET em `tests/`.

## Gates (só o dono decide)

- Publicar/expor o repo, mostrar em entrevista/cliente, e qualquer gasto (chave de LLM paga).
- Segredos NUNCA no git (`.env`, `scripts/local-secrets.sh` — já gitignored).

## Artefatos: 3 destinos <!-- origem: ~/projects/CLAUDE.md · v1 · copiado 2026-07-28 -->

- Arquivo gerado (screenshot, dump, export, peça em rascunho) NUNCA na raiz: lixo → `descarte/`
  (gitignored, só o dono apaga) · reutilizável fora de uso → `bkp/AAAA-MM-<slug>/` (gitignored,
  indexado em `bkp/LEIA-ME.md`) · versão FINAL → caminho canônico, nome estável (sem -v2/-final).
- MDs de estado guardam SÓ estado final (sem "era X virou Y"); contradição = corrigir na hora.
