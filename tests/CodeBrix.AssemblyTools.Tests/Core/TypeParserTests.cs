using System;
using System.Linq;
using Xunit;
using SilverAssertions;
namespace CodeBrix.AssemblyTools.Tests.Core; //was previously: Mono.Cecil.Tests;
public class TypeParserTests : BaseTestFixture {

	[Fact]
	public void SimpleStringReference ()
	{
		var module = GetCurrentModule ();
		var corlib = module.TypeSystem.CoreLibrary;

		const string fullname = "System.String";

		var type = TypeParser.ParseType (module, fullname);
		Assert.NotNull (type);
		(type.Scope).Should().Be(corlib);
		(type.Module).Should().Be(module);
		(type.Namespace).Should().Be("System");
		(type.Name).Should().Be("String");
		(type.MetadataType).Should().Be(MetadataType.String);
		Assert.False (type.IsValueType);
		Assert.IsAssignableFrom<TypeReference>(type);
	}

	[Fact]
	public void SimpleInt32Reference ()
	{
		var module = GetCurrentModule ();
		var corlib = module.TypeSystem.CoreLibrary;

		const string fullname = "System.Int32";

		var type = TypeParser.ParseType (module, fullname);
		Assert.NotNull (type);
		(type.Scope).Should().Be(corlib);
		(type.Module).Should().Be(module);
		(type.Namespace).Should().Be("System");
		(type.Name).Should().Be("Int32");
		(type.MetadataType).Should().Be(MetadataType.Int32);
		Assert.True (type.IsValueType);
		Assert.IsAssignableFrom<TypeReference>(type);
	}

	[Fact]
	public void SimpleTypeDefinition ()
	{
		var module = GetCurrentModule ();

		// was previously: const string fullname = "Mono.Cecil.Tests.TypeParserTests";
		const string fullname = "CodeBrix.AssemblyTools.Tests.Core.TypeParserTests";

		var type = TypeParser.ParseType (module, fullname);
		Assert.NotNull (type);
		(type.Scope).Should().Be(module);
		(type.Module).Should().Be(module);
		// was previously: (type.Namespace).Should().Be("Mono.Cecil.Tests");
		(type.Namespace).Should().Be("CodeBrix.AssemblyTools.Tests.Core");
		(type.Name).Should().Be("TypeParserTests");
		Assert.IsAssignableFrom<TypeDefinition>(type);
	}

	[Fact]
	public void ByRefTypeReference ()
	{
		var module = GetCurrentModule ();
		var corlib = module.TypeSystem.CoreLibrary;

		const string fullname = "System.String&";

		var type = TypeParser.ParseType (module, fullname);

		Assert.IsAssignableFrom<ByReferenceType>(type);

		type = ((ByReferenceType) type).ElementType;

		Assert.NotNull (type);
		(type.Scope).Should().Be(corlib);
		(type.Module).Should().Be(module);
		(type.Namespace).Should().Be("System");
		(type.Name).Should().Be("String");
		Assert.IsAssignableFrom<TypeReference>(type);
	}

	[Fact]
	public void FullyQualifiedTypeReference ()
	{
		var module = GetCurrentModule ();
		var cecil = module.AssemblyReferences.Where (reference => reference.Name != typeof (TypeDefinition).Assembly.GetName ().Name).First ();

		// was previously: var fullname = "Mono.Cecil.TypeDefinition, " + cecil.FullName;
		var fullname = "CodeBrix.AssemblyTools.TypeDefinition, " + cecil.FullName;

		var type = TypeParser.ParseType (module, fullname);
		Assert.NotNull (type);
		(type.Scope).Should().Be(cecil);
		(type.Module).Should().Be(module);
		// was previously: (type.Namespace).Should().Be("Mono.Cecil");
		(type.Namespace).Should().Be("CodeBrix.AssemblyTools");
		(type.Name).Should().Be("TypeDefinition");
		Assert.IsAssignableFrom<TypeReference>(type);
	}

