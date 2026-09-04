# CodeBrix.AssemblyTools

A fully managed, cross-platform reader, writer and rewriter for .NET assemblies, covering IL, metadata and debug symbols (portable PDB, native Windows PDB, and Mono MDB) in a single assembly.
CodeBrix.AssemblyTools has no dependencies other than .NET, and is provided as a .NET 10 library and associated `CodeBrix.AssemblyTools.MitLicenseForever` NuGet package.

CodeBrix.AssemblyTools supports applications and assemblies that target Microsoft .NET version 10.0 and later.
Microsoft .NET version 10.0 is a Long-Term Supported (LTS) version of .NET, and was released on Nov 11, 2025; and will be actively supported by Microsoft until Nov 14, 2028.
Please update your C#/.NET code and projects to the latest LTS version of Microsoft .NET.

## Installation

```
dotnet add package CodeBrix.AssemblyTools.MitLicenseForever
```

Note that the NuGet package ID and the namespace are different - there is no package named plain `CodeBrix.AssemblyTools`:

* NuGet package ID: `CodeBrix.AssemblyTools.MitLicenseForever`
* Assembly and primary namespace: `CodeBrix.AssemblyTools` - i.e. `using CodeBrix.AssemblyTools;`

XML documentation (IntelliSense) ships alongside the assembly.

The package has no NuGet dependencies of its own; it builds only against the .NET base class libraries. The IL, symbol-reading and extension-method APIs live in companion namespaces - `CodeBrix.AssemblyTools.Cil`, `.Pdb`, `.Pdb.Cci`, `.Mdb` and `.Rocks` - all in this one assembly.

## CodeBrix.AssemblyTools supports:

* Reading and writing managed assemblies (`AssemblyDefinition`, `ModuleDefinition`)
* Inspecting and modifying types, methods, fields, properties, events, and custom attributes
* Reading and emitting IL via `MethodBody` / `ILProcessor`
* Reading and writing portable PDB debug symbols (`CodeBrix.AssemblyTools.Cil`, `CodeBrix.AssemblyTools.Pdb`)
* Reading and writing native Windows PDB symbols (`CodeBrix.AssemblyTools.Pdb.Cci`)
* Reading and writing Mono MDB debug symbols (`CodeBrix.AssemblyTools.Mdb`)
* Extension-method "rocks" for common tasks: `GetAllTypes`, `SimplifyMacros`, `OptimizeMacros`, `GetConstructors`, `MakeGenericInstanceType`, and more (`CodeBrix.AssemblyTools.Rocks`)

## Sample Code

### Open an assembly, rename it, and write it back out

```csharp
using CodeBrix.AssemblyTools;

using var assembly = AssemblyDefinition.ReadAssembly("Input.dll");
assembly.Name.Name = "Renamed";
assembly.Write("Output.dll");
```

### Walk every method in a module

```csharp
using CodeBrix.AssemblyTools;
using CodeBrix.AssemblyTools.Cil;

using var module = ModuleDefinition.ReadModule("Input.dll");
foreach (var type in module.Types)
    foreach (var method in type.Methods)
        if (method.HasBody)
            foreach (Instruction instruction in method.Body.Instructions)
                Console.WriteLine($"{method.FullName}: {instruction}");
```

## Documentation

The NuGet package includes `AGENT-README.txt`, a complete API reference and usage guide written for AI coding agents - point your agent at that file when it is writing code against this library.

Additional sample code and usage examples are available in the `CodeBrix.AssemblyTools.Tests` project:
https://github.com/ellisnet/CodeBrix.AssemblyTools/tree/main/tests/CodeBrix.AssemblyTools.Tests

## License

CodeBrix.AssemblyTools is licensed under the MIT License - see the
[LICENSE](https://github.com/ellisnet/CodeBrix.AssemblyTools/blob/main/LICENSE) file.

For licensing and provenance information about the open source code included in
this package, see [THIRD-PARTY-NOTICES.txt](https://github.com/ellisnet/CodeBrix.AssemblyTools/blob/main/THIRD-PARTY-NOTICES.txt).
