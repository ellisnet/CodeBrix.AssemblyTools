//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

namespace CodeBrix.AssemblyTools; //was previously: Mono.Cecil;

static class Consts
{
	// Upstream Mono.Cecil used this constant to populate AssemblyProduct /
	// AssemblyTitle metadata in ProjectInfo.cs / AssemblyInfo.cs and to build
	// InternalsVisibleTo attribute values keyed on PublicKey. In this port
	// those assembly-level attributes come from the csproj's <Product> /
	// <Title> / <AssemblyName> properties and from InternalsVisibleTo.cs, so
	// this constant has no in-tree callers -- but it stays available under
	// the new name in case downstream consumer code reads it via reflection.
	public const string AssemblyName = "CodeBrix.AssemblyTools";

	// Upstream Mono.Cecil's strong-name public key. The CodeBrix port does
	// not strong-name-sign its assembly, so this value is preserved for
	// reference only and is no longer used by InternalsVisibleTo grants.
	public const string PublicKey = "00240000048000009400000006020000002400005253413100040000010001002b5c9f7f04346c324a3176f8d3ee823bbf2d60efdbc35f86fd9e65ea3e6cd11bcdcba3a353e55133c8ac5c4caaba581b2c6dfff2cc2d0edc43959ddb86b973300a479a82419ef489c3225f1fe429a708507bd515835160e10bc743d20ca33ab9570cfd68d479fcf0bc797a763bec5d1000f0159ef619e709d915975e87beebaf";
}