	[Fact]
	public void OpenGenericType ()
	{
		var module = GetCurrentModule ();
		var corlib = module.TypeSystem.CoreLibrary;

		const string fullname = "System.Collections.Generic.Dictionary`2";

		var type = TypeParser.ParseType (module, fullname);
		Assert.NotNull (type);
		(type.Scope).Should().Be(corlib);
		(type.Module).Should().Be(module);
		(type.Namespace).Should().Be("System.Collections.Generic");
		(type.Name).Should().Be("Dictionary`2");
		Assert.IsAssignableFrom<TypeReference>(type);
		(type.GenericParameters.Count).Should().Be(2);
	}

	public class ID {}

	[Fact]
	public void SimpleNestedType ()
	{
		var module = GetCurrentModule ();

		// was previously: const string fullname = "Mono.Cecil.Tests.TypeParserTests+ID";
		const string fullname = "CodeBrix.AssemblyTools.Tests.Core.TypeParserTests+ID";

		var type = TypeParser.ParseType (module, fullname);

		Assert.NotNull (type);
		(type.Module).Should().Be(module);
		(type.Scope).Should().Be(module);
		(type.Namespace).Should().Be("");
		(type.Name).Should().Be("ID");

		// was previously: (type.FullName).Should().Be("Mono.Cecil.Tests.TypeParserTests/ID");
		(type.FullName).Should().Be("CodeBrix.AssemblyTools.Tests.Core.TypeParserTests/ID");
		(TypeParser.ToParseable (type)).Should().Be(fullname);
	}

