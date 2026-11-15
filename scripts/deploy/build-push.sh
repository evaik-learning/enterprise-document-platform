#!/usr/bin/env bash
# Builds one container image and pushes it to the environment's Azure Container Registry.
#
#   scripts/deploy/build-push.sh <resource-group> <service-key|web> <tag> <environment> [project assembly]
#
# Examples:
#   scripts/deploy/build-push.sh rg-edp-dev gateway 42 dev Gateway/Edp.Gateway/Edp.Gateway.csproj Edp.Gateway
#   scripts/deploy/build-push.sh rg-edp-dev web 42 dev
#
# Requires: az (logged in), docker. Run from the repository root. Pushes <tag> and "latest".
set -euo pipefail

RG="${1:?resource group}"
KEY="${2:?service key (gateway, identity, ... or web)}"
TAG="${3:?image tag}"
ENVIRONMENT="${4:?environment name}"

ACR_NAME="$(az acr list --resource-group "$RG" --query "[0].name" -o tsv)"
[ -n "$ACR_NAME" ] || { echo "No container registry found in $RG. Deploy the infrastructure first." >&2; exit 1; }
LOGIN_SERVER="$(az acr show --name "$ACR_NAME" --query loginServer -o tsv)"
IMAGE="$LOGIN_SERVER/edp-$KEY"

az acr login --name "$ACR_NAME" >/dev/null

if [ "$KEY" = "web" ]; then
  docker build \
    --build-arg "VITE_ENVIRONMENT=$ENVIRONMENT" \
    --build-arg "VITE_APP_VERSION=$TAG" \
    -t "$IMAGE:$TAG" -t "$IMAGE:latest" \
    src/Web/Edp.Web
else
  PROJECT="${5:?project path relative to src/}"
  ASSEMBLY="${6:?assembly name}"
  docker build -f docker/Dockerfile.service \
    --build-arg "PROJECT=src/$PROJECT" \
    --build-arg "ASSEMBLY=$ASSEMBLY" \
    -t "$IMAGE:$TAG" -t "$IMAGE:latest" \
    .
fi

docker push "$IMAGE:$TAG"
docker push "$IMAGE:latest"
echo "Pushed $IMAGE:$TAG"
