set shell := ["pwsh", "-NoLogo", "-NoProfile", "-Command"]

# Show the available recipes.
default:
    @just --list; exit $LASTEXITCODE

# Restore solution dependencies, .NET tools, and npm tooling.
restore: _restore-solution
    dotnet tool restore; exit $LASTEXITCODE
    npm ci; exit $LASTEXITCODE

# Restore only solution dependencies for build and test iteration.
_restore-solution:
    dotnet restore Talby.Core.ResxAccess.slnx; exit $LASTEXITCODE

# Restore solution dependencies and build every project in Release.
build: _restore-solution
    dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore; exit $LASTEXITCODE

# Build, then run fast (default) or full tests with TRX validation.
test $mode="fast": build
    pwsh -NoProfile -File tests/run.ps1 -Mode $env:mode; exit $LASTEXITCODE

# Format code and Markdown using the tooling installed by restore.
format:
    npm run format; exit $LASTEXITCODE

# Check code and Markdown formatting without changing files.
format-check:
    npm run format:check; exit $LASTEXITCODE

# Build and pack a local archive; use release Prepare for release evidence.
pack: build
    dotnet pack src/Talby.Core.ResxAccess/Talby.Core.ResxAccess.csproj --configuration Release --no-build --no-restore --output artifacts/nuget; exit $LASTEXITCODE

# Inspect a release mode without executing stages or opening a browser.
release-plan $mode="Prepare":
    pwsh -NoProfile -File scripts/release.ps1 -Mode $env:mode -Plan; exit $LASTEXITCODE

# Run the release wizard: Prepare (default), Website, Cli, or Verify.
release $mode="Prepare":
    pwsh -NoProfile -File scripts/release.ps1 -Mode $env:mode; exit $LASTEXITCODE
