//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//
// Ported from Mono.Cecil 0.11.5 rocks/Test/Mono.Cecil.Tests/DocCommentIdTests.cs.
// Upstream test file contained two namespaces: `N` (test subjects) and
// `Mono.Cecil.Tests` (this class). The subjects moved to
// DocCommentIdTestSubjects.cs to satisfy CodeBrix's file-scoped namespace rule.
//

using System;
using System.Linq;
using N;
using Xunit;
using SilverAssertions;
using CodeBrix.AssemblyTools;
using CodeBrix.AssemblyTools.Rocks;

namespace CodeBrix.AssemblyTools.Tests.Rocks; //was previously: Mono.Cecil.Tests;

public class DocCommentIdTests {

	[Fact]
	public void TypeDef ()
	{
		AssertDocumentID ("T:N.X", GetTestType ());
	}

	[Fact]
	public void ParameterlessCtor ()
	{
		var type = GetTestType ();
		var ctor = type.GetConstructors ().Single (m => m.Parameters.Count == 0);

		AssertDocumentID ("M:N.X.#ctor", ctor);
	}

	[Fact]
	public void CtorWithParameters ()
	{
		var type = GetTestType ();
		var ctor = type.GetConstructors ().Single (m => m.Parameters.Count == 1);

		AssertDocumentID ("M:N.X.#ctor(System.Int32)", ctor);
	}

	[Fact]
	public void Field ()
	{
		var type = GetTestType ();
		var field = type.Fields.Single (m => m.Name == "q");

		AssertDocumentID ("F:N.X.q", field);
	}

	[Fact]
	public void ConstField ()
	{
		var type = GetTestType ();
		var field = type.Fields.Single (m => m.Name == "PI");

		AssertDocumentID ("F:N.X.PI", field);
	}

	[Fact]
	public void ParameterlessMethod ()
	{
		var type = GetTestType ();
		var method = type.Methods.Single (m => m.Name == "f");

		AssertDocumentID ("M:N.X.f", method);
	}

	[Fact]
	public void MethodWithByRefParameters ()
	{
		var type = GetTestType ();
		var method = type.Methods.Single (m => m.Name == "bb");

		AssertDocumentID ("M:N.X.bb(System.String,System.Int32@)", method);
	}

	[Fact]
	public void MethodWithArrayParameters ()
	{
		var type = GetTestType ();
		var method = type.Methods.Single (m => m.Name == "gg");

		AssertDocumentID ("M:N.X.gg(System.Int16[],System.Int32[0:,0:])", method);
	}

	[Theory]
	[InlineData("WithNestedType", "WithNestedType``1(N.GenericType{``0}.NestedType)")]
	[InlineData("WithIntOfNestedType", "WithIntOfNestedType``1(N.GenericType{System.Int32}.NestedType)")]
	[InlineData("WithNestedGenericType", "WithNestedGenericType``1(N.GenericType{``0}.NestedGenericType{``0}.NestedType)")]
	[InlineData("WithIntOfNestedGenericType", "WithIntOfNestedGenericType``1(N.GenericType{System.Int32}.NestedGenericType{System.Int32}.NestedType)")]
	[InlineData("WithMultipleTypeParameterAndNestedGenericType", "WithMultipleTypeParameterAndNestedGenericType``2(N.GenericType{``0}.NestedGenericType{``1}.NestedType)")]
	[InlineData("WithMultipleTypeParameterAndIntOfNestedGenericType", "WithMultipleTypeParameterAndIntOfNestedGenericType``2(N.GenericType{System.Int32}.NestedGenericType{System.Int32}.NestedType)")]
	public void GenericMethodWithNestedTypes (string methodName, string expectedId)
	{
		var type = GetTestType (typeof(GenericMethod));
		var method = type.Methods.Single (m => m.Name == methodName);

		AssertDocumentID ("M:N.GenericMethod." + expectedId, method);
	}

