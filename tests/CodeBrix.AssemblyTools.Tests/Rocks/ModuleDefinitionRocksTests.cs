using System.Linq;
using Xunit;
using SilverAssertions;
using CodeBrix.AssemblyTools.Rocks;

namespace CodeBrix.AssemblyTools.Tests.Rocks; //was previously: Mono.Cecil.Tests;
public class ModuleDefinitionRocksTests : BaseTestFixture {

	[Fact]
	public void GetAllTypesTest ()
	{
		TestCSharp ("Types.cs", module => {
			var sequence = new [] {
			module.GetType ("<Module>"),
			module.GetType ("Foo"),
			module.GetType ("Foo/Bar"),
			module.GetType ("Foo/Gazonk"),
			module.GetType ("Foo/Gazonk/Baz"),
			module.GetType ("Pan"),
		};

			Assert.True (sequence.SequenceEqual (module.GetAllTypes ()));
		});
	}
}
