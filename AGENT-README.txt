================================================================================

    AGENT-README: CodeBrix.AssemblyTools
    A Comprehensive Guide for AI Coding Agents

================================================================================


OVERVIEW
--------------------------------------------------------------------------------

CodeBrix.AssemblyTools is a .NET-10 port of Mono.Cecil 0.11.6. It gives you
full programmatic read / write / rewrite access to managed .NET assemblies:
modules, types, methods, fields, properties, events, custom attributes, IL,
and debug symbols (portable PDB, native PDB, and Mono MDB). The public API
surface matches Mono.Cecil 0.11.6 essentially one-to-one --- only the
namespace prefix has changed (from `Mono.Cecil.*` to `CodeBrix.AssemblyTools.*`).

The design intent is "drop-in replacement": a consumer project that used
Mono.Cecil 0.11.6 NuGet can replace that dependency with CodeBrix.AssemblyTools
and, after a find/replace of `using Mono.Cecil` -> `using CodeBrix.AssemblyTools`
(and sibling namespaces), the consumer code compiles and behaves identically.

CodeBrix.AssemblyTools is a single merged assembly that contains everything
Mono.Cecil 0.11.6 shipped across its four library projects:
  * Mono.Cecil      -> the core reader / writer / object model
  * Mono.Cecil.Rocks -> extension-method helpers
  * Mono.Cecil.Pdb  -> PDB debug-symbol reader / writer (portable + native)
  * Mono.Cecil.Mdb  -> Mono MDB debug-symbol reader / writer

