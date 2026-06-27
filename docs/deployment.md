# Api.Ebay Deployment Runbook

`Api.Ebay` is a package library. Its deployable artifact is the NuGet package produced from `src/PolyhydraGames.API.Ebay/PolyhydraGames.API.Ebay.csproj`; it does not own a long-running service or Portainer stack.

## Canonical Deploy Target

- Artifact: `Polyhydragames.API.Ebay.*.nupkg`
- Destination: GitHub Packages for `lancer1977/Api.Ebay`
- Automation: `.github/workflows/publish.yml`
- Manual trigger: GitHub Actions `publish` workflow dispatch
- CI artifact check: `.github/workflows/ci.yml`

The generic files under `deploy/` are reusable templates only. They are not the production deployment system for this package.

## Required Configuration

- `GITHUB_TOKEN`: provided by GitHub Actions for package publication.
- `GHCR_TOKEN`: used by CI restore when private GitHub package feeds are required.
- Package version: currently supplied by the publish workflow through `-p:PackageVersion=1.0.0.16`; update this intentionally when releasing.

No eBay client id, client secret, RuName, refresh token, or production credential is required for the package deploy. OAuth tests use local fake credentials and captured HTTP handlers.

## Release Procedure

1. Validate locally:

   ```bash
   dotnet restore Api.Ebay.sln
   dotnet test Api.Ebay.sln --no-restore
   bash scripts/smoke.sh --no-restore
   dotnet pack src/PolyhydraGames.API.Ebay/PolyhydraGames.API.Ebay.csproj --configuration Release --no-restore --output ./artifacts/package
   ```

2. Push to `main`.
3. Confirm the `ci` workflow uploads `api-ebay-packages`.
4. Run the `publish` workflow when the package version is ready to publish.
5. Confirm GitHub Packages contains the expected package version.

## Smoke Gate

Use:

```bash
bash scripts/smoke.sh
```

The smoke gate proves:

- sandbox consent URL generation is stable
- authorization-code token exchange sends the expected request shape
- refresh-token exchange sends the expected request shape
- the package project can produce a `.nupkg` artifact

## Rollback

NuGet packages are immutable from a consumer perspective. To roll back, pin consumers to the previous known-good package version. If a bad package was published, mark it deprecated in GitHub Packages where supported and publish a corrected higher version.
