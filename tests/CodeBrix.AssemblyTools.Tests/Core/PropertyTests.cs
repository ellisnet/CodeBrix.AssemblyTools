using System.Linq;
using Xunit;
using SilverAssertions;
namespace CodeBrix.AssemblyTools.Tests.Core; //was previously: Mono.Cecil.Tests;
public class PropertyTests : BaseTestFixture {

	[Fact]
	public void AbstractMethod ()
	{
		TestCSharp ("Properties.cs", module => {
			var type = module.GetType ("Foo");

			Assert.True (type.HasProperties);

			var properties = type.Properties;

			(properties.Count).Should().Be(3);

			var property = properties [0];

			Assert.NotNull (property);
			(property.Name).Should().Be("Bar");
			Assert.NotNull (property.PropertyType);
			(property.PropertyType.FullName).Should().Be("System.Int32");

			Assert.NotNull (property.GetMethod);
			(property.GetMethod.SemanticsAttributes).Should().Be(MethodSemanticsAttributes.Getter);
			Assert.Null (property.SetMethod);

			property = properties [1];

			Assert.NotNull (property);
			(property.Name).Should().Be("Baz");
			Assert.NotNull (property.PropertyType);
			(property.PropertyType.FullName).Should().Be("System.String");

			Assert.NotNull (property.GetMethod);
			(property.GetMethod.SemanticsAttributes).Should().Be(MethodSemanticsAttributes.Getter);
			Assert.NotNull (property.SetMethod);
			(property.SetMethod.SemanticsAttributes).Should().Be(MethodSemanticsAttributes.Setter);

			property = properties [2];

			Assert.NotNull (property);
			(property.Name).Should().Be("Gazonk");
			Assert.NotNull (property.PropertyType);
			(property.PropertyType.FullName).Should().Be("System.String");

			Assert.Null (property.GetMethod);
			Assert.NotNull (property.SetMethod);
			(property.SetMethod.SemanticsAttributes).Should().Be(MethodSemanticsAttributes.Setter);
		});
	}

	[Fact]
	public void OtherMethod ()
	{
		TestIL ("others.il", module => {
			var type = module.GetType ("Others");

			Assert.True (type.HasProperties);

			var properties = type.Properties;

			(properties.Count).Should().Be(1);

			var property = properties [0];

			Assert.NotNull (property);
			(property.Name).Should().Be("Context");
			Assert.NotNull (property.PropertyType);
			(property.PropertyType.FullName).Should().Be("System.String");

			Assert.True (property.HasOtherMethods);

			(property.OtherMethods.Count).Should().Be(2);

			var other = property.OtherMethods [0];
			(other.Name).Should().Be("let_Context");

			other = property.OtherMethods [1];
			(other.Name).Should().Be("bet_Context");
		});
	}

	[Fact]
	public void SetOnlyIndexer ()
	{
		TestCSharp ("Properties.cs", module => {
			var type = module.GetType ("Bar");
			var indexer = type.Properties.Where (property => property.Name == "Item").First ();

			var parameters = indexer.Parameters;

			(parameters.Count).Should().Be(2);
			(parameters [0].ParameterType.FullName).Should().Be("System.Int32");
			(parameters [1].ParameterType.FullName).Should().Be("System.String");
		});
	}

	[Fact]
	public void ReadSemanticsFirst ()
	{
		TestCSharp ("Properties.cs", module => {
			var type = module.GetType ("Baz");
			var setter = type.GetMethod ("set_Bingo");

			(setter.SemanticsAttributes).Should().Be(MethodSemanticsAttributes.Setter);

			var property = type.Properties.Where (p => p.Name == "Bingo").First ();

			(property.SetMethod).Should().Be(setter);
			(property.GetMethod).Should().Be(type.GetMethod ("get_Bingo"));
		});
	}

	[Fact]
	public void UnattachedProperty ()
	{
		var property = new PropertyDefinition ("Property", PropertyAttributes.None, typeof (int).ToDefinition ());

		Assert.Null (property.GetMethod);
	}
}
