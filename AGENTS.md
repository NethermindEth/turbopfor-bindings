# AGENTS instructions

C# bindings for TurboPFor. See [global.json](./global.json) and [src](./src/) directory for the project requirements and configuration.

## Project structure

- [src](./src/): The main codebase. The P/Invoke declarations mirror TurboPFor's [ic.h](https://github.com/powturbo/TurboPFor-Integer-Compression/blob/da4fa61f6dedee61a3c0c624fc8656d84b820392/include/ic.h) C API.
- [makefile.patch](./makefile.patch): Adds the macOS and Windows shared library targets to TurboPFor's makefile.
- [build-turbopfor.yml](./.github/workflows/build-turbopfor.yml): Builds TurboPFor for the specified ref and optionally opens a pull request with the resulting binaries.
- [test-publish.yml](./.github/workflows/test-publish.yml): Runs the tests and optionally publishes on NuGet.

## Coding guidelines

- Follow [.editorconfig](./.editorconfig).
- Do not assume; measure, research, ask if unsure.
- Keep comments short and to the point.
- Add tests for new code and bug fixes.
- Use conventional commits; keep scoped and imperative.
- Keep the native binaries under `src/Nethermind.TurboPForBindings/runtimes/` in sync with a single TurboPFor revision (currently [da4fa61](https://github.com/powturbo/TurboPFor-Integer-Compression/tree/da4fa61f6dedee61a3c0c624fc8656d84b820392)); they are Git LFS objects built only by [build-turbopfor.yml](./.github/workflows/build-turbopfor.yml), so do not edit or rebuild them locally.
- Keep the P/Invoke signatures in sync with the TurboPFor headers of the shipped binaries.
- Prefer the latest versions of GitHub Actions and runners.
- Update [THIRD-PARTY-NOTICES](./THIRD-PARTY-NOTICES) when introducing a dependency if needed.
- Keep [AGENTS.md](./AGENTS.md) in sync with the ongoing development.
