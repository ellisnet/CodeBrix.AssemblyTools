//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using SR = System.Reflection;

using CodeBrix.AssemblyTools.Collections.Generic;
using CodeBrix.AssemblyTools.Cil;
using CodeBrix.AssemblyTools.PE;

namespace CodeBrix.AssemblyTools; //was previously: Mono.Cecil;


static partial class Mixin {

	public static ImageDebugHeaderEntry GetCodeViewEntry (this ImageDebugHeader header)
	{
		return GetEntry (header, ImageDebugType.CodeView);
	}

	public static ImageDebugHeaderEntry GetDeterministicEntry (this ImageDebugHeader header)
	{
		return GetEntry (header, ImageDebugType.Deterministic);
	}

	public static ImageDebugHeader AddDeterministicEntry (this ImageDebugHeader header)
	{
		var entry = new ImageDebugHeaderEntry (new ImageDebugDirectory { Type = ImageDebugType.Deterministic }, Empty<byte>.Array);
		if (header == null)
			return new ImageDebugHeader (entry);

		var entries = new ImageDebugHeaderEntry [header.Entries.Length + 1];
		Array.Copy (header.Entries, entries, header.Entries.Length);
		entries [entries.Length - 1] = entry;
		return new ImageDebugHeader (entries);
	}

	public static ImageDebugHeaderEntry GetEmbeddedPortablePdbEntry (this ImageDebugHeader header)
	{
		return GetEntry (header, ImageDebugType.EmbeddedPortablePdb);
	}

	public static ImageDebugHeaderEntry GetPdbChecksumEntry (this ImageDebugHeader header)
	{
		return GetEntry (header, ImageDebugType.PdbChecksum);
	}

	private static ImageDebugHeaderEntry GetEntry (this ImageDebugHeader header, ImageDebugType type)
	{
		if (!header.HasEntries)
			return null;

		for (var i = 0; i < header.Entries.Length; i++) {
			var entry = header.Entries [i];
			if (entry.Directory.Type == type)
				return entry;
		}

		return null;
	}

	public static string GetPdbFileName (string assemblyFileName)
	{
		return Path.ChangeExtension (assemblyFileName, ".pdb");
	}

	public static string GetMdbFileName (string assemblyFileName)
	{
		return assemblyFileName + ".mdb";
	}

	public static bool IsPortablePdb (string fileName)
	{
		using (var file = new FileStream (fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			return IsPortablePdb (file);
	}

	public static bool IsPortablePdb (Stream stream)
	{
		const uint ppdb_signature = 0x424a5342;

		if (stream.Length < 4) return false;
		var position = stream.Position;
		try {
			var reader = new BinaryReader (stream);
			return reader.ReadUInt32 () == ppdb_signature;
		} finally {
			stream.Position = position;
		}
	}
}