	[Fact]
	public void TripleNestedTypeWithScope ()
	{
		var module = GetCurrentModule ();

		const string fullname = "Bingo.Foo`1+Bar`1+Baz`1, Bingo";

		var type = TypeParser.ParseType (module, fullname);

		(TypeParser.ToParseable (type)).Should().Be("Bingo.Foo`1+Bar`1+Baz`1, Bingo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

		Assert.NotNull (type);
		(type.Scope.Name).Should().Be("Bingo");
		(type.Module).Should().Be(module);
		(type.Namespace).Should().Be("");
		(type.Name).Should().Be("Baz`1");
		Assert.IsAssignableFrom<TypeReference>(type);
		(type.GenericParameters.Count).Should().Be(1);

		type = type.DeclaringType;

		Assert.NotNull (type);
		(type.Scope.Name).Should().Be("Bingo");
		(type.Module).Should().Be(module);
		(type.Namespace).Should().Be("");
		(type.Name).Should().Be("Bar`1");
		Assert.IsAssignableFrom<TypeReference>(type);
		(type.GenericParameters.Count).Should().Be(1);

		type = type.DeclaringType;

		Assert.NotNull (type);
		(type.Scope.Name).Should().Be("Bingo");
		(type.Module).Should().Be(module);
		(type.Namespace).Should().Be("Bingo");
		(type.Name).Should().Be("Foo`1");
		Assert.IsAssignableFrom<TypeReference>(type);
		(type.GenericParameters.Count).Should().Be(1);
	}

	[Fact]
	public void Vector ()
	{
		var module = GetCurrentModule ();

		const string fullname = "Bingo.Gazonk[], Bingo";

		var type = TypeParser.ParseType (module, fullname);

		(TypeParser.ToParseable (type)).Should().Be("Bingo.Gazonk[], Bingo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

		var array = type as ArrayType;
		Assert.NotNull (array);
		(array.Rank).Should().Be(1);
		Assert.True (array.IsVector);

		type = array.ElementType;

		Assert.NotNull (type);
		(type.Scope.Name).Should().Be("Bingo");
		(type.Module).Should().Be(module);
		(type.Namespace).Should().Be("Bingo");
		(type.Name).Should().Be("Gazonk");
		Assert.IsAssignableFrom<TypeReference>(type);
	}

	[Fact]
	public void ThreeDimensionalArray ()
	{
		var module = GetCurrentModule ();

		const string fullname = "Bingo.Gazonk[,,], Bingo";

		var type = TypeParser.ParseType (module, fullname);

		var array = type as ArrayType;
		Assert.NotNull (array);
		(array.Rank).Should().Be(3);
		Assert.False (array.IsVector);

		type = array.ElementType;

		Assert.NotNull (type);
		(type.Scope.Name).Should().Be("Bingo");
		(type.Module).Should().Be(module);
		(type.Namespace).Should().Be("Bingo");
		(type.Name).Should().Be("Gazonk");
		Assert.IsAssignableFrom<TypeReference>(type);
	}

	[Fact]
	public void GenericInstanceExternArguments ()
	{
		if (Platform.OnCoreClr)
			return;

		var module = GetCurrentModule ();

		var fullname = string.Format ("System.Collections.Generic.Dictionary`2[[System.Int32, {0}],[System.String, {0}]]",
			typeof (object).Assembly.FullName);

		var type = TypeParser.ParseType (module, fullname);

		(TypeParser.ToParseable (type)).Should().Be(fullname);

		var instance = type as GenericInstanceType;
		Assert.NotNull (instance);
		(instance.GenericArguments.Count).Should().Be(2);
		(type.Scope.Name).Should().Be("mscorlib");
		(type.Module).Should().Be(module);
		(type.Namespace).Should().Be("System.Collections.Generic");
		(type.Name).Should().Be("Dictionary`2");

		type = instance.ElementType;

		(type.GenericParameters.Count).Should().Be(2);

		var argument = instance.GenericArguments [0];
		(argument.Scope.Name).Should().Be("mscorlib");
		(argument.Module).Should().Be(module);
		(argument.Namespace).Should().Be("System");
		(argument.Name).Should().Be("Int32");

		argument = instance.GenericArguments [1];
		(argument.Scope.Name).Should().Be("mscorlib");
		(argument.Module).Should().Be(module);
		(argument.Namespace).Should().Be("System");
		(argument.Name).Should().Be("String");
	}

	[Fact]
	public void GenericInstanceMixedArguments ()
	{
		var module = GetCurrentModule ();

		// was previously: var fullname = string.Format ("System.Collections.Generic.Dictionary`2[Mono.Cecil.Tests.TypeParserTests,[System.String, {0}]]",
		var fullname = string.Format ("System.Collections.Generic.Dictionary`2[CodeBrix.AssemblyTools.Tests.Core.TypeParserTests,[System.String, {0}]]",
			typeof (object).Assembly.FullName);

		var type = TypeParser.ParseType (module, fullname);

		var instance = type as GenericInstanceType;
		Assert.NotNull (instance);
		(instance.GenericArguments.Count).Should().Be(2);
		(type.Scope.Name).Should().Be(Platform.OnCoreClr ? "System.Runtime" : "mscorlib");
		(type.Module).Should().Be(module);
		(type.Namespace).Should().Be("System.Collections.Generic");
		(type.Name).Should().Be("Dictionary`2");

		type = instance.ElementType;

		(type.GenericParameters.Count).Should().Be(2);

		var argument = instance.GenericArguments [0];
		Assert.IsAssignableFrom<TypeDefinition>(argument);
		(argument.Module).Should().Be(module);
		// was previously: (argument.Namespace).Should().Be("Mono.Cecil.Tests");
		(argument.Namespace).Should().Be("CodeBrix.AssemblyTools.Tests.Core");
		(argument.Name).Should().Be("TypeParserTests");

		argument = instance.GenericArguments [1];
		(argument.Scope.Name).Should().Be(Platform.OnCoreClr ? "System.Private.CoreLib" : "mscorlib");
		(argument.Module).Should().Be(module);
		(argument.Namespace).Should().Be("System");
		(argument.Name).Should().Be("String");
	}

	public class Foo<TX, TY> {
	}

	public class Bar {}

	[Fact]
	public void GenericInstanceTwoNonFqArguments ()
	{
		var module = GetCurrentModule ();

		// was previously: var fullname = string.Format ("System.Collections.Generic.Dictionary`2[Mono.Cecil.Tests.TypeParserTests+Bar,Mono.Cecil.Tests.TypeParserTests+Bar], {0}", typeof (object).Assembly.FullName);
		var fullname = string.Format ("System.Collections.Generic.Dictionary`2[CodeBrix.AssemblyTools.Tests.Core.TypeParserTests+Bar,CodeBrix.AssemblyTools.Tests.Core.TypeParserTests+Bar], {0}", typeof (object).Assembly.FullName);

		var type = TypeParser.ParseType (module, fullname);

		var instance = type as GenericInstanceType;
		Assert.NotNull (instance);
		(instance.GenericArguments.Count).Should().Be(2);
		(type.Scope.Name).Should().Be(Platform.OnCoreClr ? "System.Private.CoreLib" : "mscorlib");
		(type.Module).Should().Be(module);
		(type.Namespace).Should().Be("System.Collections.Generic");
		(type.Name).Should().Be("Dictionary`2");

		type = instance.ElementType;

		(type.GenericParameters.Count).Should().Be(2);

		var argument = instance.GenericArguments [0];
		(argument.Module).Should().Be(module);
		(argument.Namespace).Should().Be("");
		(argument.Name).Should().Be("Bar");
		Assert.IsAssignableFrom<TypeDefinition>(argument);

		argument = instance.GenericArguments [1];
		(argument.Module).Should().Be(module);
		(argument.Namespace).Should().Be("");
		(argument.Name).Should().Be("Bar");
		Assert.IsAssignableFrom<TypeDefinition>(argument);
	}

	[Fact]
	public void ComplexGenericInstanceMixedArguments ()
	{
		var module = GetCurrentModule ();

		// was previously: var fullname = string.Format ("System.Collections.Generic.Dictionary`2[[System.String, {0}],Mono.Cecil.Tests.TypeParserTests+Foo`2[Mono.Cecil.Tests.TypeParserTests,[System.Int32, {0}]]]",
		var fullname = string.Format ("System.Collections.Generic.Dictionary`2[[System.String, {0}],CodeBrix.AssemblyTools.Tests.Core.TypeParserTests+Foo`2[CodeBrix.AssemblyTools.Tests.Core.TypeParserTests,[System.Int32, {0}]]]",
			typeof (object).Assembly.FullName);

		var type = TypeParser.ParseType (module, fullname);

		var instance = type as GenericInstanceType;
		Assert.NotNull (instance);
		(instance.GenericArguments.Count).Should().Be(2);
		(type.Scope.Name).Should().Be(Platform.OnCoreClr ? "System.Runtime" : "mscorlib");
		(type.Module).Should().Be(module);
		(type.Namespace).Should().Be("System.Collections.Generic");
		(type.Name).Should().Be("Dictionary`2");

		type = instance.ElementType;

		(type.GenericParameters.Count).Should().Be(2);

		var argument = instance.GenericArguments [0];
		(argument.Scope.Name).Should().Be(Platform.OnCoreClr ? "System.Private.CoreLib" : "mscorlib");
		(argument.Module).Should().Be(module);
		(argument.Namespace).Should().Be("System");
		(argument.Name).Should().Be("String");

		argument = instance.GenericArguments [1];

		instance = argument as GenericInstanceType;
		Assert.NotNull (instance);
		(instance.GenericArguments.Count).Should().Be(2);
		(instance.Module).Should().Be(module);
		// was previously: (instance.ElementType.FullName).Should().Be("Mono.Cecil.Tests.TypeParserTests/Foo`2");
		(instance.ElementType.FullName).Should().Be("CodeBrix.AssemblyTools.Tests.Core.TypeParserTests/Foo`2");
		Assert.IsAssignableFrom<TypeDefinition>(instance.ElementType);

		argument = instance.GenericArguments [0];
		(argument.Module).Should().Be(module);
		// was previously: (argument.Namespace).Should().Be("Mono.Cecil.Tests");
		(argument.Namespace).Should().Be("CodeBrix.AssemblyTools.Tests.Core");
		(argument.Name).Should().Be("TypeParserTests");
		Assert.IsAssignableFrom<TypeDefinition>(argument);

		argument = instance.GenericArguments [1];
		(argument.Scope.Name).Should().Be(Platform.OnCoreClr ? "System.Private.CoreLib" : "mscorlib");
		(argument.Module).Should().Be(module);
		(argument.Namespace).Should().Be("System");
		(argument.Name).Should().Be("Int32");
	}
}
