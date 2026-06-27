# Api.Ebay Project Atlas

## Purpose

`Api.Ebay` is a minimal .NET OAuth client for eBay authorization-code, refresh-token, and application-token flows.

## Primary Surfaces

- Library project: `src/PolyhydraGames.API.Ebay/`.
- Contract tests: `tests/PolyhydraGames.API.Ebay.Tests/`.
- Package publish workflow: `.github/workflows/publish.yml`.
- CI package artifact workflow: `.github/workflows/ci.yml`.
- Deploy runbook: `docs/deployment.md`.
- Smoke gate: `scripts/smoke.sh`.

## Validation

```bash
dotnet restore Api.Ebay.sln
dotnet test Api.Ebay.sln --no-restore
bash scripts/smoke.sh --no-restore
dotnet pack src/PolyhydraGames.API.Ebay/PolyhydraGames.API.Ebay.csproj --configuration Release --no-restore --output ./artifacts/package
dotnet list Api.Ebay.sln package --outdated
devstudio validate --repo .
```
