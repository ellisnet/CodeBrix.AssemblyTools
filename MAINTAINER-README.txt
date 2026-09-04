================================================================================
MAINTAINER-README: CodeBrix.AssemblyTools
Notes for people and agents MAINTAINING this repository — not for package consumers
================================================================================

If you are CONSUMING the NuGet package, read AGENT-README.txt instead. This
file is about the repository itself: how it is laid out, how it builds, how
its tests run, how it is packed and published, and where its source came
from.


PURPOSE AND SCOPE
=================

This repository produces exactly ONE NuGet package from ONE project.

    Package id      CodeBrix.AssemblyTools.MitLicenseForever
    Assembly        CodeBrix.AssemblyTools.dll
    Project         src/CodeBrix.AssemblyTools/CodeBrix.AssemblyTools.csproj
    Consumer doc    AGENT-README.txt (repo root) -- covers this one package
    License         MIT (PackageLicenseExpression), LICENSE at the repo root

The library is a port of Mono.Cecil: full read / write / rewrite access to
managed .NET assemblies, IL, metadata and debug symbols. Four upstream
library projects (core, Rocks, Pdb, Mdb) are merged into this single
assembly and re-namespaced under CodeBrix.AssemblyTools.*.

There are no other packable projects, no sample apps and no tool projects
in this repository. See EXTRAS-README.txt.


REPOSITORY LAYOUT
=================

Root files
----------
    CodeBrix.AssemblyTools.slnx    solution; carries a "Solution Items"
                                   folder (.gitignore, AGENT-README.txt,
                                   EXTRAS-README.txt, global.json,
                                   icon-codebrix-128.png, LICENSE,
                                   MAINTAINER-README.txt, README-INDEX.txt,
                                   README.md, THIRD-PARTY-NOTICES.txt) and a
                                   "Tests" folder holding the test project
    global.json                    selects the Microsoft.Testing.Platform
                                   test runner; pins no SDK version (see
                                   TESTING)
    AGENT-README.txt               consumer documentation; PACKED into the
                                   nupkg (see PACKAGING AND PUBLISHING)
    MAINTAINER-README.txt          this file
    EXTRAS-README.txt              non-package content inventory
    README-INDEX.txt               map of the README files here
    README.md                      human-facing overview; packed as the
                                   nuspec readme
    LICENSE                        MIT
    THIRD-PARTY-NOTICES.txt        upstream attribution; packed
    icon-codebrix-128.png          package icon; packed
    .gitignore                     see the sidecar-fixture note under TESTING

Source layout (src/CodeBrix.AssemblyTools/) -- folder to namespace to
upstream-folder mapping:

    Internal/             -- ns CodeBrix.AssemblyTools.Internal
                             (upstream: Mono/) Disposable, Empty, MergeSort
                             and other implementation helpers.
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
                             (upstream: Mono.Collections.Generic/) the
                             observable Collection<T> helpers the object
                             model is built on.
    Security/Cryptography/ -- ns CodeBrix.AssemblyTools.Security.Cryptography
                             (upstream: Mono.Security.Cryptography/)
                             strong-name hashing helpers.
    Rocks/                -- ns CodeBrix.AssemblyTools.Rocks
                             (upstream: rocks/Mono.Cecil.Rocks/) extension
                             methods: GetAllTypes, SimplifyMacros,
                             OptimizeMacros, DocCommentId, ILParser, etc.
    Pdb/                  -- ns CodeBrix.AssemblyTools.Pdb
                             (upstream: symbols/pdb/Mono.Cecil.Pdb/) the PDB
                             symbol-provider facade.
    Pdb.Cci/              -- ns CodeBrix.AssemblyTools.Pdb.Cci
                             (upstream: symbols/pdb/Microsoft.Cci.Pdb/) the
                             Microsoft CCI native-PDB reader / writer;
                             internal.
    Mdb/                  -- ns CodeBrix.AssemblyTools.Mdb
                             (upstream: symbols/mdb/Mono.Cecil.Mdb/) the MDB
                             symbol-provider facade.
    Mdb.SymbolWriter/     -- ns CodeBrix.AssemblyTools.Mdb.SymbolWriter
                             (upstream:
                             symbols/mdb/Mono.CompilerServices.SymbolWriter/)
                             the upstream symbol-writer implementation.

    InternalsVisibleTo.cs -- grants InternalsVisibleTo to
                             "CodeBrix.AssemblyTools.Tests".

