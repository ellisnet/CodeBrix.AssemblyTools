//
// Smoke tests for CodeBrix.AssemblyTools P1 port of Mono.Cecil 0.11.5.
// These tests exercise the public API surface (module / type / method creation,
// IL emission, metadata tokens, opcodes, collections) without depending on the
// full upstream test-fixture infrastructure (BaseTestFixture, CompilationService,
// Formatter, etc.) -- that gets ported in later phases.
//

using System;
using System.IO;
using System.Linq;
using CodeBrix.AssemblyTools;
using CodeBrix.AssemblyTools.Cil;
using CodeBrix.AssemblyTools.Collections.Generic;
using CodeBrix.AssemblyTools.Metadata;
using SilverAssertions;
using Xunit;

namespace CodeBrix.AssemblyTools.Tests;

public class ModuleDefinitionTests
{
	[Fact]
	public void CreateModule_dll_has_expected_name_and_kind()
	{
		//Arrange, Act
		var module = ModuleDefinition.CreateModule("Test.dll", ModuleKind.Dll);

		//Assert
		module.Name.Should().Be("Test.dll");
		module.Kind.Should().Be(ModuleKind.Dll);
		module.Assembly.Name.Name.Should().Be("Test");
	}

	[Fact]
	public void CreateModule_exe_strips_extension_for_assembly_name()
	{
		var module = ModuleDefinition.CreateModule("Test.exe", ModuleKind.Console);

		module.Assembly.Name.Name.Should().Be("Test");
		module.Kind.Should().Be(ModuleKind.Console);
	}

	[Fact]
	public void CreateModule_exposes_core_library_reference()
		=> ModuleDefinition.CreateModule("Test.dll", ModuleKind.Dll)
			.TypeSystem.CoreLibrary.Should().NotBeNull();

	[Fact]
	public void CreateModule_has_entry_points_null_initially()
		=> ModuleDefinition.CreateModule("Test.dll", ModuleKind.Dll).EntryPoint.Should().BeNull();

	[Fact]
	public void ModuleDefinition_mvid_is_non_empty()
		=> ModuleDefinition.CreateModule("Test.dll", ModuleKind.Dll).Mvid.Should().NotBe(Guid.Empty);

	[Fact]
	public void Dispose_does_not_throw()
	{
		//Arrange
		var module = ModuleDefinition.CreateModule("Test.dll", ModuleKind.Dll);

		//Act, Assert
		Action act = () => module.Dispose();
		act.Should().NotThrow();
	}
}

public class AssemblyDefinitionTests
{
	[Fact]
	public void CreateAssembly_sets_name_and_main_module()
	{
		//Arrange, Act
		var assembly = AssemblyDefinition.CreateAssembly(
			new AssemblyNameDefinition("Widget", new Version(1, 2, 3, 4)),
			"Widget.dll",
			ModuleKind.Dll);

		//Assert
		assembly.Name.Name.Should().Be("Widget");
		assembly.Name.Version.Should().Be(new Version(1, 2, 3, 4));
		assembly.MainModule.Should().NotBeNull();
		assembly.MainModule.Name.Should().Be("Widget.dll");
		assembly.FullName.Should().StartWith("Widget, Version=1.2.3.4");
	}

	[Fact]
	public void AssemblyNameDefinition_metadata_token_is_assembly_token_1()
		=> AssemblyDefinition
			.CreateAssembly(new AssemblyNameDefinition("A", new Version(0, 0)), "A.dll", ModuleKind.Dll)
			.MetadataToken.Should().Be(new MetadataToken(TokenType.Assembly, 1));
}

public class TypeSystemTests
{
	[Fact]
	public void TypeSystem_primitive_references_are_not_null()
	{
		//Arrange
		var module = ModuleDefinition.CreateModule("Test.dll", ModuleKind.Dll);
		var ts = module.TypeSystem;

		//Assert
		ts.Boolean.Should().NotBeNull();
		ts.Byte.Should().NotBeNull();
		ts.Int32.Should().NotBeNull();
		ts.Int64.Should().NotBeNull();
		ts.Single.Should().NotBeNull();
		ts.Double.Should().NotBeNull();
		ts.Object.Should().NotBeNull();
		ts.String.Should().NotBeNull();
		ts.Void.Should().NotBeNull();
		ts.Int32.FullName.Should().Be("System.Int32");
	}
}

public class ILProcessorTests
{
	[Fact]
	public void Emit_adds_instructions_to_body()
	{
		//Arrange
		var module = ModuleDefinition.CreateModule("Test.dll", ModuleKind.Dll);
		var type = new TypeDefinition("", "Foo", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object);
		var method = new MethodDefinition("Bar",
			MethodAttributes.Public | MethodAttributes.Static,
			module.TypeSystem.Int32);
		type.Methods.Add(method);
		module.Types.Add(type);

		//Act
		var il = method.Body.GetILProcessor();
		il.Emit(OpCodes.Ldc_I4, 42);
		il.Emit(OpCodes.Ret);

		//Assert
		method.Body.Instructions.Should().HaveCount(2);
		method.Body.Instructions[0].OpCode.Should().Be(OpCodes.Ldc_I4);
		method.Body.Instructions[0].Operand.Should().Be(42);
		method.Body.Instructions[1].OpCode.Should().Be(OpCodes.Ret);
	}

