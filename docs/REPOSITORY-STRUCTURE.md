# Repository Structure

This repository follows a standard .NET library layout:

```text
.
├── src/
│   └── PolyhydraGames.API.Ebay/
├── tests/
│   └── PolyhydraGames.API.Ebay.Tests/
├── docs/
├── scripts/
├── Api.Ebay.sln
└── build.yml
```

## Conventions

- Keep production library code under `src/`.
- Keep automated tests under `tests/`.
- Keep repository/process documentation under `docs/`.
- Keep utility scripts/snippets under `scripts/`.

## Notes

- CI build pipeline points to `src/PolyhydraGames.API.Ebay/PolyhydraGames.API.Ebay.csproj`.
- Solution references both source and test projects using the new paths.
