# CodeBrix.AssemblyTools

A fully managed, cross-platform .NET-assembly reading / writing / rewriting library for .NET. CodeBrix.AssemblyTools is a .NET-10 port of Mono.Cecil 0.11.6 — the same public API surface (renamed into the `CodeBrix.AssemblyTools.*` namespace), the same IL / metadata / PDB / MDB capabilities, and no NuGet dependencies beyond .NET itself.
CodeBrix.AssemblyTools has no dependencies other than .NET, and is provided as a .NET 10 library and associated `CodeBrix.AssemblyTools.MitLicenseForever` NuGet package.

CodeBrix.AssemblyTools supports applications and assemblies that target Microsoft .NET version 10.0 and later.
Microsoft .NET version 10.0 is a Long-Term Supported (LTS) version of .NET, and was released on Nov 11, 2025; and will be actively supported by Microsoft until Nov 14, 2028.
Please update your C#/.NET code and projects to the latest LTS version of Microsoft .NET.

## CodeBrix.AssemblyTools supports:

* Reading and writing managed assemblies (`AssemblyDefinition`, `ModuleDefinition`)
* Inspecting and modifying types, methods, fields, properties, events, and custom attributes
* Reading and emitting IL via `MethodBody` / `ILProcessor`
* Reading and writing portable PDB debug symbols (`CodeBrix.AssemblyTools.Cil`, `CodeBrix.AssemblyTools.Pdb`)
* Reading and writing native Windows PDB symbols (`CodeBrix.AssemblyTools.Pdb.Cci`)
* Reading and writing Mono MDB debug symbols (`CodeBrix.AssemblyTools.Mdb`)
* Extension-method "rocks" for common tasks: `GetAllTypes`, `SimplifyMacros`, `OptimizeMacros`, `SortInterfaces`, and more (`CodeBrix.AssemblyTools.Rocks`)

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

## License

The project is licensed under the MIT License. see: https://en.wikipedia.org/wiki/MIT_License
