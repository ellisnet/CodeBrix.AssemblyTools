//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using System.Runtime.InteropServices;

// Upstream Mono.Cecil 0.11.5 declared this GUID on its assembly (in the file
// Mono.Cecil/AssemblyInfo.cs) as its COM type-library identity. Preserving
// the same GUID on CodeBrix.AssemblyTools means any consumer code that reads
// Cecil's assembly GUID -- via `Marshal.GetTypeLibGuidForAssembly` or
// `typeof(AssemblyDefinition).Assembly.GetCustomAttribute<GuidAttribute>()` --
// will find this library when it is swapped in as a drop-in replacement.
[assembly: Guid("fd225bb4-fa53-44b2-a6db-85f5e48dcb54")]

// ComVisible is false-by-default on modern .NET, but we assert it explicitly
// to mirror upstream and to keep the P/Invoke surface unambiguous.
[assembly: ComVisible(false)]