	[Theory]
	[InlineData("WithTypeParameterOfGenericMethod", "WithTypeParameterOfGenericMethod``1(System.Collections.Generic.List{``0})")]
	[InlineData("WithTypeParameterOfGenericType", "WithTypeParameterOfGenericType(System.Collections.Generic.Dictionary{`0,`1})")]
	// Typo fix: my original hand-port of this InlineData copied the
	// `WithTypeParameterOfGenericType``1(...)` string from the upstream method's
	// XML `<summary>` comment, which itself was a typo (the doc-ID the summary
	// claims would be generated is wrong). Upstream's actual TestCase used
	// `WithTypeParameterOfNestedGenericType``1(...)` -- matching the real
	// method name -- which is what Cecil's DocCommentId.GetDocCommentId actually
	// produces. This line now matches the upstream test data.
	[InlineData("WithTypeParameterOfNestedGenericType", "WithTypeParameterOfNestedGenericType``1(System.Collections.Generic.List{`1})")]
	[InlineData("WithTypeParameterOfGenericTypeAndGenericMethod", "WithTypeParameterOfGenericTypeAndGenericMethod``1(System.Collections.Generic.Dictionary{`1,``0})")]
	public void NestedGenericTypeWithGenericMethods (string methodName, string expectedId)
	{
		var nestedType = GetTestType (typeof(GenericType<>.NestedGenericType<>));
		var method = nestedType.Methods.Single (m => m.Name == methodName);

		AssertDocumentID ("M:N.GenericType`1.NestedGenericType`1." + expectedId, method);
	}

	[Fact]
	public void OperatorAddition ()
	{
		var type = GetTestType ();
		var method = type.Methods.Single (m => m.Name == "op_Addition");

		AssertDocumentID ("M:N.X.op_Addition(N.X,N.X)", method);
	}

	[Fact]
	public void Property ()
	{
		var type = GetTestType ();
		var property = type.Properties.Single (m => m.Name == "prop");

		AssertDocumentID ("P:N.X.prop", property);
	}

	[Fact]
	public void Event ()
	{
		var type = GetTestType ();
		var @event = type.Events.Single (m => m.Name == "d");

		AssertDocumentID ("E:N.X.d", @event);
	}

	[Fact]
	public void Indexer ()
	{
		var type = GetTestType ();
		var indexer = type.Properties.Single (m => m.Name == "Item");

		AssertDocumentID ("P:N.X.Item(System.String)", indexer);
	}

	[Fact]
	public void Nested ()
	{
		var type = GetTestType ();
		var nested = type.NestedTypes.Single (m => m.Name == "Nested");

		AssertDocumentID ("T:N.X.Nested", nested);
	}

	[Fact]
	public void Delegate ()
	{
		var type = GetTestType ();
		var @delegate = type.NestedTypes.Single (m => m.Name == "D");

		AssertDocumentID ("T:N.X.D", @delegate);
	}

	[Fact]
	public void OperatorExplicit ()
	{
		var type = GetTestType ();
		var method = type.Methods.Single (m => m.Name == "op_Explicit");

		AssertDocumentID ("M:N.X.op_Explicit(N.X)~System.Int32", method);
	}

	[Fact]
	public void ExplicitInterfaceImplementationMethod ()
	{
		var type = GetTestType ();
		var method = type.Methods.Single (m => m.Name == "N.IX<N.KVP<System.String,System.Int32>>.IXA");

		AssertDocumentID ("M:N.X.N#IX{N#KVP{System#String,System#Int32}}#IXA(N.KVP{System.String,System.Int32})", method);
	}

	TypeDefinition GetTestType ()
	{
		return GetTestType (typeof(X));
	}

	TypeDefinition GetTestType (Type type)
	{
		return type.ToDefinition ();
	}

	static void AssertDocumentID (string docId, IMemberDefinition member)
	{
		(DocCommentId.GetDocCommentId (member)).Should().Be(docId);
	}
}
