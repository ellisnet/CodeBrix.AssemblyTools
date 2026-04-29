using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using CodeBrix.AssemblyTools;
using Xunit;
using SilverAssertions;
namespace CodeBrix.AssemblyTools.Tests.Core; //was previously: Mono.Cecil.Tests;
public class ModuleTests : BaseTestFixture {

	[Fact]
	public void CreateModuleEscapesAssemblyName ()
	{
		var module = ModuleDefinition.CreateModule ("Test.dll", ModuleKind.Dll);
		(module.Assembly.Name.Name).Should().Be("Test");

		module = ModuleDefinition.CreateModule ("Test.exe", ModuleKind.Console);
		(module.Assembly.Name.Name).Should().Be("Test");
	}

	[Fact]
	public void SingleModule ()
	{
		TestModule ("hello.exe", module => {
			var assembly = module.Assembly;

			(assembly.Modules.Count).Should().Be(1);
			Assert.NotNull (assembly.MainModule);
		});
	}

	[Fact]
	public void EntryPoint ()
	{
		TestModule ("hello.exe", module => {
			var entry_point = module.EntryPoint;
			Assert.NotNull (entry_point);

			(entry_point.ToString ()).Should().Be("System.Void Program::Main()");

			module.EntryPoint = null;
			Assert.Null (module.EntryPoint);

			module.EntryPoint = entry_point;
			Assert.NotNull (module.EntryPoint);
		});
	}

	[Fact]
	public void MultiModules ()
	{
		IgnoreOnCoreClr ();

		TestModule("mma.exe", module => {
			var assembly = module.Assembly;

			(assembly.Modules.Count).Should().Be(3);

			(assembly.Modules [0].Name).Should().Be("mma.exe");
			(assembly.Modules [0].Kind).Should().Be(ModuleKind.Console);

			(assembly.Modules [1].Name).Should().Be("moda.netmodule");
			(assembly.Modules [1].Mvid.ToString ()).Should().Be("eedb4721-6c3e-4d9a-be30-49021121dd92");
			(assembly.Modules [1].Kind).Should().Be(ModuleKind.NetModule);

			(assembly.Modules [2].Name).Should().Be("modb.netmodule");
			(assembly.Modules [2].Mvid.ToString ()).Should().Be("46c5c577-11b2-4ea0-bb3c-3c71f1331dd0");
			(assembly.Modules [2].Kind).Should().Be(ModuleKind.NetModule);
		});
	}

	[Fact]
	public void ModuleInformation ()
	{
		TestModule ("hello.exe", module => {
			Assert.NotNull (module);

			(module.Name).Should().Be("hello.exe");
			(module.Mvid).Should().Be(new Guid ("C3BC2BD3-2576-4D00-A80E-465B5632415F"));
		});
	}

	[Fact]
	public void AssemblyReferences ()
	{
		TestModule ("hello.exe", module => {
			(module.AssemblyReferences.Count).Should().Be(1);

			var reference = module.AssemblyReferences [0];

			(reference.Name).Should().Be("mscorlib");
			(reference.Version).Should().Be(new Version (2, 0, 0, 0));
			(reference.PublicKeyToken).Should().Equal(new byte [] { 0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89 });
		});
	}

	[Fact]
	public void ModuleReferences ()
	{
		TestModule ("pinvoke.exe", module => {
			(module.ModuleReferences.Count).Should().Be(2);
			(module.ModuleReferences [0].Name).Should().Be("kernel32.dll");
			(module.ModuleReferences [1].Name).Should().Be("shell32.dll");
		});
	}

	[Fact]
	public void Types ()
	{
		TestModule ("hello.exe", module => {
			(module.Types.Count).Should().Be(2);
			(module.Types [0].FullName).Should().Be("<Module>");
			(module.GetType ("<Module>").FullName).Should().Be("<Module>");
			(module.Types [1].FullName).Should().Be("Program");
			(module.GetType ("Program").FullName).Should().Be("Program");
		});
	}