Test layout (tests/CodeBrix.AssemblyTools.Tests/):

    SmokeTests.cs         CodeBrix-authored end-to-end smoke coverage.
    Core/                 the bulk of the ported upstream suite, plus its
                          fixtures and helpers (BaseTestFixture.cs,
                          CompilationService.cs, Extensions.cs,
                          Formatter.cs, TypeDefinitionUtils.cs,
                          LoadAssemblyDefinitionForTestsBaseSimple.cs).
    Pdb/, Mdb/, Rocks/    the symbol-format and extension-method suites.
    Resources/            binary and source test fixtures; see TESTING.
    xunit.runner.json     runner configuration; see TESTING.


BUILDING
========

    dotnet restore CodeBrix.AssemblyTools.slnx
    dotnet build   CodeBrix.AssemblyTools.slnx

Project facts recorded in the library csproj under
src/CodeBrix.AssemblyTools/:

  * TargetFramework net10.0 only. No multi-targeting.
  * AllowUnsafeBlocks is ON. The IL / PE reading paths use unsafe pointer
    arithmetic in several places.
  * GenerateDocumentationFile is FALSE. This is a deliberate, temporary
    relaxation of the CodeBrix convention: the port started from roughly
    400 upstream source files with sparse XML doc comments, and turning doc
    generation on would require writing hundreds of new summaries. Revisit
    this in a follow-up audit; do not treat it as licence to skip XML docs
    on newly authored CodeBrix code.
  * NoWarn carries CS8981 project-wide, as an explicitly granted exception
    to the CodeBrix "no project-wide NoWarn" rule. CS8981 warns about
    all-lowercase type names; every hit fires on upstream Microsoft CCI PDB
    source incorporated verbatim under Pdb.Cci/, and renaming those types
    would diverge from upstream for no benefit. Do NOT add any other
    project-wide suppression -- fix warnings at source.
  * A .nupkg is produced on every build (GeneratePackageOnBuild is true),
    so an ordinary `dotnet build` also packs.


TESTING
=======

    dotnet test CodeBrix.AssemblyTools.slnx

The test runner is Microsoft.Testing.Platform, selected by `global.json` at
the repository root (`{ "test": { "runner": "Microsoft.Testing.Platform" } }`).
That file pins no SDK version, so the newest installed .NET 10 SDK is still
used; keep it committed, because without it `dotnet test` falls back to the
older VSTest bridge.

    # one class
    dotnet test CodeBrix.AssemblyTools.slnx --filter "FullyQualifiedName~ModuleTests"

Test project: tests/CodeBrix.AssemblyTools.Tests/, targeting net10.0 with
AllowUnsafeBlocks ON. Framework is xUnit v3 (upstream used NUnit);
assertions are a mix of SilverAssertions fluent and raw xUnit Assert.
Microsoft.CodeAnalysis.CSharp is referenced so CompilationService can
compile the .cs fixtures under Resources/cs at test time.

Parallelism is DISABLED
-----------------------
xunit.runner.json sets "parallelizeTestCollections": false. Many ported
tests round-trip assemblies through shared temp paths under
Path.GetTempPath() (the codebrix-assemblytools-irt and
codebrix-assemblytools-drt directories) via the TestRunner round-trip
helper. Running collections in parallel races them into IOException
("file is being used by another process") and into non-thread-safe writes
inside the library's own collections. The suite is not CPU-bound, so
sequential execution costs essentially nothing. Do not re-enable
parallelism to "speed the suite up".

Binary fixtures and the .gitignore exception
--------------------------------------------
tests/CodeBrix.AssemblyTools.Tests/Resources/ holds pre-built fixtures
copied verbatim from the upstream MIT-licensed test suite:

    Resources/assemblies/   .dll / .exe images plus their .pdb and .mdb
                            sidecars (about 128 files)
    Resources/cs/           C# sources compiled at test time by
                            CompilationService
    Resources/il/           IL sources used as reader inputs

Many symbol tests (PortablePdbTests, PdbTests, SymbolTests, parts of
ImageReadTests) require the .pdb sidecars to sit NEXT TO their .dll/.exe
under Resources/assemblies/. The standard VisualStudio .gitignore template
ignores "*.pdb" globally, so this repository's .gitignore carries explicit
un-ignore exceptions:

    !tests/**/Resources/**/*.pdb
    !tests/**/Resources/**/*.mdb