	[Fact]
	public void Create_then_Append_inserts_at_tail()
	{
		var module = ModuleDefinition.CreateModule("Test.dll", ModuleKind.Dll);
		var type = new TypeDefinition("", "Foo", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object);
		var method = new MethodDefinition("Bar", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
		type.Methods.Add(method);
		module.Types.Add(type);

		var il = method.Body.GetILProcessor();
		var first = il.Create(OpCodes.Nop);
		var second = il.Create(OpCodes.Ret);
		il.Append(first);
		il.Append(second);

		method.Body.Instructions.Should().ContainInOrder(first, second);
	}
}

public class OpCodesTests
{
	[Fact]
	public void Nop_has_single_byte_encoding()
	{
		OpCodes.Nop.Op1.Should().Be(0xff);
		OpCodes.Nop.Op2.Should().Be(0x00);
		OpCodes.Nop.Size.Should().Be(1);
	}

	[Fact]
	public void Ret_is_return()
		=> OpCodes.Ret.FlowControl.Should().Be(FlowControl.Return);

	[Theory]
	[InlineData("Add")]
	[InlineData("Ldarg_0")]
	[InlineData("Ret")]
	[InlineData("Nop")]
	[InlineData("Ldc_I4")]
	public void Well_known_opcode_names_resolve(string name)
	{
		//Arrange, Act
		var field = typeof(OpCodes).GetField(name);

		//Assert
		field.Should().NotBeNull();
		var value = field.GetValue(null);
		value.Should().NotBeNull();
	}
}

public class MetadataTokenTests
{
	[Fact]
	public void MetadataToken_zero_is_empty()
		=> new MetadataToken(0u).ToUInt32().Should().Be(0u);

	[Fact]
	public void MetadataToken_encodes_type_and_rid()
	{
		//Arrange, Act
		var token = new MetadataToken(TokenType.TypeDef, 5);

		//Assert
		token.TokenType.Should().Be(TokenType.TypeDef);
		token.RID.Should().Be(5u);
	}

	[Fact]
	public void MetadataToken_equality_matches_when_identical()
	{
		var a = new MetadataToken(TokenType.Method, 7);
		var b = new MetadataToken(TokenType.Method, 7);
		(a == b).Should().BeTrue();
		a.GetHashCode().Should().Be(b.GetHashCode());
	}
}

public class CollectionTests
{
	[Fact]
	public void Collection_add_grows_and_indexer_works()
	{
		//Arrange
		var collection = new Collection<string>();

		//Act
		collection.Add("a");
		collection.Add("b");

		//Assert
		collection.Count.Should().Be(2);
		collection[0].Should().Be("a");
		collection[1].Should().Be("b");
	}

	[Fact]
	public void Collection_remove_shifts_elements()
	{
		//Arrange
		var collection = new Collection<string> { "a", "b", "c" };

		//Act
		var removed = collection.Remove("b");

		//Assert
		removed.Should().BeTrue();
		collection.Count.Should().Be(2);
		collection[0].Should().Be("a");
		collection[1].Should().Be("c");
	}

	[Fact]
	public void Collection_enumerates_in_order()
		=> new Collection<int> { 1, 2, 3 }.ToArray().Should().ContainInOrder(1, 2, 3);
}

public class RoundTripTests
{
	[Fact]
	public void Write_then_ReadModule_preserves_type()
	{
		//Arrange
		var tempPath = Path.Combine(Path.GetTempPath(), $"codebrix-roundtrip-{Guid.NewGuid():N}.dll");

		try
		{
			var module = ModuleDefinition.CreateModule(Path.GetFileName(tempPath), ModuleKind.Dll);
			var type = new TypeDefinition("", "Foo", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object);
			module.Types.Add(type);

			//Act
			module.Write(tempPath);
			using var reread = ModuleDefinition.ReadModule(tempPath);

			//Assert
			reread.Types.Any(t => t.Name == "Foo").Should().BeTrue();
		}
		finally
		{
			if (File.Exists(tempPath))
				File.Delete(tempPath);
		}
	}

	[Fact]
	public void ReadModule_on_self_finds_SmokeTests_assembly()
	{
		//Arrange
		var selfPath = typeof(ModuleDefinitionTests).Assembly.Location;

		//Act
		using var module = ModuleDefinition.ReadModule(selfPath);

		//Assert
		module.Assembly.Name.Name.Should().Be("CodeBrix.AssemblyTools.Tests");
		module.GetTypes().Any(t => t.Name == "ModuleDefinitionTests").Should().BeTrue();
	}
}
