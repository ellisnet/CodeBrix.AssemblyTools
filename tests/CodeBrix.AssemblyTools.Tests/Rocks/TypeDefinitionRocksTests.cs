using System;
using System.Collections.Generic;
using System.Linq;
using CodeBrix.AssemblyTools.Rocks;
using Xunit;
using SilverAssertions;
namespace CodeBrix.AssemblyTools.Tests.Rocks; //was previously: Mono.Cecil.Tests;
public class TypeDefinitionRocksTests {

	class Foo {

		static Foo ()
		{
		}

		public Foo (int a)
		{
		}

		public Foo (int a, string s)
		{
		}

		public static void Bar ()
		{
		}

		void Baz ()
		{
		}
	}

	[Fact]
	public void GetConstructors ()
	{
		var foo = typeof (Foo).ToDefinition ();
		var ctors = foo.GetConstructors ().Select (ctor => ctor.FullName);

		var expected = new [] {
			// was previously: "System.Void Mono.Cecil.Tests.TypeDefinitionRocksTests/Foo::.cctor()",
			"System.Void CodeBrix.AssemblyTools.Tests.Rocks.TypeDefinitionRocksTests/Foo::.cctor()",
			// was previously: "System.Void Mono.Cecil.Tests.TypeDefinitionRocksTests/Foo::.ctor(System.Int32)",
			"System.Void CodeBrix.AssemblyTools.Tests.Rocks.TypeDefinitionRocksTests/Foo::.ctor(System.Int32)",
			// was previously: "System.Void Mono.Cecil.Tests.TypeDefinitionRocksTests/Foo::.ctor(System.Int32,System.String)",
			"System.Void CodeBrix.AssemblyTools.Tests.Rocks.TypeDefinitionRocksTests/Foo::.ctor(System.Int32,System.String)",
		};

		AssertSet (expected, ctors);
	}

	static void AssertSet<T> (IEnumerable<T> expected, IEnumerable<T> actual)
	{
		Assert.False (expected.Except (actual).Any ());
		Assert.True (expected.Intersect (actual).SequenceEqual (expected));
	}

	[Fact]
	public void GetStaticConstructor ()
	{
		var foo = typeof (Foo).ToDefinition ();
		var cctor = foo.GetStaticConstructor ();

		Assert.NotNull (cctor);
		// was previously: (cctor.FullName).Should().Be("System.Void Mono.Cecil.Tests.TypeDefinitionRocksTests/Foo::.cctor()");
		(cctor.FullName).Should().Be("System.Void CodeBrix.AssemblyTools.Tests.Rocks.TypeDefinitionRocksTests/Foo::.cctor()");
	}

	[Fact]
	public void GetMethods ()
	{
		var foo = typeof (Foo).ToDefinition ();
		var methods = foo.GetMethods ().ToArray ();

		(methods.Length).Should().Be(2);
		// was previously: (methods [0].FullName).Should().Be("System.Void Mono.Cecil.Tests.TypeDefinitionRocksTests/Foo::Bar()");
		(methods [0].FullName).Should().Be("System.Void CodeBrix.AssemblyTools.Tests.Rocks.TypeDefinitionRocksTests/Foo::Bar()");
		// was previously: (methods [1].FullName).Should().Be("System.Void Mono.Cecil.Tests.TypeDefinitionRocksTests/Foo::Baz()");
		(methods [1].FullName).Should().Be("System.Void CodeBrix.AssemblyTools.Tests.Rocks.TypeDefinitionRocksTests/Foo::Baz()");
	}

	enum Pan : byte {
		Pin,
		Pon,
	}

	[Fact]
	public void GetEnumUnderlyingType ()
	{
		var pan = typeof (Pan).ToDefinition ();

		Assert.NotNull (pan);
		Assert.True (pan.IsEnum);

		var underlying_type = pan.GetEnumUnderlyingType ();
		Assert.NotNull (underlying_type);

		(underlying_type.FullName).Should().Be("System.Byte");
	}
}
