# Release Process

## Overview

ParseZero uses GitHub Actions to automate builds, testing, and releases. The CI/CD pipeline is configured to:

- **Build & Test**: Run on every push to `main`/`develop` and on PRs targeting `main`/`develop`
- **Pack**: Create NuGet packages on push or release
- **Publish**: Push packages to NuGet.org on release

## Release Workflow

### 1. Create a GitHub Release

Navigate to the repository's [Releases](https://github.com/jdtoon/ParseZero/releases) page and:

1. Click "Draft a new release"
2. Set the tag name (e.g., `v1.0.0`, `v1.1.0-beta.1`)
3. Set the title (e.g., "ParseZero 1.0.0")
4. Add release notes describing features, bug fixes, breaking changes
5. Click "Publish release"

### 2. Automatic Versioning

The GitHub Actions workflow automatically:

1. **Detects** the release tag (e.g., `v1.0.0`)
2. **Strips** the leading 'v' to create the version (e.g., `1.0.0`)
3. **Packs** the NuGet package with that version
4. **Publishes** to NuGet.org

### 3. Prerequisites

For the publish step to succeed, ensure:

- **NUGET_API_KEY** secret is configured in the GitHub repository settings
  - Value: Your NuGet API key (from https://www.nuget.org/account/apikeys)
  - Scope: `Push version 1.0.0 and later` (recommended)
- **Permissions**: The repository has enabled the `nuget` environment with appropriate restrictions

### 4. Verification

After publishing:

1. Check NuGet.org: https://www.nuget.org/packages/ParseZero/
2. Verify package metadata (version, dependencies, readme)
3. Test installation: `dotnet add package ParseZero --version 1.0.0`

## CI Pipeline Details

### Build Job
- Runs on: `ubuntu-latest`, `windows-latest`, `macos-latest`
- Tests: `.NET 8` (all platforms), `.NET Framework 4.7.2` (Windows only)
- Artifacts: Test results (7-day retention)

### Pack Job
- Runs only on `push` or `release`
- Version source:
  - Release: `${{ github.event.release.tag_name }}`
  - Push: `1.0.0-ci.${{ github.run_number }}`
- Artifacts: NuGet package (30-day retention)

### Publish Job
- Runs only on `release` events
- Requires `nuget` environment approval (optional)
- Pushes to: `https://api.nuget.org/v3/index.json`
- Skips duplicates automatically

### Benchmarks Job
- Runs on successful `push` to `main` only
- Generates BenchmarkDotNet results in JSON format
- Artifacts: Results (30-day retention)

## Branch Strategy

- **main**: Stable releases; PRs trigger full test suite
- **develop**: Integration branch; PRs trigger full test suite
- **feature/***: Feature branches; create PR to develop

## Semantic Versioning

Use [Semantic Versioning](https://semver.org/) for release tags:

- **Major** (breaking changes): `v2.0.0`, `v3.0.0`
- **Minor** (new features): `v1.1.0`, `v1.2.0`
- **Patch** (bug fixes): `v1.0.1`, `v1.0.2`
- **Pre-release**: `v1.0.0-beta.1`, `v1.0.0-rc.1`

## Troubleshooting

### Package not published
- Check workflow run logs: Actions tab → Latest run → Publish job
- Verify NUGET_API_KEY is set correctly
- Confirm tag format (e.g., `v1.0.0`)

### Wrong version in package
- Verify the GitHub release tag name
- The version will be: tag minus leading 'v'
- Example: Tag `v1.0.0` → Package version `1.0.0`

### Duplicate push error
- Normal—workflow has `--skip-duplicate` enabled
- Package already on NuGet; re-run publishes old version
