using System.Linq;
using Xunit;
using SilverAssertions;
using CodeBrix.AssemblyTools.Rocks;

namespace CodeBrix.AssemblyTools.Tests.Rocks; //was previously: Mono.Cecil.Tests;
public class MethodDefinitionRocksTests : BaseTestFixture {

	abstract class Foo {
		public abstract void DoFoo ();
		public abstract void DoBar ();
	}

	class Bar : Foo {
		public override void DoFoo ()
		{
		}

		public override void DoBar ()
		{
		}
	}

	class Baz : Bar {
		public override void DoFoo ()
		{
		}

		public virtual new void DoBar ()
		{
		}
	}

	[Fact]
	public void GetBaseMethod ()
	{
		var baz = typeof (Baz).ToDefinition ();
		var baz_dofoo = baz.GetMethod ("DoFoo");

		var @base = baz_dofoo.GetBaseMethod ();
		(@base.DeclaringType.Name).Should().Be("Bar");

		@base = @base.GetBaseMethod ();
		(@base.DeclaringType.Name).Should().Be("Foo");

		(@base.GetBaseMethod ()).Should().Be(@base);

		var new_dobar = baz.GetMethod ("DoBar");
		@base = new_dobar.GetBaseMethod();
		(@base.DeclaringType.Name).Should().Be("Baz");
	}

	[Fact]
	public void GetOriginalBaseMethod ()
	{
		var baz = typeof (Baz).ToDefinition ();
		var baz_dofoo = baz.GetMethod ("DoFoo");

		var @base = baz_dofoo.GetOriginalBaseMethod ();
		(@base.DeclaringType.Name).Should().Be("Foo");
	}
}
