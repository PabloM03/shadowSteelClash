#!/usr/bin/env bash
set -euo pipefail

# —————————— CONFIGURACIÓN ——————————
: "${GITHUB_TOKEN:?Tienes que definir GITHUB_TOKEN en las Env Vars de Unity Cloud Build}"
: "${REPO_OWNER:?Tienes que definir REPO_OWNER en las Env Vars de Unity Cloud Build}"
: "${REPO_NAME:?Tienes que definir REPO_NAME en las Env Vars de Unity Cloud Build}"

echo "� Disparando evento unity-build-complete en ${REPO_OWNER}/${REPO_NAME}…"

curl -sS --fail \
  -X POST \
  -H "Accept: application/vnd.github+json" \
  -H "Content-Type: application/json" \
  -H "Authorization: token ${GITHUB_TOKEN}" \
  "https://api.github.com/repos/${REPO_OWNER}/${REPO_NAME}/dispatches" \
  -d '{"event_type":"unity-build-complete"}'

echo "✅ Evento enviado correctamente."