	[Fact]
	public void LinkedResource ()
	{
		TestModule ("libres.dll", module => {
			var resource = module.Resources.Where (res => res.Name == "linked.txt").First () as LinkedResource;
			Assert.NotNull (resource);

			(resource.Name).Should().Be("linked.txt");
			(resource.File).Should().Be("linked.txt");
			(resource.ResourceType).Should().Be(ResourceType.Linked);
			Assert.True (resource.IsPublic);
		});
	}

	[Fact]
	public void EmbeddedResource ()
	{
		TestModule ("libres.dll", module => {
			var resource = module.Resources.Where (res => res.Name == "embedded1.txt").First () as EmbeddedResource;
			Assert.NotNull (resource);

			(resource.Name).Should().Be("embedded1.txt");
			(resource.ResourceType).Should().Be(ResourceType.Embedded);
			Assert.True (resource.IsPublic);

			using (var reader = new StreamReader (resource.GetResourceStream ()))
			(reader.ReadToEnd ()).Should().Be("Hello");

			resource = module.Resources.Where (res => res.Name == "embedded2.txt").First () as EmbeddedResource;
			Assert.NotNull (resource);

			(resource.Name).Should().Be("embedded2.txt");
			(resource.ResourceType).Should().Be(ResourceType.Embedded);
			Assert.True (resource.IsPublic);

			using (var reader = new StreamReader (resource.GetResourceStream ()))
			(reader.ReadToEnd ()).Should().Be("World");
		});
	}

	[Fact]
	public void ExportedTypeFromNetModule ()
	{
		IgnoreOnCoreClr ();

		TestModule ("mma.exe", module => {
			Assert.True (module.HasExportedTypes);
			(module.ExportedTypes.Count).Should().Be(2);

			var exported_type = module.ExportedTypes [0];

			(exported_type.FullName).Should().Be("Module.A.Foo");
			(exported_type.Scope.Name).Should().Be("moda.netmodule");

			exported_type = module.ExportedTypes [1];

			(exported_type.FullName).Should().Be("Module.B.Baz");
			(exported_type.Scope.Name).Should().Be("modb.netmodule");
		});
	}

	[Fact]
	public void NestedTypeForwarder ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			Assert.True (module.HasExportedTypes);
			(module.ExportedTypes.Count).Should().Be(2);

			var exported_type = module.ExportedTypes [0];

			(exported_type.FullName).Should().Be("System.Diagnostics.DebuggableAttribute");
			(exported_type.Scope.Name).Should().Be(Platform.OnCoreClr ? "System.Private.CoreLib" : "mscorlib");
			Assert.True (exported_type.IsForwarder);

			var nested_exported_type = module.ExportedTypes [1];

