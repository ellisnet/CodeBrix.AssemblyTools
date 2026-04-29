using Xunit;
using SilverAssertions;
using System;
using System.Linq;

namespace CodeBrix.AssemblyTools.Tests.Core; //was previously: Mono.Cecil.Tests;
public class MethodReferenceComparerTests : LoadAssemblyDefinitionForTestsBaseSimple {

	private TypeDefinition _class1;
	private TypeDefinition _class2;
	// Upstream NUnit ran this as a `[SetUp]` method before each test. xUnit
	// instantiates a fresh test-class instance per test, so the equivalent is
	// the constructor. Without this, `_class1` / `_class2` / `_assembly` stay
	// null and every test below throws NullReferenceException.
	public MethodReferenceComparerTests ()
	{
		SetupAssemblyDefinitions (typeof (MethodReferenceComparerTests).Assembly);
		_class1 = TypeDefinitionUtils.TypeDefinitionFor (typeof (Class1), _assembly);
		_class2 = TypeDefinitionUtils.TypeDefinitionFor (typeof (Class2), _assembly);
	}

	[Fact]
	public void MethodReferenceEqualsMethodDefinition ()
	{
		var typeDefinition = TypeDefinitionUtils.TypeDefinitionFor (typeof (Int32), _mscorlib);
		var method = typeDefinition.Methods.Single (m => m.Name == "GetHashCode");
		var methodReference = new MethodReference (method.Name, method.ReturnType, method.DeclaringType);
		methodReference.HasThis = method.HasThis;

		Assert.True(MethodReferenceComparer.AreEqual (method, methodReference));
	}

	[Fact]
	public void VerifyMethodSignatureMatches ()
	{
		Assert.True (CompareSignatures ("MethodWithNoParametersOrReturn"));
		Assert.True (CompareSignatures ("GenericMethodWithNoParametersOrReturn"));
		Assert.False (CompareSignatures ("MethodWithNoParametersOrReturn", "GenericMethodWithNoParametersOrReturn"));

		Assert.True (CompareSignatures ("MethodWithIntParameterAndVoidReturn"));
	}

	[Fact]
	public void VerifySignatureComparisonConsidersStatic ()
	{
		Assert.True (CompareSignatures ("StaticMethodWithNoParametersOrReturn"));
		Assert.True (CompareSignatures ("StaticMethodWithNoParametersOrReturn"));
		Assert.False (CompareSignatures ("MethodWithNoParametersOrReturn", "StaticMethodWithNoParametersOrReturn"));
		Assert.False (CompareSignatures ("GenericMethodWithNoParametersOrReturn", "GenericStaticMethodWithNoParametersOrReturn"));
	}

	[Fact]
	public void VerifyMethodSignatureWithGenericParameters ()
	{
		Assert.True (CompareSignatures ("GenericMethodWithGenericParameter"));
		Assert.True (CompareSignatures ("GenericMethodWithGenericParameterArray"));
		Assert.True (CompareSignatures ("GenericMethodWithByReferenceGenericParameter"));
		Assert.True (CompareSignatures ("GenericMethodWithGenericInstanceGenericParameter"));
	}

	[Fact]
	public void VerifyNonResolvableMethodReferencesWithDifferentParameterTypesAreNotEqual ()
	{
		var method1 = new MethodReference ("TestMethod", _class1.Module.TypeSystem.Void, _class1);
		method1.Parameters.Add (new ParameterDefinition (new ByReferenceType (_class1.Module.TypeSystem.Int16)));

		var method2 = new MethodReference ("TestMethod", _class1.Module.TypeSystem.Void, _class1);
		method2.Parameters.Add (new ParameterDefinition (new ByReferenceType (_class1.Module.TypeSystem.Char)));

		Assert.False (MethodReferenceComparer.AreEqual (method1, method2));
	}

	[Fact]
	public void VerifyNonResolvableRecursiveMethodsDontStackOverflow ()
	{
		var method1 = new MethodReference ("TestMethod", _class1.Module.TypeSystem.Void, _class1);
		method1.GenericParameters.Add (new GenericParameter (method1));
		method1.Parameters.Add (new ParameterDefinition (method1.GenericParameters[0]));

		var method2 = new MethodReference ("TestMethod", _class1.Module.TypeSystem.Void, _class1);
		method2.GenericParameters.Add (new GenericParameter (method2));
		method2.Parameters.Add (new ParameterDefinition (method2.GenericParameters[0]));

		Assert.True (MethodReferenceComparer.AreEqual (method1, method2));
	}

	bool CompareSignatures (string name)
	{
		return CompareSignatures (name, name);
	}

	bool CompareSignatures (string name1, string name2)
	{
		return MethodReferenceComparer.AreSignaturesEqual (GetMethod (_class1, name1), GetMethod (_class2, name2), TypeComparisonMode.SignatureOnly);
	}

	static MethodDefinition GetMethod (TypeDefinition type, string name)
	{
		return type.Methods.Single (m => m.Name == name);
	}

	class GenericClass<T> {

	}

	class Class1 {

		void MethodWithNoParametersOrReturn () {}
		void GenericMethodWithNoParametersOrReturn<T> () {}
		static void StaticMethodWithNoParametersOrReturn () {}
		static void GenericStaticMethodWithNoParametersOrReturn<T> () {}

		void MethodWithIntParameterAndVoidReturn (int a) {}

		void GenericMethodWithGenericParameter<T> (T t) {}
		void GenericMethodWithGenericParameterArray<T> (T[] t) {}
		void GenericMethodWithByReferenceGenericParameter<T> (ref T a) {}
		void GenericMethodWithGenericInstanceGenericParameter<T> (GenericClass<T> a) {}
	}

	class Class2 {

		void MethodWithNoParametersOrReturn () {}
		void GenericMethodWithNoParametersOrReturn<T> () {}
		static void StaticMethodWithNoParametersOrReturn () {}
		static void GenericStaticMethodWithNoParametersOrReturn<T> () {}

		void MethodWithIntParameterAndVoidReturn (int a) {}

		void GenericMethodWithGenericParameter<T> (T t) {}
		void GenericMethodWithGenericParameterArray<T> (T[] t) {}
		void GenericMethodWithByReferenceGenericParameter<T> (ref T a) {}
		void GenericMethodWithGenericInstanceGenericParameter<T> (GenericClass<T> a) {}
	}
}