The port was originally taken from the Mono.Cecil 0.11.5 tag and brought up
to 0.11.6 by cherry-picking every source-level commit between the two tags.
Notable 0.11.6 bug / correctness fixes that this port carries:

  * WriteCompressedInt32 negative-value encoding correctness
    (Mono.Cecil commit 608fac6; was a silent metadata-corruption bug on 0.11.5
     for marshal descriptors and custom attributes with negative constants).
  * BaseAssemblyResolver now tries `.dll` before `.exe`, so dependency
    resolution against modern .NET-published apps no longer fails with
    BadImageFormatException when the unmanaged `Foo.exe` apphost sits next
    to the managed `Foo.dll` (commit 8e1ae7b).
  * MetadataResolver now returns MethodDefinition fast-paths directly, which
    also fixes the privatescope method-resolution bug for generic instance
    methods (commit 56d4409).
  * Import (via System.Reflection) now preserves required / optional custom
    modifiers --- `in`, `ref readonly`, `init`-only, `required`, `unmanaged`
    constraint, etc. --- instead of silently dropping them (commit fec4ee9).
  * New AllowByRefLikeConstraint flag (0x0020) on GenericParameterAttributes
    plus a matching `AllowByRefLikeConstraint` property on GenericParameter
    (C# 13 `where T : allows ref struct` --- commit 1da2145).
  * Strong-name SHA1 hashing uses `SHA1.Create()`, which is FIPS-compliant
    on hosts with `FIPSAlgorithmPolicy = 1` (commit dff01d9; upstream used
    `SHA1CryptoServiceProvider` but that is obsolete on .NET 6+).
  * SymWriter COM object is deterministically released even when
    NativePdbWriter.Write is never called due to an exception (commit 50292e7;
    fixes intermittent PDB file-locking on Windows).
  * TypeDefinition now implements ICustomDebugInformationProvider; type-level
    PDB debug metadata (e.g. NullableContext entries emitted by MSBuild on
    generated types) survives round-trips instead of being silently dropped
    (commit a0f61f9).

Instruction also gained a new `GetPrototype()` public API for IL-rewriter
code that wants to clone an instruction (commit 04286ac).


INSTALLATION
--------------------------------------------------------------------------------

NuGet package:
    CodeBrix.AssemblyTools.MitLicenseForever

dotnet CLI:
    dotnet add package CodeBrix.AssemblyTools.MitLicenseForever

Target framework:   .NET 10.0 or higher
Package license:    MIT


KEY NAMESPACES
--------------------------------------------------------------------------------

    using CodeBrix.AssemblyTools;                     // root API (upstream Mono.Cecil)
    using CodeBrix.AssemblyTools.Cil;                 // IL / method-body types
    using CodeBrix.AssemblyTools.Metadata;            // metadata-heap / token types
    using CodeBrix.AssemblyTools.PE;                  // portable-executable layer
    using CodeBrix.AssemblyTools.Collections.Generic; // Mono.Collections.Generic
    using CodeBrix.AssemblyTools.Rocks;               // extension-method rocks
    using CodeBrix.AssemblyTools.Pdb;                 // PDB provider facade
    using CodeBrix.AssemblyTools.Mdb;                 // MDB provider facade


CORE API REFERENCE
--------------------------------------------------------------------------------

The API is Mono.Cecil 0.11.6, renamespaced. The main entry points are:

  AssemblyDefinition
      Static factory: AssemblyDefinition.ReadAssembly(path [, parameters])
      Instance API:   Name, MainModule, Modules, CustomAttributes,
                      SecurityDeclarations, Write(stream [, parameters]),
                      Dispose()

  ModuleDefinition
      Static factory: ModuleDefinition.ReadModule(path [, parameters])
                      ModuleDefinition.CreateModule(name, ModuleKind)
      Instance API:   Types, GetTypes(), GetAllTypes() (rocks), Assembly,
                      ImportReference(...), Write(stream [, parameters]),
                      SymbolReader / SymbolWriter, Dispose()

  TypeDefinition / TypeReference
      Name, Namespace, FullName, Module, Scope, BaseType, Interfaces,
      Fields, Methods, Properties, Events, NestedTypes, GenericParameters,
      Resolve()

  MethodDefinition / MethodReference
      Name, Parameters, ReturnType, Body (MethodBody with ILProcessor),
      Overrides, GenericParameters, Resolve()

  ILProcessor (from a MethodBody)
      Emit(OpCode [, operand]), Append(instruction), Insert{Before,After}(...),
      Replace(...), Remove(...), Create(OpCode [, operand]).

Read parameters, write parameters, assembly resolvers (BaseAssemblyResolver,
DefaultAssemblyResolver), symbol providers (ISymbolReaderProvider, Portable
PDB provider, native-PDB provider, MDB provider), and metadata resolvers
are all available --- see the type definitions under src/CodeBrix.AssemblyTools/
for the full surface.


CODING CONVENTIONS (CodeBrix family)
--------------------------------------------------------------------------------

When editing this codebase, follow the CodeBrix family conventions:

  * Target framework: net10.0 only. No multi-targeting.
  * Nullable reference types are OFF library-wide. DO NOT use `?` on
    reference types (e.g. `string?`, `MyClass?`), and DO NOT use the
    null-forgiveness `!` operator. Value-type nullables (`int?`,
    `DateOnly?`, enum `?`) remain fine.
  * ImplicitUsings is OFF. No `global using` directives. All usings are
    written at the top of each file.
  * File-scoped namespaces only. No block-scoped `namespace X { ... }`.
  * Every ported .cs file preserves its upstream copyright header
    verbatim, and its `namespace` line carries a `//was previously:`
    provenance comment identifying the upstream namespace.
  * XML documentation generation is currently DISABLED
    (<GenerateDocumentationFile>false</GenerateDocumentationFile>) on
    this project. This is a deliberate temporary relaxation of the CodeBrix
    convention --- the port started from ~400 Mono.Cecil source files with
    sparse XML doc comments; turning doc generation on would require
    writing hundreds of new summaries. A follow-up audit will revisit this.
  * AllowUnsafeBlocks is ON (Cecil's IL / PE reading uses unsafe pointer
    arithmetic in several spots).
  * No project-level warning suppression (`<NoWarn>`, `<WarningLevel>0</>`,
    `<TreatWarningsAsErrors>false</>`). Warnings should be fixed at source.

Test project conventions:

  * xUnit v3 + SilverAssertions.ApacheLicenseForever.
  * Every call that accepts a `CancellationToken` inside a test passes
    `TestContext.Current.CancellationToken`.
  * Test classes named `<ClassUnderTest>Tests` (helper / scenario files
    may skip the `Tests` suffix).
  * Test method names use either `<Member>_snake_case_description` or
    pure `snake_case`.
  * Assertions are a mix of SilverAssertions fluent (`.Should().Be(...)`)
    and xUnit `Assert.Equal` --- fluent when a test was substantively
    rewritten, mechanical xUnit when the port was close to one-for-one.


ARCHITECTURE
--------------------------------------------------------------------------------

Source layout (src/CodeBrix.AssemblyTools/):

    Internal/             -- ns CodeBrix.AssemblyTools.Internal
                             (upstream: Mono/) Disposable, Empty,
                             MergeSort --- implementation helpers.
    Cecil/                -- ns CodeBrix.AssemblyTools
                             (upstream: Mono.Cecil/) the main object model:
                             AssemblyDefinition, ModuleDefinition,
                             TypeDefinition, MethodDefinition,
                             AssemblyReader, AssemblyWriter, etc.
    Cil/                  -- ns CodeBrix.AssemblyTools.Cil
                             (upstream: Mono.Cecil.Cil/) IL types,
                             MethodBody, ILProcessor, Document,
                             SequencePoint, Symbols.
    Metadata/             -- ns CodeBrix.AssemblyTools.Metadata
                             (upstream: Mono.Cecil.Metadata/) heap / table /
                             token types.
    PE/                   -- ns CodeBrix.AssemblyTools.PE
                             (upstream: Mono.Cecil.PE/) portable-executable
                             file-format layer.
    Collections/Generic/  -- ns CodeBrix.AssemblyTools.Collections.Generic
                             (upstream: Mono.Collections.Generic/) small
                             observable-Collection helpers.
    Security/Cryptography/ -- ns CodeBrix.AssemblyTools.Security.Cryptography
                             (upstream: Mono.Security.Cryptography/)
                             strong-name hashing helpers.
    Rocks/                -- ns CodeBrix.AssemblyTools.Rocks
                             (upstream: rocks/Mono.Cecil.Rocks/) extension
                             methods: GetAllTypes, SimplifyMacros,
                             OptimizeMacros, DocCommentId, ILParser, etc.
    Pdb/                  -- ns CodeBrix.AssemblyTools.Pdb
                             (upstream: symbols/pdb/Mono.Cecil.Pdb/) PDB
                             symbol provider facade.
    Pdb.Cci/              -- ns CodeBrix.AssemblyTools.Pdb.Cci
                             (upstream: symbols/pdb/Microsoft.Cci.Pdb/)
                             Microsoft CCI PDB reader / writer, internal.
    Mdb/                  -- ns CodeBrix.AssemblyTools.Mdb
                             (upstream: symbols/mdb/Mono.Cecil.Mdb/) MDB
                             symbol provider facade.
    Mdb.SymbolWriter/     -- ns CodeBrix.AssemblyTools.Mdb.SymbolWriter
                             (upstream:
                             symbols/mdb/Mono.CompilerServices.SymbolWriter/)
                             Mono's symbol-writer implementation.

    InternalsVisibleTo.cs -- grants InternalsVisibleTo to
                             CodeBrix.AssemblyTools.Tests.


TESTING
--------------------------------------------------------------------------------

Test project: tests/CodeBrix.AssemblyTools.Tests/
Framework:    xUnit v3 (upstream was NUnit 3.11.0)
Assertions:   mix of SilverAssertions fluent + xUnit Assert
Resources:    tests/CodeBrix.AssemblyTools.Tests/Resources/ --- pre-built
              binary fixtures (.dll / .exe / .pdb / .mdb / .cs / .il)
              copied verbatim from Mono.Cecil 0.11.6, MIT-licensed.

              IMPORTANT: many symbol-loading tests (PortablePdbTests,
              PdbTests, SymbolTests, parts of ImageReadTests) require
              `.pdb` sidecar files (e.g. libpdb.pdb, PdbTarget.pdb,
              ComplexPdb.pdb, mylib.pdb, ...) to sit next to their
              corresponding .dll/.exe under Resources/assemblies/. The
              standard VisualStudio.gitignore template ignores `*.pdb`
              globally, so the .gitignore in this repo carries an
              explicit un-ignore exception:

                  !tests/**/Resources/**/*.pdb
                  !tests/**/Resources/**/*.mdb

              Do NOT remove those exceptions, and do NOT generalize the
              `*.pdb` ignore in a way that re-shadows them. If a clone
              of this repo is missing those sidecars, ~50 symbol-related
              tests will fail with `SymbolsNotFoundException` /
              `FileNotFoundException` for the missing `.pdb` path; the
              fix is to restore the fixtures, not to change library code
              or skip the tests per-platform.

To run the full suite:

    dotnet test CodeBrix.AssemblyTools.slnx

To run a single test class:

    dotnet test CodeBrix.AssemblyTools.slnx --filter "FullyQualifiedName~ModuleTests"


================================================================================
