#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

restore=true
if [[ "${1:-}" == "--no-restore" ]]; then
  restore=false
fi

if [[ "$restore" == true ]]; then
  dotnet restore Api.Ebay.sln
fi

dotnet test tests/PolyhydraGames.API.Ebay.Tests/PolyhydraGames.API.Ebay.Tests.csproj \
  --no-restore \
  --filter "FullyQualifiedName~ConsentUrl_UsesSandboxAuthorizeEndpointAndEncodesQuery|FullyQualifiedName~ExchangeCodeForTokensAsync_SendsAuthorizationCodeGrantRequest|FullyQualifiedName~RefreshAccessTokenAsync_SendsRefreshGrantRequest" \
  --verbosity minimal

smoke_dir="${TMPDIR:-/tmp}/api-ebay-smoke"
rm -rf "$smoke_dir"
mkdir -p "$smoke_dir"

dotnet pack src/PolyhydraGames.API.Ebay/PolyhydraGames.API.Ebay.csproj \
  --configuration Release \
  --no-restore \
  --output "$smoke_dir" \
  --verbosity minimal

test -n "$(find "$smoke_dir" -maxdepth 1 -name '*.nupkg' -print -quit)"
echo "Api.Ebay smoke passed: OAuth contract tests and package artifact are valid."