Do NOT remove those lines, and do NOT generalise the "*.pdb" ignore in a
way that re-shadows them. If a clone is missing the sidecars, roughly 50
symbol tests fail with SymbolsNotFoundException / FileNotFoundException on
the missing .pdb path. The fix is to restore the fixtures -- never to change
library code, and never to skip the tests per platform.

The .cs fixtures under Resources are removed from the default Compile glob
in the test csproj (they are inputs, not test code) and the whole Resources
tree is included as Content with CopyToOutputDirectory="PreserveNewest" so
the runtime helpers can find it.

Native-PDB writing is Windows-only at run time, so the write half of
Pdb/PdbTests.cs does not exercise on Linux or macOS. That is expected;
it is a platform limit of the format, not a port defect.


PACKAGING AND PUBLISHING
========================

There is no pack script and no CI workflow. Packing happens as a side
effect of building, because the library csproj sets
GeneratePackageOnBuild=true; `dotnet pack` on the same project produces the
same nupkg.

What ships in the nupkg, beyond the assembly (all four are declared as
<None ... Pack="true" PackagePath=""> in the library csproj, pulled from the
repository root two levels up):

    icon-codebrix-128.png     PackageIcon
    README.md                 PackageReadmeFile
    AGENT-README.txt          the consumer guide -- this is the ONLY
                              README-family file that ships; keep it
                              accurate, because agents read it out of the
                              extracted package
    THIRD-PARTY-NOTICES.txt   upstream attribution

MAINTAINER-README.txt, EXTRAS-README.txt and README-INDEX.txt are
repository-only and are NOT packed.

Versioning
----------
Date-stamped and auto-incrementing, computed in the csproj from
System.DateTime.UtcNow, in the form 1.<x>.<y>.<z>:

    1   major       pinned to 1 for this library
    x   minor       whole years since $(_VersionBaseYear) (2026 => 0)
    y   build       UTC day of year, 1-based (Jan 1 = 1)
    z   revision    UTC minute of day, 0..1439

Version, AssemblyVersion and FileVersion all take that value. Consequences
worth remembering:

  * Every build produces a NEW version, and with GeneratePackageOnBuild that
    means a fresh .nupkg on every build.
  * Two builds within the SAME UTC minute produce the SAME version. Do not
    publish two packages from within one minute.
  * This is not SemVer: minor encodes the year and major is pinned, so
    major/minor say nothing about API compatibility.
  * To re-baseline the minor number, change _VersionBaseYear in the csproj.

Publishing is manual: build, then push the produced .nupkg to nuget.org.
Tag the repository to match the published package version.


PROVENANCE AND VENDORED SOURCES
===============================

