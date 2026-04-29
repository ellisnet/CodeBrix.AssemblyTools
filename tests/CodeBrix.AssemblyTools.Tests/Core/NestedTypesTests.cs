using System;

using CodeBrix.AssemblyTools;
using Xunit;
using SilverAssertions;
namespace CodeBrix.AssemblyTools.Tests.Core; //was previously: Mono.Cecil.Tests;
public class NestedTypesTests : BaseTestFixture {

	[Fact]
	public void NestedTypes ()
	{
		TestCSharp ("NestedTypes.cs", module => {
			var foo = module.GetType ("Foo");

			(foo.Name).Should().Be("Foo");
			(foo.FullName).Should().Be("Foo");
			(foo.Module).Should().Be(module);
			(foo.NestedTypes.Count).Should().Be(1);

			var bar = foo.NestedTypes [0];

			(bar.Name).Should().Be("Bar");
			(bar.FullName).Should().Be("Foo/Bar");
			(bar.Module).Should().Be(module);
			(bar.NestedTypes.Count).Should().Be(1);

			var baz = bar.NestedTypes [0];

			(baz.Name).Should().Be("Baz");
			(baz.FullName).Should().Be("Foo/Bar/Baz");
			(baz.Module).Should().Be(module);
		});
	}

	[Fact]
	public void DirectNestedType ()
	{
		TestCSharp ("NestedTypes.cs", module => {
			var bingo = module.GetType ("Bingo");
			var get_fuel = bingo.GetMethod ("GetFuel");

			(get_fuel.ReturnType.FullName).Should().Be("Bingo/Fuel");
		});
	}

	[Fact]
	public void NestedTypeWithOwnNamespace ()
	{
		TestModule ("bug-185.dll", module => {
			var foo = module.GetType ("Foo");
			var foo_child = foo.NestedTypes [0];

			(foo_child.Namespace).Should().Be("<IFoo<System.Byte[]>");
			(foo_child.Name).Should().Be("Do>d__0");

			(foo_child.FullName).Should().Be("Foo/<IFoo<System.Byte[]>.Do>d__0");
		});
	}

	[Fact]
	public void NestedTypeFullName ()
	{
		var foo = new TypeDefinition (null, "Foo", TypeAttributes.Class);
		var bar = new TypeDefinition (null, "Bar", TypeAttributes.Class);
		var baz = new TypeDefinition (null, "Baz", TypeAttributes.Class);

		foo.NestedTypes.Add (bar);
		bar.NestedTypes.Add (baz);

		(baz.FullName).Should().Be("Foo/Bar/Baz");

		foo.Namespace = "Change";

		(bar.FullName).Should().Be("Change.Foo/Bar");
		(baz.FullName).Should().Be("Change.Foo/Bar/Baz");

		bar.Namespace = "AnotherChange";

		(bar.FullName).Should().Be("Change.Foo/AnotherChange.Bar");
		(baz.FullName).Should().Be("Change.Foo/AnotherChange.Bar/Baz");

		foo.Name = "FooFoo";

		(bar.FullName).Should().Be("Change.FooFoo/AnotherChange.Bar");
		(baz.FullName).Should().Be("Change.FooFoo/AnotherChange.Bar/Baz");
	}
}
