using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using CodeBrix.AssemblyTools;
using CodeBrix.AssemblyTools.Cil;
using Xunit;
using SilverAssertions;
namespace CodeBrix.AssemblyTools.Tests.Core; //was previously: Mono.Cecil.Tests;
public class ResolveTests : BaseTestFixture {

	[Fact]
	public void StringEmpty ()
	{
		var string_empty = GetReference<Func<string>, FieldReference> (
			() => string.Empty);

		(string_empty.FullName).Should().Be("System.String System.String::Empty");

		var definition = string_empty.Resolve ();

		Assert.NotNull (definition);

		(definition.FullName).Should().Be("System.String System.String::Empty");
		(definition.Module.Assembly.Name.Name).Should().Be(Platform.OnCoreClr ? "System.Private.CoreLib" : "mscorlib");
	}

	delegate string GetSubstring (string str, int start, int length);

	[Fact]
	public void StringSubstring ()
	{
		var string_substring = GetReference<GetSubstring, MethodReference> (
			(s, start, length) => s.Substring (start, length));

		var definition = string_substring.Resolve ();

		Assert.NotNull (definition);

		(definition.FullName).Should().Be("System.String System.String::Substring(System.Int32,System.Int32)");
		(definition.Module.Assembly.Name.Name).Should().Be(Platform.OnCoreClr ? "System.Private.CoreLib" : "mscorlib");
	}

	[Fact]
	public void StringLength ()
	{
		var string_length = GetReference<Func<string, int>, MethodReference> (s => s.Length);

		var definition = string_length.Resolve ();

		Assert.NotNull (definition);

		(definition.Name).Should().Be("get_Length");
		(definition.DeclaringType.FullName).Should().Be("System.String");
		(definition.Module.Assembly.Name.Name).Should().Be(Platform.OnCoreClr ? "System.Private.CoreLib" : "mscorlib");
	}

	[Fact]
	public void ListOfStringAdd ()
	{
		var list_add = GetReference<Action<List<string>>, MethodReference> (
			list => list.Add ("coucou"));

		(list_add.FullName).Should().Be("System.Void System.Collections.Generic.List`1<System.String>::Add(!0)");

		var definition = list_add.Resolve ();

		Assert.NotNull (definition);

		(definition.FullName).Should().Be("System.Void System.Collections.Generic.List`1::Add(T)");
		(definition.Module.Assembly.Name.Name).Should().Be(Platform.OnCoreClr ? "System.Private.CoreLib" : "mscorlib");
	}

	[Fact]
	public void DictionaryOfStringTypeDefinitionTryGetValue ()
	{
		var try_get_value = GetReference<Func<Dictionary<string, TypeDefinition>, string, bool>, MethodReference> (
			(d, s) => {
				TypeDefinition type;
				return d.TryGetValue (s, out type);
			});

		// was previously: (try_get_value.FullName).Should().Be("System.Boolean System.Collections.Generic.Dictionary`2<System.String,Mono.Cecil.TypeDefinition>::TryGetValue(!0,!1&)");
		(try_get_value.FullName).Should().Be("System.Boolean System.Collections.Generic.Dictionary`2<System.String,CodeBrix.AssemblyTools.TypeDefinition>::TryGetValue(!0,!1&)");

		var definition = try_get_value.Resolve ();

		Assert.NotNull (definition);

		(definition.FullName).Should().Be("System.Boolean System.Collections.Generic.Dictionary`2::TryGetValue(TKey,TValue&)");
		(definition.Module.Assembly.Name.Name).Should().Be(Platform.OnCoreClr ? "System.Private.CoreLib" : "mscorlib");
	}

	class CustomResolver : DefaultAssemblyResolver {

		public void Register (AssemblyDefinition assembly)
		{
			this.RegisterAssembly (assembly);
			this.AddSearchDirectory (Path.GetDirectoryName (assembly.MainModule.FileName));
		}
	}

