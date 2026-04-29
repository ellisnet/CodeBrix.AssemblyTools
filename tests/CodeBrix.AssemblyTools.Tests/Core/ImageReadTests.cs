using System;
using System.IO;
using System.Linq;

using CodeBrix.AssemblyTools;
using CodeBrix.AssemblyTools.Cil;
using CodeBrix.AssemblyTools.PE;
using CodeBrix.AssemblyTools.Metadata;
using Xunit;
using SilverAssertions;
namespace CodeBrix.AssemblyTools.Tests.Core; //was previously: Mono.Cecil.Tests;
public class ImageReadTests : BaseTestFixture {

	[Fact]
	public void ImageSections ()
	{
		using (var image = GetResourceImage ("hello.exe")) {
			(image.Sections.Length).Should().Be(3);
			(image.Sections [0].Name).Should().Be(".text");
			(image.Sections [1].Name).Should().Be(".rsrc");
			(image.Sections [2].Name).Should().Be(".reloc");
		}
	}

	[Fact]
	public void ImageMetadataVersion ()
	{
		using (var image = GetResourceImage ("hello.exe"))
			(image.RuntimeVersion.ParseRuntime ()).Should().Be(TargetRuntime.Net_2_0);

		using (var image = GetResourceImage ("hello1.exe"))
			(image.RuntimeVersion.ParseRuntime ()).Should().Be(TargetRuntime.Net_1_1);
	}

	[Fact]
	public void ImageModuleKind ()
	{
		using (var image = GetResourceImage ("hello.exe"))
			(image.Kind).Should().Be(ModuleKind.Console);

		using (var image = GetResourceImage ("libhello.dll"))
			(image.Kind).Should().Be(ModuleKind.Dll);

		using (var image = GetResourceImage ("hellow.exe"))
			(image.Kind).Should().Be(ModuleKind.Windows);
	}

	[Fact]
	public void MetadataHeaps ()
	{
		using (var image = GetResourceImage ("hello.exe")) {
			Assert.NotNull (image.TableHeap);

			Assert.NotNull (image.StringHeap);
			(image.StringHeap.Read (0)).Should().Be(string.Empty);
			(image.StringHeap.Read (1)).Should().Be("<Module>");

			Assert.NotNull (image.UserStringHeap);
			(image.UserStringHeap.Read (0)).Should().Be(string.Empty);
			(image.UserStringHeap.Read (1)).Should().Be("Hello Cecil World !");

			Assert.NotNull (image.GuidHeap);
			(image.GuidHeap.Read (0)).Should().Be(new Guid ());
			(image.GuidHeap.Read (1)).Should().Be(new Guid ("C3BC2BD3-2576-4D00-A80E-465B5632415F"));

			Assert.NotNull (image.BlobHeap);
			(image.BlobHeap.Read (0)).Should().Equal(new byte [0]);
		}
	}

	[Fact]
	public void TablesHeap ()
	{
		using (var image = GetResourceImage ("hello.exe")) {
			var heap = image.TableHeap;

			Assert.NotNull (heap);

			(heap [Table.Module].Length).Should().Be(1);
			(heap [Table.TypeRef].Length).Should().Be(4);
			(heap [Table.TypeDef].Length).Should().Be(2);
			(heap [Table.Field].Length).Should().Be(0);
			(heap [Table.Method].Length).Should().Be(2);
			(heap [Table.MemberRef].Length).Should().Be(4);
			(heap [Table.CustomAttribute].Length).Should().Be(2);
			(heap [Table.Assembly].Length).Should().Be(1);
			(heap [Table.AssemblyRef].Length).Should().Be(1);
		}
	}

	[Fact]
	public void X64Module ()
	{
		TestModule ("hello.x64.exe", module => {
			(module.Image.Architecture).Should().Be(TargetArchitecture.AMD64);
			(module.Image.Attributes).Should().Be(ModuleAttributes.ILOnly);
		}, verify: !Platform.OnMono);
	}

	[Fact]
	public void AnyCPU32BitPreferred ()
	{
		TestModule ("anycpu32bitpreferred.exe", module => {
			(module.Image.Characteristics & 0x0020).Should().NotBe(0);
		});
	}

	[Fact]
	public void X64ModuleTextOnlySection ()
	{
		TestModule ("hello.textonly.x64.exe", module => {
			(module.Image.Architecture).Should().Be(TargetArchitecture.AMD64);
			(module.Image.Attributes).Should().Be(ModuleAttributes.ILOnly);
		}, verify: !Platform.OnMono);
	}

	[Fact]
	public void IA64Module ()
	{
		TestModule ("hello.ia64.exe", module => {
			(module.Image.Architecture).Should().Be(TargetArchitecture.IA64);
			(module.Image.Attributes).Should().Be(ModuleAttributes.ILOnly);
		}, verify: !Platform.OnMono);
	}

	[Fact]
	public void X86Module ()
	{
		TestModule ("hello.x86.exe", module => {
			(module.Image.Architecture).Should().Be(TargetArchitecture.I386);
			(module.Image.Attributes).Should().Be(ModuleAttributes.ILOnly | ModuleAttributes.Required32Bit);
		});
	}

