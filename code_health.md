# code_health.md

- repo: Api.Ebay
- path: /home/lancer1977/code/Api.Ebay
- utc_timestamp: 2026-06-27T19:18:00Z
- scan_scope: README, deploy docs, workflows, tests, package metadata, dependency drift, DevStudio metadata
- last_pass_timestamp: 2026-06-27T19:18:00Z

## Validation

- `dotnet restore Api.Ebay.sln`
- `dotnet test Api.Ebay.sln --no-restore`
- `bash scripts/smoke.sh --no-restore`
- `dotnet build Api.Ebay.sln --no-restore`
- `dotnet pack src/PolyhydraGames.API.Ebay/PolyhydraGames.API.Ebay.csproj --configuration Release --no-restore --output ./artifacts/package`
- `dotnet list Api.Ebay.sln package --outdated`
- `devstudio validate --repo .`

## Findings

### Deployment lane — clarified
- The deployable artifact is the NuGet package, not a Portainer stack.
- `docs/deployment.md` names GitHub Packages as the deploy target and marks `deploy/` as reusable template material only.
- `scripts/smoke.sh` verifies OAuth request-shape contracts and package creation without live eBay credentials.

### CI/package health — improved
- CI now restores the solution, builds, tests, runs the smoke gate, packs, and uploads the package artifact.
- Publish now runs tests and smoke before pushing to GitHub Packages.

### Dependency health — checked
- Run `dotnet list Api.Ebay.sln package --outdated` before the next release slice.
- Keep project-local package versions until this repo starts sharing enough package drift with other API repos to justify central package management.

## Recommended Next Slice

Add a non-secret sandbox integration probe only after a stable eBay developer sandbox credential source is available in the deployment secret store.