	[Fact]
	public void ExportedTypeFromModule ()
	{
		var resolver = new CustomResolver ();
		var parameters = new ReaderParameters { AssemblyResolver = resolver };
		var mma = GetResourceModule ("mma.exe", parameters);

		resolver.Register (mma.Assembly);

		using (var current_module = GetCurrentModule (parameters)) {
			var reference = new TypeReference ("Module.A", "Foo", current_module, AssemblyNameReference.Parse (mma.Assembly.FullName), false);

			var definition = reference.Resolve ();
			Assert.NotNull (definition);
			(definition.FullName).Should().Be("Module.A.Foo");
		}
	}

	[Fact]
	public void TypeForwarder ()
	{
		var resolver = new CustomResolver ();
		var parameters = new ReaderParameters { AssemblyResolver = resolver };

		var types = ModuleDefinition.ReadModule (
			CompilationService.CompileResource (GetCSharpResourcePath ("CustomAttributes.cs")),
			parameters);

		resolver.Register (types.Assembly);

		var current_module = GetCurrentModule (parameters);
		var reference = new TypeReference ("System.Diagnostics", "DebuggableAttribute", current_module, AssemblyNameReference.Parse (types.Assembly.FullName), false);

		var definition = reference.Resolve ();
		Assert.NotNull (definition);
		(definition.FullName).Should().Be("System.Diagnostics.DebuggableAttribute");
		(definition.Module.Assembly.Name.Name).Should().Be(Platform.OnCoreClr ? "System.Private.CoreLib" : "mscorlib");
	}

	[Fact]
	public void NestedTypeForwarder ()
	{
		var resolver = new CustomResolver ();
		var parameters = new ReaderParameters { AssemblyResolver = resolver };

		var types = ModuleDefinition.ReadModule (
			CompilationService.CompileResource (GetCSharpResourcePath ("CustomAttributes.cs")),
			parameters);

		resolver.Register (types.Assembly);

		var current_module = GetCurrentModule (parameters);
		var reference = new TypeReference ("", "DebuggingModes", current_module, null, true);
		reference.DeclaringType = new TypeReference ("System.Diagnostics", "DebuggableAttribute", current_module, AssemblyNameReference.Parse (types.Assembly.FullName), false);

		var definition = reference.Resolve ();
		Assert.NotNull (definition);
		(definition.FullName).Should().Be("System.Diagnostics.DebuggableAttribute/DebuggingModes");
		(definition.Module.Assembly.Name.Name).Should().Be(Platform.OnCoreClr ? "System.Private.CoreLib" : "mscorlib");
	}

	[Fact]
	public void RectangularArrayResolveGetMethod ()
	{
		var get_a_b = GetReference<Func<int[,], int>, MethodReference> (matrix => matrix [2, 2]);

		(get_a_b.Name).Should().Be("Get");
		Assert.NotNull (get_a_b.Module);
		Assert.Null (get_a_b.Resolve ());
	}

	[Fact]
	public void GenericRectangularArrayGetMethodInMemberReferences ()
	{
		using (var module = GetResourceModule ("FSharp.Core.dll")) {
			foreach (var member in module.GetMemberReferences ()) {
				if (!member.DeclaringType.IsArray)
					continue;

				Assert.Null (member.Resolve ());
			}
		}
	}

	[Fact]
	public void ResolveFunctionPointer ()
	{
		var module = GetResourceModule ("cppcli.dll");
		var global = module.GetType ("<Module>");
		var field = global.GetField ("__onexitbegin_app_domain");

		var type = field.FieldType as PointerType;
		Assert.NotNull(type);

		var fnptr = type.ElementType as FunctionPointerType;
		Assert.NotNull (fnptr);

		Assert.Null (fnptr.Resolve ());
	}

	[Fact]
	public void ResolveGenericParameter ()
	{
		// Fully-qualified upstream type reference; the porter's `using`-rewriter
		// misses inline qualified names like this one.
		var collection = typeof (CodeBrix.AssemblyTools.Collections.Generic.Collection<>).ToDefinition ();
		var parameter = collection.GenericParameters [0];

		Assert.NotNull (parameter);

		Assert.Null (parameter.Resolve ());
	}

