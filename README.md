# secp256r1-bindings

C# bindings for the [TurboPFor](https://github.com/powturbo/TurboPFor-Integer-Compression) integer compression library.

### Build

- Checkout the repository, including nested submodule.

- Copy `makefile` and from the `src` folder to `src/TurboPFor-Integer-Compression`, replacing existing file.

- Build the TurboPFor library using
  ```bash
  make libic.so
  ```
  on Linux/MacOS, or
  ```bash
  make ic.dll
  ```
  on Windows.

- Put the built `ic`/`libic` library from `src/TurboPFor-Integer-Compression` into the respective subdirectory in `src/Nethermind.TurboPFor/runtimes`, renaming it and replacing the existing `ic`/`libic` stub file

- Build the .NET project as follows:

  ```bash
  dotnet build src/Nethermind.TurboPFor
  ```
