================================================================================
EXTRAS-README: CodeBrix.AssemblyTools
Samples, tools and other content in this repository that is not part of a NuGet package
================================================================================

This repository contains NO sample applications, demo projects, benchmark
projects or tool projects. It holds exactly two projects: the packable
library and its test project. There is no samples/ folder, no tools/ folder,
and no build or codegen scripts.


TEST PROJECT
============

    tests/CodeBrix.AssemblyTools.Tests/

The only non-package content in the repository. It is not packed and is not
published; it exists to verify the library. Run it with:

    dotnet test CodeBrix.AssemblyTools.slnx

It doubles as the worked-example corpus for the library: AGENT-README.txt
points consumers at individual test files on GitHub as the canonical usage
examples for each feature area (reading and writing modules, ILProcessor
edits, import, custom attributes, resolution, symbols, the extension-method
Rocks). If you are looking for "how do I do X with this library", the test
file named for X is the answer.


OPTIONAL AND FIXTURE TEST DATA
==============================

    tests/CodeBrix.AssemblyTools.Tests/Resources/

Binary and source fixtures copied verbatim from the upstream MIT-licensed
test suite. Nothing here is optional in the "opt-in" sense -- the suite needs
all of it -- but it is data rather than code, and it has a maintenance rule
attached:

    Resources/assemblies/   pre-built .dll / .exe images with their .pdb and
                            .mdb sidecars (about 128 files)
    Resources/cs/           C# sources compiled at test time
    Resources/il/           IL sources used as reader inputs

The .pdb and .mdb sidecars are only in git because .gitignore carries
explicit un-ignore exceptions for them. See TESTING in MAINTAINER-README.txt
before touching .gitignore or moving these files.

There are no opt-in environment variables in this repository. The whole
suite runs unconditionally on a headless host; the only naturally skipped
coverage is native-PDB WRITING, which the format supports on Windows only.