	[Fact]
	public void ResolveNullVersionAssembly ()
	{
		var reference = AssemblyNameReference.Parse ("System.Core");
		reference.Version = null;

		var resolver = new DefaultAssemblyResolver ();
		Assert.NotNull (resolver.Resolve (reference));
	}

	[Fact]
	public void ResolvePortableClassLibraryReference ()
	{
		var resolver = new DefaultAssemblyResolver ();
		var parameters = new ReaderParameters { AssemblyResolver = resolver };
		var pcl = GetResourceModule ("PortableClassLibrary.dll", parameters);

		foreach (var reference in pcl.AssemblyReferences) {
			Assert.True (reference.IsRetargetable);
			var assembly = resolver.Resolve (reference);
			Assert.NotNull (assembly);

			if (!Platform.OnCoreClr)
				(assembly.Name.Version).Should().Be(typeof (object).Assembly.GetName ().Version);
		}
	}

	[Fact]
	public void ResolveModuleReferenceFromMemberReferenceTest ()
	{
		using (var mma = AssemblyDefinition.ReadAssembly (GetAssemblyResourcePath ("mma.exe"))) {
			var modB = mma.Modules [2];
			var bazType = modB.GetType ("Module.B.Baz");
			var gazonkMethod = bazType.Methods.First (m => m.Name.Equals ("Gazonk"));
			var callInstr = gazonkMethod.Body.Instructions [1];

			var methodRef = callInstr.Operand as MethodReference;
			var methodTypeRef = methodRef.DeclaringType;

			(methodTypeRef.Module.Assembly).Should().Be(mma);

			var def = methodTypeRef.Resolve ();
			Assert.NotNull (def);
			(def.FullName).Should().Be("Module.A.Foo");
		}
	}

	[Fact]
	public void ResolveModuleReferenceFromMemberReferenceOfSingleNetModuleTest ()
	{
		using (var modb = ModuleDefinition.ReadModule (GetAssemblyResourcePath ("modb.netmodule"))) {
			var bazType = modb.GetType ("Module.B.Baz");
			var gazonkMethod = bazType.Methods.First (m => m.Name.Equals ("Gazonk"));
			var callInstr = gazonkMethod.Body.Instructions [1];

			var methodRef = callInstr.Operand as MethodReference;
			var methodTypeRef = methodRef.DeclaringType;

			Assert.Null (methodTypeRef.Module.Assembly);
			Assert.Null (methodTypeRef.Resolve ());
		}
	}
	TRet GetReference<TDel, TRet> (TDel code)
	{
		var @delegate = code as Delegate;
		if (@delegate == null)
			throw new InvalidOperationException ();

		var reference = (TRet) GetReturnee (GetMethodFromDelegate (@delegate));

		Assert.NotNull (reference);

		return reference;
	}

	static object GetReturnee (MethodDefinition method)
	{
		Assert.True (method.HasBody);

		var instruction = method.Body.Instructions [method.Body.Instructions.Count - 1];

		Assert.NotNull (instruction);

		while (instruction != null) {
			var opcode = instruction.OpCode;
			switch (opcode.OperandType) {
			case OperandType.InlineField:
			case OperandType.InlineTok:
			case OperandType.InlineType:
			case OperandType.InlineMethod:
				return instruction.Operand;
			default:
				instruction = instruction.Previous;
				break;
			}
		}

		throw new InvalidOperationException ();
	}

	MethodDefinition GetMethodFromDelegate (Delegate @delegate)
	{
		var method = @delegate.Method;
		var type = (TypeDefinition) TypeParser.ParseType (GetCurrentModule (), method.DeclaringType.FullName);

		Assert.NotNull (type);

		return type.Methods.Where (m => m.Name == method.Name).First ();
	}
}
