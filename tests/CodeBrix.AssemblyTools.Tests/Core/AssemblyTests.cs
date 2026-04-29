using System;

using CodeBrix.AssemblyTools;
using CodeBrix.AssemblyTools.Cil;
using Xunit;
using SilverAssertions;
namespace CodeBrix.AssemblyTools.Tests.Core; //was previously: Mono.Cecil.Tests;
public class AssemblyTests : BaseTestFixture {

	[Fact]
	public void Name ()
	{
		TestModule ("hello.exe", module => {
			var name = module.Assembly.Name;

			Assert.NotNull (name);

			(name.Name).Should().Be("hello");
			(name.Culture).Should().Be("");
			(name.Version).Should().Be(new Version (0, 0, 0, 0));
			(name.HashAlgorithm).Should().Be(AssemblyHashAlgorithm.SHA1);
		});
	}

	[Fact]
	public void ParseLowerCaseNameParts ()
	{
		var name = AssemblyNameReference.Parse ("Foo, version=2.0.0.0, culture=fr-FR");
		(name.Name).Should().Be("Foo");
		(name.Version.Major).Should().Be(2);
		(name.Version.Minor).Should().Be(0);
		(name.Culture).Should().Be("fr-FR");
	}

	[Fact]
	public void ZeroVersion ()
	{
		var name = new AssemblyNameReference ("Foo", null);
		(name.Version.ToString (fieldCount: 4)).Should().Be("0.0.0.0");
		(name.FullName).Should().Be("Foo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

		name = new AssemblyNameReference ("Foo", new Version (0, 0, 0, 0));
		(name.Version.ToString (fieldCount: 4)).Should().Be("0.0.0.0");
		(name.FullName).Should().Be("Foo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
	}

	[Fact]
	public void NoBuildOrMajor ()
	{
		var name = new AssemblyNameReference ("Foo", new Version (0, 0));
		(name.FullName).Should().Be("Foo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

		name = new AssemblyNameReference ("Foo", new Version (0, 0, 0));
		(name.FullName).Should().Be("Foo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
	}

	[Fact]
	public void Retargetable ()
	{
		if (Platform.OnCoreClr)
			return;

		TestModule ("RetargetableExample.dll", module => {
			var type = module.Types [1];
			var property = type.Properties [0];
			var attribute = property.CustomAttributes [0];

			var argumentType = ((CustomAttributeArgument) attribute.ConstructorArguments [0].Value).Type;
			var reference = (AssemblyNameReference) argumentType.Scope;

			(reference.FullName).Should().Be("System.Data, Version=3.5.0.0, Culture=neutral, PublicKeyToken=969db8053d3322ac, Retargetable=Yes");
		}, verify: !Platform.OnMono);
	}

	[Fact]
	public void SystemRuntime ()
	{
		if (Platform.OnCoreClr)
			return;

		TestModule ("System.Runtime.dll", module => {
			(module.Assembly.Name.Name).Should().Be("System.Runtime");
			(module.AssemblyReferences.Count).Should().Be(1);
			(module.TypeSystem.CoreLibrary).Should().NotBe(module);
			(module.TypeSystem.CoreLibrary).Should().Be(module.AssemblyReferences [0]);
		}, verify: !Platform.OnMono);
	}

	[Fact]
	public void MismatchedLibraryAndSymbols ()
	{
		// SQLite-net.dll (from nuget) shiped with mismatched symbol files, but throwIfNoSymbol did not prevent it from throwing
		var parameters = new ReaderParameters {
			ReadSymbols = true,
			SymbolReaderProvider = new DefaultSymbolReaderProvider (throwIfNoSymbol: false),
			ThrowIfSymbolsAreNotMatching = false,
		};

		using (var module = GetResourceModule ("SQLite-net.dll", parameters)) {
			Assert.Null (module.SymbolReader);
		}
	}
}
