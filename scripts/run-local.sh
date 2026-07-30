#!/usr/bin/env bash
# run-local.sh — sobe a API na :5080 (e o front é `cd web && npm start`).
# Se existir scripts/local-secrets.sh (gitignored), carrega as chaves de lá:
#   export SUPABASE_DB_CONNECTION='...'   # banco Postgres/Supabase (senão: SQLite local)
#   export GROQ_API_KEY='gsk_...'         # LLM ao vivo no seletor (senão: só determinístico)
#   export GOOGLE_CLIENT_ID='....apps.googleusercontent.com'  # botão Google (senão: só senha)
#   export GITHUB_TOKEN='ghp_...'         # opcional: 60→5000 buscas/h no GitHub
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SECRETS="$ROOT/scripts/local-secrets.sh"
if [ -f "$SECRETS" ]; then . "$SECRETS"; echo "🔑 local-secrets.sh carregado"; else echo "⚠️ sem local-secrets.sh — subindo em modo SQLite/sem-LLM/sem-Google"; fi
cd "$ROOT/src/AllocationHub.Api"
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export ASPNETCORE_URLS="http://localhost:5080"
exec "$HOME/.dotnet/dotnet" run --no-launch-profile