CodeBrix.AssemblyTools is a port of Mono.Cecil 0.11.6 (upstream:
https://github.com/jbevain/cecil, MIT). Essentially the whole upstream
source tree is incorporated, re-namespaced from Mono.* / Mono.Cecil.* into
CodeBrix.AssemblyTools.*, with the four upstream library projects merged
into one assembly. Upstream top-of-file copyright headers are preserved
verbatim on every ported file, and every ported file carries a
"//was previously: <upstream namespace>;" comment on its namespace line so
the upstream origin stays trivially recoverable. THIRD-PARTY-NOTICES.txt is
the authoritative attribution record and lists three components: Mono.Cecil,
the embedded Microsoft CCI PDB reader / writer that Mono.Cecil itself
carries (vendored under Pdb.Cci/), and the binary test fixtures.

The port was originally taken from the upstream 0.11.5 tag and brought up to
0.11.6 by cherry-picking every source-level commit between the two tags. The
0.11.6 correctness fixes this port therefore carries -- worth knowing when
diffing against a 0.11.5 checkout:

  * WriteCompressedInt32 negative-value encoding correctness (upstream
    commit 608fac6). On 0.11.5 this was a silent metadata-corruption bug for
    marshal descriptors and custom attributes with negative constants.
  * BaseAssemblyResolver tries ".dll" before ".exe" (8e1ae7b), so resolving
    against modern published apps no longer fails with
    BadImageFormatException when an unmanaged Foo.exe apphost sits beside
    the managed Foo.dll.
  * MetadataResolver returns MethodDefinition fast-paths directly (56d4409),
    which also fixes privatescope method resolution for generic instance
    methods.
  * Import via System.Reflection preserves required and optional custom
    modifiers -- in, ref readonly, init-only, required, the unmanaged
    constraint -- instead of silently dropping them (fec4ee9).
  * New AllowByRefLikeConstraint flag (0x0020) on GenericParameterAttributes
    plus a matching AllowByRefLikeConstraint property on GenericParameter,
    for C# 13 "where T : allows ref struct" (1da2145).
  * Strong-name SHA1 hashing uses SHA1.Create(), which is FIPS-compliant on
    hosts with FIPSAlgorithmPolicy = 1 (dff01d9). Upstream used
    SHA1CryptoServiceProvider, obsolete on .NET 6 and later.
  * The SymWriter COM object is released deterministically even when
    NativePdbWriter.Write is never reached because of an exception
    (50292e7), fixing intermittent PDB file locking on Windows.
  * TypeDefinition implements ICustomDebugInformationProvider, so type-level
    PDB debug metadata (for example the NullableContext entries MSBuild
    emits on generated types) survives round-trips instead of being dropped
    (a0f61f9).
  * Instruction gained the public GetPrototype() API for rewriter code that
    clones an instruction (04286ac).

Two upstream commits in that range were deliberately NOT applied, and
THIRD-PARTY-NOTICES.txt records why: a840bdb (retarget the upstream test
harness to .NET 8) is moot because these tests target net10.0, and ba9c6c7
(bump .NET Framework reference assemblies) is moot because this port does
not target .NET Framework.

The library csproj's PackageDescription is written as a plain statement of
what the library does; upstream project names and upstream version numbers
belong in THIRD-PARTY-NOTICES.txt and in this file, not in the NuGet
metadata or in README.md.

Maintaining the vendored source
-------------------------------
Treat Cecil/, Cil/, Metadata/, PE/, Collections/, Security/, Rocks/, Pdb/,
Pdb.Cci/, Mdb/, Mdb.SymbolWriter/ and Internal/ as vendored upstream code.
Prefer changes that a future upstream cherry-pick can still be applied over.
When you must diverge, say so in a comment at the divergence rather than
silently reformatting, and keep the "//was previously:" namespace comments
and the upstream copyright headers intact.


CODING CONVENTIONS
==================

CodeBrix family conventions, as they apply in this repository:

  * Target framework net10.0 only. No multi-targeting.
  * Nullable reference types are OFF library-wide. Do NOT use "?" on
    reference types (string?, MyClass?) and do NOT use the null-forgiving
    "!" operator. Value-type nullables (int?, DateOnly?, enum?) are fine.
  * ImplicitUsings is OFF and there are no "global using" directives. Every
    file writes its own usings at the top.
  * File-scoped namespaces only. No block-scoped "namespace X { ... }".
  * Every ported .cs file preserves its upstream copyright header verbatim
    and carries a "//was previously:" provenance comment on its namespace
    line.
  * AllowUnsafeBlocks is ON for this library.
  * No project-level warning suppression beyond the granted CS8981
    exception described under BUILDING. Fix warnings at source.

Test project conventions:

  * xUnit v3 plus SilverAssertions.
  * Every call inside a test that accepts a CancellationToken passes
    TestContext.Current.CancellationToken (xUnit1051).
  * Test classes are named <ClassUnderTest>Tests; helper and scenario files
    may skip the Tests suffix.
  * Test method names use <Member>_snake_case_description or plain
    snake_case.
  * Assertions mix SilverAssertions fluent (.Should().Be(...)) and xUnit
    Assert.Equal -- fluent where a test was substantively rewritten,
    mechanical xUnit where the port stayed close to one-for-one. Both are
    acceptable here; prefer fluent for newly authored tests.


NOTES
=====

  * Nothing in this repository requires a native library, a GPU, a display
    or network access. The full suite runs on a headless Linux host.
  * The AI-agent pointer files at the repository root and under .cursor/,
    .github/, .junie/ are stubs that route an agent to AGENT-README.txt.
    They are maintained centrally across the CodeBrix family -- do not
    hand-edit them here.
  * The public surface is large (roughly 213 public types). When you add or
    rename public API, update AGENT-README.txt in the same change; it is
    what ships to consumers inside the package.
