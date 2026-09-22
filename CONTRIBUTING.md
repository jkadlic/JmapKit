# Contributing

Issues and pull requests are welcome.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) matching the version pinned in
  [`global.json`](global.json)

## Building and testing

```
dotnet restore JmapKit.sln
dotnet build JmapKit.sln
dotnet test JmapKit.sln
```

CI builds with `-warnaserror`, and `Directory.Build.props` enables the same analyzers and
`TreatWarningsAsErrors` locally, so a clean local build is a good signal your change will pass CI.

## Code style

Formatting and naming conventions are defined in [`.editorconfig`](.editorconfig) and enforced by
most IDEs automatically (Rider, Visual Studio, VS Code). You can also check formatting from the
command line:

```
dotnet format JmapKit.sln --verify-no-changes
```

## Versioning

Releases are tagged and versioned via [MinVer](https://github.com/adamralph/minver); you don't
need to bump a version number by hand. Add a note under `## Unreleased` in
[`CHANGELOG.md`](CHANGELOG.md) for any user-facing change.

## Pull requests

- Target `main` unless a maintainer asks otherwise.
- Make sure `dotnet build` and `dotnet test` pass locally before opening the PR.