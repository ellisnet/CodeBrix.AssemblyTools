//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

namespace CodeBrix.AssemblyTools.Metadata; //was previously: Mono.Cecil.Metadata;


enum CodedIndex {
	TypeDefOrRef,
	HasConstant,
	HasCustomAttribute,
	HasFieldMarshal,
	HasDeclSecurity,
	MemberRefParent,
	HasSemantics,
	MethodDefOrRef,
	MemberForwarded,
	Implementation,
	CustomAttributeType,
	ResolutionScope,
	TypeOrMethodDef,
	HasCustomDebugInformation,
}
