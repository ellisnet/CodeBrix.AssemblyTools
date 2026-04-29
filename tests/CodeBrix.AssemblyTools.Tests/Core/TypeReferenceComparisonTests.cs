using System;
using System.Collections.Generic;
using CodeBrix.AssemblyTools;
using Xunit;
using SilverAssertions;
namespace CodeBrix.AssemblyTools.Tests.Core; //was previously: Mono.Cecil.Tests;
public class TypeReferenceComparisonTests : LoadAssemblyDefinitionForTestsBaseSimple {

	// Upstream NUnit ran this as a `[SetUp]` method before each test. xUnit
	// instantiates a fresh test-class instance per test, so the equivalent is
	// the constructor. Without this, `_mscorlib` would be null and every test
	// below would throw NullReferenceException inside TypeDefinitionUtils.
	public TypeReferenceComparisonTests ()
	{
		SetupAssemblyDefinitions (typeof (TypeReferenceComparisonTests).Assembly);
	}

	[Fact]
	public void TypeReferenceEqualsTypeDefinition ()
	{
		var typeDefinition = TypeDefinitionUtils.TypeDefinitionFor (typeof (Int32), _mscorlib);
		var typeReference = new TypeReference (typeDefinition.Namespace, typeDefinition.Name, typeDefinition.Module, typeDefinition.Scope);

		Assert.True(TypeReferenceEqualityComparer.AreEqual (typeDefinition, typeReference));
	}

	[Fact]
	public void GenericParametersFromTwoTypesAreNotEqual ()
	{
		var listDefinition = TypeDefinitionUtils.TypeDefinitionFor (typeof (List<>), _mscorlib);
		var stackDefinition = TypeDefinitionUtils.TypeDefinitionFor (typeof (Comparer<>), _mscorlib);

		Assert.False(TypeReferenceEqualityComparer.AreEqual (listDefinition.GenericParameters[0], stackDefinition.GenericParameters[0]));
	}

	[Fact]
	public void ArrayTypesDoNotMatchIfRankIsDifferent ()
	{
		var elementType = TypeDefinitionUtils.TypeDefinitionFor (typeof (Int32), _mscorlib);

		(TypeReferenceEqualityComparer.AreEqual (new ArrayType (elementType, 1), new ArrayType (elementType, 2))).Should().BeFalse("Two array types with different ranks match, which is not expected.");
	}

	[Fact]
	public void ArrayTypesDoNotMatchIfElementTypeIsDifferent ()
	{
		(TypeReferenceEqualityComparer.AreEqual (new ArrayType (TypeDefinitionUtils.TypeDefinitionFor (typeof (Int32), _mscorlib), 1), new ArrayType (TypeDefinitionUtils.TypeDefinitionFor (typeof (Int64), _mscorlib), 1))).Should().BeFalse("Two array types with different element types match, which is not expected.");
	}

	[Fact]
	public void ArrayTypesWithDifferentRanksToNotMatch ()
	{
		var elementType = TypeDefinitionUtils.TypeDefinitionFor (typeof (Int32), _mscorlib);

		(TypeReferenceEqualityComparer.AreEqual ( (TypeSpecification) new ArrayType (elementType, 1),  (TypeSpecification) new ArrayType (elementType, 2))).Should().BeFalse("Two type specifications that are array types with different ranks match, which is not expected.");
	}

	[Fact]
	public void GenericInstanceTypeFromTwoTypesAreNotEqual ()
	{
		var int32Definition = TypeDefinitionUtils.TypeDefinitionFor (typeof (Int32), _mscorlib);
		var listDefinition = TypeDefinitionUtils.TypeDefinitionFor (typeof (List<>), _mscorlib);
		var listGenericInstance = new GenericInstanceType (listDefinition);
		listGenericInstance.GenericArguments.Add (int32Definition);
		var stackDefinition = TypeDefinitionUtils.TypeDefinitionFor (typeof (Comparer<>), _mscorlib);
		var stackGenericInstance = new GenericInstanceType (stackDefinition);
		stackGenericInstance.GenericArguments.Add (int32Definition);

		Assert.False(TypeReferenceEqualityComparer.AreEqual (listGenericInstance, stackGenericInstance));
	}

	[Fact]
	public void GenericInstanceTypeForSameTypeIsEqual ()
	{
		var int32Definition = TypeDefinitionUtils.TypeDefinitionFor (typeof (Int32), _mscorlib);
		var listDefinition = TypeDefinitionUtils.TypeDefinitionFor (typeof (List<>), _mscorlib);
		var listGenericInstance = new GenericInstanceType (listDefinition);
		listGenericInstance.GenericArguments.Add (int32Definition);
		var listGenericInstance2 = new GenericInstanceType (listDefinition);
		listGenericInstance2.GenericArguments.Add (int32Definition);

		Assert.True(TypeReferenceEqualityComparer.AreEqual (listGenericInstance, listGenericInstance2));
	}
}