	[Fact]
	public void AnyCpuModule ()
	{
		TestModule ("hello.anycpu.exe", module => {
			(module.Image.Architecture).Should().Be(TargetArchitecture.I386);
			(module.Image.Attributes).Should().Be(ModuleAttributes.ILOnly);
		});
	}

	[Fact]
	public void DelaySignedAssembly ()
	{
		TestModule ("delay-signed.dll", module => {
			Assert.NotNull (module.Assembly.Name.PublicKey);
			(module.Assembly.Name.PublicKey.Length).Should().NotBe(0);
			(module.Attributes & ModuleAttributes.StrongNameSigned).Should().NotBe(ModuleAttributes.StrongNameSigned);
			(module.Image.StrongName.VirtualAddress).Should().NotBe(0);
			(module.Image.StrongName.Size).Should().NotBe(0);
		});
	}

	[Fact]
	public void WindowsPhoneNonSignedAssembly ()
	{
		TestModule ("wp7.dll", module => {
			(module.Assembly.Name.PublicKey.Length).Should().Be(0);
			(module.Attributes & ModuleAttributes.StrongNameSigned).Should().NotBe(ModuleAttributes.StrongNameSigned);
			(module.Image.StrongName.VirtualAddress).Should().Be(0);
			(module.Image.StrongName.Size).Should().Be(0);
		}, verify: false);
	}

	[Fact]
	public void MetroAssembly ()
	{
		if (Platform.OnMono)
			return;

		TestModule ("metro.exe", module => {
			(module.Characteristics & ModuleCharacteristics.AppContainer).Should().Be(ModuleCharacteristics.AppContainer);
		}, verify: false);
	}

	[Fact]
	public void DeterministicAssembly ()
	{
		TestModule ("Deterministic.dll", module => {
			Assert.True (module.HasDebugHeader);

			var header = module.GetDebugHeader ();

			(header.Entries.Length).Should().Be(1);
			Assert.Contains(header.Entries, e => e.Directory.Type == ImageDebugType.Deterministic);
		});
	}

	[Fact]
	public void Net471TargetingAssembly ()
	{
		TestModule ("net471.exe", module => {
			(module.Image.SubSystemMajor).Should().Be(6);
			(module.Image.SubSystemMinor).Should().Be(0);
		});
	}

	[Fact]
	public void LocallyScopedConstantArray ()
	{
		TestModule ("LocallyScopedConstantArray.dll", module => {
			Assert.True (module.HasDebugHeader);
			var method = module.Types
				.Single (x => x.Name == "TestClass")
				.Methods
				.Single (x => x.Name == "TestMethod");
			var debugInformation = method.DebugInformation;
			Assert.Null (debugInformation.Scope.Constants.Single ().Value);
		}, symbolReaderProvider: typeof (PortablePdbReaderProvider), symbolWriterProvider: typeof (PortablePdbWriterProvider));
	}

	[Fact]
	public void ExternalPdbDeterministicAssembly ()
	{
		TestModule ("ExternalPdbDeterministic.dll", module => {
			Assert.True (module.HasDebugHeader);

			var header = module.GetDebugHeader ();

			Assert.True (header.Entries.Length >= 2);
			Assert.Contains(header.Entries, e => e.Directory.Type == ImageDebugType.CodeView);
			Assert.Contains(header.Entries, e => e.Directory.Type == ImageDebugType.Deterministic);

			// If read directly from a file the PdbChecksum may not be persent (in this test case it isn't)
			// but when written through Cecil it will always be there.
			if (header.Entries.Length > 2) {
				(header.Entries.Length).Should().Be(3);
				Assert.Contains(header.Entries, e => e.Directory.Type == ImageDebugType.PdbChecksum);
			}
		}, symbolReaderProvider: typeof (PortablePdbReaderProvider), symbolWriterProvider: typeof (PortablePdbWriterProvider));
	}

	[Fact]
	public void EmbeddedPdbDeterministicAssembly ()
	{
		TestModule ("EmbeddedPdbDeterministic.dll", module => {
			Assert.True (module.HasDebugHeader);

			var header = module.GetDebugHeader ();

			Assert.True (header.Entries.Length >= 3);
			Assert.Contains(header.Entries, e => e.Directory.Type == ImageDebugType.CodeView);
			Assert.Contains(header.Entries, e => e.Directory.Type == ImageDebugType.Deterministic);
			Assert.Contains(header.Entries, e => e.Directory.Type == ImageDebugType.EmbeddedPortablePdb);

			// If read directly from a file the PdbChecksum may not be persent (in this test case it isn't)
			// but when written through Cecil it will always be there.
			if (header.Entries.Length > 3) {
				(header.Entries.Length).Should().Be(4);
				Assert.Contains(header.Entries, e => e.Directory.Type == ImageDebugType.PdbChecksum);
			}
		}, symbolReaderProvider: typeof (EmbeddedPortablePdbReaderProvider), symbolWriterProvider: typeof (EmbeddedPortablePdbWriterProvider));
	}
}
