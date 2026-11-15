#!/usr/bin/env bash
# Points every app of an environment at a given image tag, restarts it and waits until the Gateway and web app are healthy.
# Also used for rollback: pass an older tag.
#
#   scripts/deploy/release.sh <resource-group> <tag>
#
# Requires: az (logged in), curl.
set -euo pipefail

RG="${1:?resource group}"
TAG="${2:?image tag}"

ACR_LOGIN="$(az acr list --resource-group "$RG" --query "[0].loginServer" -o tsv)"
[ -n "$ACR_LOGIN" ] || { echo "No container registry found in $RG." >&2; exit 1; }

echo "Releasing tag '$TAG' from $ACR_LOGIN"
GATEWAY_HOST=""
WEB_HOST=""
while read -r NAME SERVICE; do
  [ -n "$NAME" ] || continue
  echo "  $SERVICE -> $NAME"
  az webapp config set --resource-group "$RG" --name "$NAME" \
    --linux-fx-version "DOCKER|$ACR_LOGIN/edp-$SERVICE:$TAG" --output none
  az webapp restart --resource-group "$RG" --name "$NAME"
  HOST="$(az webapp show --resource-group "$RG" --name "$NAME" --query defaultHostName -o tsv)"
  [ "$SERVICE" = "gateway" ] && GATEWAY_HOST="$HOST"
  [ "$SERVICE" = "web" ] && WEB_HOST="$HOST"
done < <(az webapp list --resource-group "$RG" --query "[?tags.\"edp-service\"].[name, tags.\"edp-service\"]" -o tsv)

wait_for() {
  local url="$1" deadline=$((SECONDS + 600))
  until [ "$(curl -s -o /dev/null -w '%{http_code}' --max-time 10 "$url" || true)" = "200" ]; do
    [ "$SECONDS" -lt "$deadline" ] || { echo "Timed out waiting for $url" >&2; return 1; }
    sleep 10
  done
  echo "Healthy: $url"
}

[ -n "$GATEWAY_HOST" ] && wait_for "https://$GATEWAY_HOST/health/live"
[ -n "$WEB_HOST" ] && wait_for "https://$WEB_HOST/healthz"
echo "WEB_URL=https://$WEB_HOST"
