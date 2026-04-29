using System;

using CodeBrix.AssemblyTools.Rocks;
using Xunit;
using SilverAssertions;
namespace CodeBrix.AssemblyTools.Tests.Rocks; //was previously: Mono.Cecil.Tests;
public class TypeReferenceRocksTests {

	[Fact]
	public void MakeArrayType ()
	{
		var @string = GetTypeReference (typeof (string));

		var string_array = @string.MakeArrayType ();

		Assert.IsAssignableFrom<ArrayType>(string_array);
		(string_array.Rank).Should().Be(1);
	}

	[Fact]
	public void MakeArrayTypeRank ()
	{
		var @string = GetTypeReference (typeof (string));

		var string_array = @string.MakeArrayType (3);

		Assert.IsAssignableFrom<ArrayType>(string_array);
		(string_array.Rank).Should().Be(3);
	}

	[Fact]
	public void MakePointerType ()
	{
		var @string = GetTypeReference (typeof (string));

		var string_ptr = @string.MakePointerType ();

		Assert.IsAssignableFrom<PointerType>(string_ptr);
	}

	[Fact]
	public void MakeByReferenceType ()
	{
		var @string = GetTypeReference (typeof (string));

		var string_byref = @string.MakeByReferenceType ();

		Assert.IsAssignableFrom<ByReferenceType>(string_byref);
	}

	class OptionalModifier {}

	[Fact]
	public void MakeOptionalModifierType ()
	{
		var @string = GetTypeReference (typeof (string));
		var modopt = GetTypeReference (typeof (OptionalModifier));

		var string_modopt = @string.MakeOptionalModifierType (modopt);

		Assert.IsAssignableFrom<OptionalModifierType>(string_modopt);
		(string_modopt.ModifierType).Should().Be(modopt);
	}

	class RequiredModifier { }

	[Fact]
	public void MakeRequiredModifierType ()
	{
		var @string = GetTypeReference (typeof (string));
		var modreq = GetTypeReference (typeof (RequiredModifierType));

		var string_modreq = @string.MakeRequiredModifierType (modreq);

		Assert.IsAssignableFrom<RequiredModifierType>(string_modreq);
		(string_modreq.ModifierType).Should().Be(modreq);
	}

	[Fact]
	public void MakePinnedType ()
	{
		var byte_array = GetTypeReference (typeof (byte []));

		var pinned_byte_array = byte_array.MakePinnedType ();

		Assert.IsAssignableFrom<PinnedType>(pinned_byte_array);
	}

	[Fact]
	public void MakeSentinelType ()
	{
		var @string = GetTypeReference (typeof (string));

		var string_sentinel = @string.MakeSentinelType ();

		Assert.IsAssignableFrom<SentinelType>(string_sentinel);
	}

	class Foo<T1, T2> {}

	[Fact]
	public void MakeGenericInstanceType ()
	{
		var foo = GetTypeReference (typeof (Foo<,>));
		var @string = GetTypeReference (typeof (string));
		var @int = GetTypeReference (typeof (int));

		var foo_string_int = foo.MakeGenericInstanceType (@string, @int);

		Assert.IsAssignableFrom<GenericInstanceType>(foo_string_int);
		(foo_string_int.GenericArguments.Count).Should().Be(2);
		(foo_string_int.GenericArguments [0]).Should().Be(@string);
		(foo_string_int.GenericArguments [1]).Should().Be(@int);
	}

	static TypeReference GetTypeReference (Type type)
	{
		return ModuleDefinition.ReadModule (typeof (TypeReferenceRocksTests).Module.FullyQualifiedName).ImportReference (type);
	}
}