			(nested_exported_type.FullName).Should().Be("System.Diagnostics.DebuggableAttribute/DebuggingModes");
			(nested_exported_type.DeclaringType).Should().Be(exported_type);
			(nested_exported_type.Scope.Name).Should().Be(Platform.OnCoreClr ? "System.Private.CoreLib" : "mscorlib");
		});
	}

	[Fact]
	public void HasTypeReference ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			Assert.True (module.HasTypeReference ("System.Attribute"));
			Assert.True (module.HasTypeReference (Platform.OnCoreClr ? "System.Private.CoreLib" : "mscorlib", "System.Attribute"));

			Assert.False (module.HasTypeReference ("System.Core", "System.Attribute"));
			Assert.False (module.HasTypeReference ("System.Linq.Enumerable"));
		});
	}

	[Fact]
	public void Win32FileVersion ()
	{
		IgnoreOnCoreClr ();

		TestModule ("libhello.dll", module => {
			var version = FileVersionInfo.GetVersionInfo (module.FileName);

			(version.FileVersion).Should().Be("0.0.0.0");
		});
	}

	[Fact]
	public void ModuleWithoutBlob ()
	{
		TestModule ("noblob.dll", module => {
			Assert.Null (module.Image.BlobHeap);
		});
	}

	[Fact]
	public void MixedModeModule ()
	{
		using (var module = GetResourceModule ("cppcli.dll")) {
			(module.ModuleReferences.Count).Should().Be(1);
			(module.ModuleReferences [0].Name).Should().Be(string.Empty);
		}
	}

	[Fact]
	public void OpenIrrelevantFile ()
	{
		Assert.Throws<BadImageFormatException> (() => GetResourceModule ("text_file.txt"));
	}

	[Fact]
	public void GetTypeNamespacePlusName ()
	{
		using (var module = GetResourceModule ("moda.netmodule")) {
			var type = module.GetType ("Module.A", "Foo");
			Assert.NotNull (type);
		}
	}

	[Fact]
	public void GetNonExistentTypeRuntimeName ()
	{
		using (var module = GetResourceModule ("libhello.dll")) {
			var type = module.GetType ("DoesNotExist", runtimeName: true);
			Assert.Null (type);
		}
	}

	[Fact]
	public void OpenModuleImmediate ()
	{
		using (var module = GetResourceModule ("hello.exe", ReadingMode.Immediate)) {
			(module.ReadingMode).Should().Be(ReadingMode.Immediate);
		}
	}

	[Fact]
	public void OpenModuleDeferred ()
	{
		using (var module = GetResourceModule ("hello.exe", ReadingMode.Deferred)) {
			(module.ReadingMode).Should().Be(ReadingMode.Deferred);
		}
	}

	
	[Fact]
	public void OpenModuleDeferredAndThenPerformImmediateRead ()
	{
		using (var module = GetResourceModule ("hello.exe", ReadingMode.Deferred)) {
			(module.ReadingMode).Should().Be(ReadingMode.Deferred);
			module.ImmediateRead ();
			(module.ReadingMode).Should().Be(ReadingMode.Immediate);
		}
	}
	
	[Fact]
	public void ImmediateReadDoesNothingForModuleWithNoImage ()
	{
		using (var module = new ModuleDefinition ()) {
			var initialReadingMode = module.ReadingMode;
			module.ImmediateRead ();
			(module.ReadingMode).Should().Be(initialReadingMode);
		}
	}

	[Fact]
	public void OwnedStreamModuleFileName ()
	{
		var path = GetAssemblyResourcePath ("hello.exe");
		using (var file = File.Open (path, FileMode.Open))
		{
			using (var module = ModuleDefinition.ReadModule (file))
			{
				Assert.NotNull (module.FileName);
				Assert.NotEmpty (module.FileName);
				(module.FileName).Should().Be(path);
			}
		}
	}

	[Fact]
	public void ReadAndWriteFile ()
	{
		var path = Path.GetTempFileName ();

		var original = ModuleDefinition.CreateModule ("FooFoo", ModuleKind.Dll);
		var type = new TypeDefinition ("Foo", "Foo", TypeAttributes.Abstract | TypeAttributes.Sealed);
		original.Types.Add (type);
		original.Write (path);

		using (var module = ModuleDefinition.ReadModule (path, new ReaderParameters { ReadWrite = true })) {
			module.Write ();
		}

		using (var module = ModuleDefinition.ReadModule (path))
			(module.Types [1].FullName).Should().Be("Foo.Foo");
	}

	[Fact]
	public void ExceptionInWriteDoesNotKeepLockOnFile ()
	{
		var path = Path.GetTempFileName ();

		var module = ModuleDefinition.CreateModule ("FooFoo", ModuleKind.Dll);
		// Mixed mode module that Cecil can not write
		module.Attributes = (ModuleAttributes) 0;

		Assert.Throws<NotSupportedException>(() => module.Write (path));

		// Ensure you can still delete the file
		File.Delete (path);
	}
}
