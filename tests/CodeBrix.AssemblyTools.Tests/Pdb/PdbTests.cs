using System.IO;
using System.Linq;
using CodeBrix.AssemblyTools.Cil;
using CodeBrix.AssemblyTools.Pdb;
using Xunit;
using SilverAssertions;
namespace CodeBrix.AssemblyTools.Tests.Pdb; //was previously: Mono.Cecil.Tests;
	public class PdbTests : BaseTestFixture {

		[Fact]
		public void Main ()
		{
			TestModule ("test.exe", module => {
				var type = module.GetType ("Program");
				var main = type.GetMethod ("Main");

				AssertCode (@"
	.locals init (System.Int32 i, System.Int32 CS$1$0000, System.Boolean CS$4$0001)
	.line 6,6:2,3 'c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs'
	IL_0000: nop
	.line 7,7:8,18 'c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs'
	IL_0001: ldc.i4.0
	IL_0002: stloc.0
	.line hidden 'c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs'
	IL_0003: br.s IL_0012
	.line 8,8:4,21 'c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs'
	IL_0005: ldarg.0
	IL_0006: ldloc.0
	IL_0007: ldelem.ref
	IL_0008: call System.Void Program::Print(System.String)
	IL_000d: nop
	.line 7,7:36,39 'c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs'
	IL_000e: ldloc.0
	IL_000f: ldc.i4.1
	IL_0010: add
	IL_0011: stloc.0
	.line 7,7:19,34 'c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs'
	IL_0012: ldloc.0
	IL_0013: ldarg.0
	IL_0014: ldlen
	IL_0015: conv.i4
	IL_0016: clt
	IL_0018: stloc.2
	.line hidden 'c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs'
	IL_0019: ldloc.2
	IL_001a: brtrue.s IL_0005
	.line 10,10:3,12 'c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs'
	IL_001c: ldc.i4.0
	IL_001d: stloc.1
	IL_001e: br.s IL_0020
	.line 11,11:2,3 'c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs'
	IL_0020: ldloc.1
	IL_0021: ret
", main);
			}, readOnly: !Platform.HasNativePdbSupport, symbolReaderProvider: typeof (PdbReaderProvider), symbolWriterProvider: typeof (PdbWriterProvider));
		}

		[Fact]
		public void DebuggerHiddenVariable ()
		{
			TestModule ("test.exe", module => {
				var type = module.GetType ("Program");
				var method = type.GetMethod ("Main");

				var scope = method.DebugInformation.Scope;

				Assert.True (scope.HasVariables);
				var variables = scope.Variables;

				(variables [0].Name).Should().Be("CS$1$0000");
				Assert.True (variables [0].IsDebuggerHidden);
				(variables [1].Name).Should().Be("CS$4$0001");
				Assert.True (variables [1].IsDebuggerHidden);

				(scope.Scopes.Count).Should().Be(1);
				scope = scope.Scopes [0];
				variables = scope.Variables;

				(variables [0].Name).Should().Be("i");
				Assert.False (variables [0].IsDebuggerHidden);
			}, readOnly: !Platform.HasNativePdbSupport, symbolReaderProvider: typeof (PdbReaderProvider), symbolWriterProvider: typeof (PdbWriterProvider));
		}

		[Fact]
		public void Document ()
		{
			TestModule ("test.exe", module => {
				var type = module.GetType ("Program");
				var method = type.GetMethod ("Main");

				var sequence_point = method.DebugInformation.SequencePoints.First (sp => sp != null);
				var document = sequence_point.Document;

				Assert.NotNull (document);

				(document.Url).Should().Be(@"c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs");
				(document.Type).Should().Be(DocumentType.Text);
				(document.HashAlgorithm).Should().Be(DocumentHashAlgorithm.MD5);
				(document.Hash).Should().Equal(new byte [] { 228, 176, 152, 54, 82, 238, 238, 68, 237, 156, 5, 142, 118, 160, 118, 245 });
				(document.Language).Should().Be(DocumentLanguage.CSharp);
				(document.LanguageVendor).Should().Be(DocumentLanguageVendor.Microsoft);
			}, readOnly: !Platform.HasNativePdbSupport, symbolReaderProvider: typeof (PdbReaderProvider), symbolWriterProvider: typeof (PdbWriterProvider));
		}

		[Fact]
		public void BasicDocument ()
		{
			TestModule ("VBConsApp.exe", module => {
				var type = module.GetType ("VBConsApp.Program");
				var method = type.GetMethod ("Main");

				var sequence_point = method.DebugInformation.SequencePoints.First (sp => sp != null);
				var document = sequence_point.Document;

				Assert.NotNull (document);

				(document.Url).Should().Be(@"c:\tmp\VBConsApp\Program.vb");
				(document.Type).Should().Be(DocumentType.Text);
				(document.HashAlgorithm).Should().Be(DocumentHashAlgorithm.MD5);
				(document.Hash).Should().Equal(new byte [] { 184, 188, 100, 23, 27, 123, 187, 201, 175, 206, 110, 198, 242, 139, 154, 119 });
				(document.Language).Should().Be(DocumentLanguage.Basic);
				(document.LanguageVendor).Should().Be(DocumentLanguageVendor.Microsoft);
			}, readOnly: !Platform.HasNativePdbSupport, symbolReaderProvider: typeof (PdbReaderProvider), symbolWriterProvider: typeof (PdbWriterProvider));
		}

		[Fact]
		public void FSharpDocument ()
		{
			TestModule ("fsapp.exe", module => {
				var type = module.GetType ("Program");
				var method = type.GetMethod ("fact");

				var sequence_point = method.DebugInformation.SequencePoints.First (sp => sp != null);
				var document = sequence_point.Document;

				Assert.NotNull (document);

				(document.Url).Should().Be(@"c:\tmp\fsapp\Program.fs");
				(document.Type).Should().Be(DocumentType.Text);
				(document.HashAlgorithm).Should().Be(DocumentHashAlgorithm.None);
				(document.Language).Should().Be(DocumentLanguage.FSharp);
				(document.LanguageVendor).Should().Be(DocumentLanguageVendor.Microsoft);
			}, readOnly: !Platform.HasNativePdbSupport, symbolReaderProvider: typeof (PdbReaderProvider), symbolWriterProvider: typeof (PdbWriterProvider));
		}

		[Fact]
		public void EmptyEnumerable ()
		{
			TestModule ("empty-iterator.dll", module => {
			}, readOnly: !Platform.HasNativePdbSupport, symbolReaderProvider: typeof (PdbReaderProvider), symbolWriterProvider: typeof (PdbWriterProvider));
		}

		[Fact]
		public void EmptyRootNamespace ()
		{
			TestModule ("EmptyRootNamespace.dll", module => {
			}, readOnly: !Platform.HasNativePdbSupport, symbolReaderProvider: typeof (PdbReaderProvider), symbolWriterProvider: typeof (PdbWriterProvider));
		}

		[Fact]
		public void VisualBasicNamespace ()
		{
			TestModule ("AVbTest.exe", module => {
			}, readOnly: !Platform.HasNativePdbSupport, symbolReaderProvider: typeof (PdbReaderProvider), symbolWriterProvider: typeof (PdbWriterProvider));

		}

		[Fact]
		public void LocalVariables ()
		{
			TestModule ("ComplexPdb.dll", module => {
				var type = module.GetType ("ComplexPdb.Program");
				var method = type.GetMethod ("Bar");
				var debug_info = method.DebugInformation;

				Assert.NotNull (debug_info.Scope);
				Assert.True (debug_info.Scope.HasScopes);
				(debug_info.Scope.Scopes.Count).Should().Be(2);

				var scope = debug_info.Scope.Scopes [0];

				Assert.NotNull (scope);
				Assert.True (scope.HasVariables);
				(scope.Variables.Count).Should().Be(1);

				var variable = scope.Variables [0];

				(variable.Name).Should().Be("s");
				Assert.False (variable.IsDebuggerHidden);
				(variable.Index).Should().Be(2);

				scope = debug_info.Scope.Scopes [1];

				Assert.NotNull (scope);
				Assert.True (scope.HasVariables);
				(scope.Variables.Count).Should().Be(1);

				variable = scope.Variables [0];

				(variable.Name).Should().Be("s");
				Assert.False (variable.IsDebuggerHidden);
				(variable.Index).Should().Be(3);

				Assert.True (scope.HasScopes);
				(scope.Scopes.Count).Should().Be(1);

				scope = scope.Scopes [0];

				Assert.NotNull (scope);
				Assert.True (scope.HasVariables);
				(scope.Variables.Count).Should().Be(1);

				variable = scope.Variables [0];

				(variable.Name).Should().Be("u");
				Assert.False (variable.IsDebuggerHidden);
				(variable.Index).Should().Be(5);
			}, readOnly: !Platform.HasNativePdbSupport, symbolReaderProvider: typeof (PdbReaderProvider), symbolWriterProvider: typeof (PdbWriterProvider));
		}

		[Fact]
		public void LocalConstants ()
		{
			TestModule ("ComplexPdb.dll", module => {
				var type = module.GetType ("ComplexPdb.Program");
				var method = type.GetMethod ("Bar");
				var debug_info = method.DebugInformation;

				Assert.NotNull (debug_info.Scope);
				Assert.True (debug_info.Scope.HasScopes);
				(debug_info.Scope.Scopes.Count).Should().Be(2);

				var scope = debug_info.Scope.Scopes [1];

				Assert.NotNull (scope);
				Assert.True (scope.HasConstants);
				(scope.Constants.Count).Should().Be(2);

				var constant = scope.Constants [0];

				(constant.Name).Should().Be("b");
				(constant.Value).Should().Be(12);
				(constant.ConstantType.MetadataType).Should().Be(MetadataType.Int32);

				constant = scope.Constants [1];
				(constant.Name).Should().Be("c");
				(constant.Value).Should().Be((decimal)74);
				(constant.ConstantType.MetadataType).Should().Be(MetadataType.ValueType);

				method = type.GetMethod ("Foo");
				debug_info = method.DebugInformation;

				Assert.NotNull (debug_info.Scope);
				Assert.True (debug_info.Scope.HasConstants);
				(debug_info.Scope.Constants.Count).Should().Be(4);

				constant = debug_info.Scope.Constants [0];
				(constant.Name).Should().Be("s");
				(constant.Value).Should().Be("const string");
				(constant.ConstantType.MetadataType).Should().Be(MetadataType.String);

				constant = debug_info.Scope.Constants [1];
				(constant.Name).Should().Be("f");
				(constant.Value).Should().Be(1);
				(constant.ConstantType.MetadataType).Should().Be(MetadataType.Int32);

				constant = debug_info.Scope.Constants [2];
				(constant.Name).Should().Be("o");
				(constant.Value).Should().Be(null);
				(constant.ConstantType.MetadataType).Should().Be(MetadataType.Object);

				constant = debug_info.Scope.Constants [3];
				(constant.Name).Should().Be("u");
				(constant.Value).Should().Be(null);
				(constant.ConstantType.MetadataType).Should().Be(MetadataType.String);
			}, readOnly: !Platform.HasNativePdbSupport, symbolReaderProvider: typeof (PdbReaderProvider), symbolWriterProvider: typeof (PdbWriterProvider));
		}

		[Fact]
		public void ImportScope ()
		{
			TestModule ("ComplexPdb.dll", module => {
				var type = module.GetType ("ComplexPdb.Program");
				var method = type.GetMethod ("Bar");
				var debug_info = method.DebugInformation;

				Assert.NotNull (debug_info.Scope);

				var import = debug_info.Scope.Import;
				Assert.NotNull (import);

				Assert.True (import.HasTargets);
				(import.Targets.Count).Should().Be(6);
				var target = import.Targets [0];

				(target.Kind).Should().Be(ImportTargetKind.ImportNamespace);
				(target.Namespace).Should().Be("System");

				target = import.Targets [1];

				(target.Kind).Should().Be(ImportTargetKind.ImportNamespace);
				(target.Namespace).Should().Be("System.Collections.Generic");

				target = import.Targets [2];

				(target.Kind).Should().Be(ImportTargetKind.ImportNamespace);
				(target.Namespace).Should().Be("System.Threading.Tasks");

				target = import.Targets [3];

				(target.Kind).Should().Be(ImportTargetKind.ImportType);
				(target.Type.FullName).Should().Be("System.Console");

				target = import.Targets [4];

				(target.Kind).Should().Be(ImportTargetKind.DefineTypeAlias);
				(target.Alias).Should().Be("Foo1");
				(target.Type.FullName).Should().Be("System.Console");

				target = import.Targets [5];

				(target.Kind).Should().Be(ImportTargetKind.DefineNamespaceAlias);
				(target.Alias).Should().Be("Foo2");
				(target.Namespace).Should().Be("System.Reflection");
			}, readOnly: !Platform.HasNativePdbSupport, symbolReaderProvider: typeof (PdbReaderProvider), symbolWriterProvider: typeof (PdbWriterProvider));
		}

		[Fact]
		public void StateMachineKickOff ()
		{
			TestModule ("ComplexPdb.dll", module => {
				var state_machine = module.GetType ("ComplexPdb.Program/<TestAsync>d__2");
				var move_next = state_machine.GetMethod ("MoveNext");
				var symbol = move_next.DebugInformation;

				Assert.NotNull (symbol);
				Assert.NotNull (symbol.StateMachineKickOffMethod);
				(symbol.StateMachineKickOffMethod.FullName).Should().Be("System.Threading.Tasks.Task ComplexPdb.Program::TestAsync()");
			}, readOnly: !Platform.HasNativePdbSupport, symbolReaderProvider: typeof (PdbReaderProvider), symbolWriterProvider: typeof (PdbWriterProvider));
		}

		[Fact]
		public void Iterators ()
		{
			TestModule ("ComplexPdb.dll", module => {
				var state_machine = module.GetType ("ComplexPdb.Program/<TestAsync>d__2");
				var move_next = state_machine.GetMethod ("MoveNext");

				Assert.True (move_next.DebugInformation.HasCustomDebugInformations);
				(move_next.DebugInformation.CustomDebugInformations.Count).Should().Be(2);

				var state_machine_scope = move_next.DebugInformation.CustomDebugInformations [0] as StateMachineScopeDebugInformation;
				Assert.NotNull (state_machine_scope);
				(state_machine_scope.Scopes.Count).Should().Be(1);
				(state_machine_scope.Scopes [0].Start.Offset).Should().Be(142);
				(state_machine_scope.Scopes [0].End.Offset).Should().Be(319);

				var async_body = move_next.DebugInformation.CustomDebugInformations [1] as AsyncMethodBodyDebugInformation;
				Assert.NotNull (async_body);
				(async_body.CatchHandler.Offset).Should().Be(-1);

				(async_body.Yields.Count).Should().Be(2);
				(async_body.Yields [0].Offset).Should().Be(68);
				(async_body.Yields [1].Offset).Should().Be(197);

				(async_body.Resumes.Count).Should().Be(2);
				(async_body.Resumes [0].Offset).Should().Be(98);
				(async_body.Resumes [1].Offset).Should().Be(227);

				(async_body.ResumeMethods [0]).Should().Be(move_next);
				(async_body.ResumeMethods [1]).Should().Be(move_next);
			}, readOnly: !Platform.HasNativePdbSupport, symbolReaderProvider: typeof (PdbReaderProvider), symbolWriterProvider: typeof (PdbWriterProvider));
		}

		[Fact]
		public void ImportsForFirstMethod ()
		{
			TestModule ("CecilTest.exe", module => {
				var type = module.GetType ("CecilTest.Program");
				var method = type.GetMethod ("Main");

				var debug = method.DebugInformation;
				var scope = debug.Scope;

				Assert.True (scope.End.IsEndOfMethod);

				var import = scope.Import;

				Assert.NotNull (import);
				(import.Targets.Count).Should().Be(5);

				var ns = new [] {
					"System",
					"System.Collections.Generic",
					"System.Linq",
					"System.Text",
					"System.Threading.Tasks",
				};

				for (int i = 0; i < import.Targets.Count; i++) {
					var target = import.Targets [i];

					(target.Kind).Should().Be(ImportTargetKind.ImportNamespace);
					(target.Namespace).Should().Be(ns [i]);
				}

				(import.Targets [0].Namespace).Should().Be("System");
			}, readOnly: !Platform.HasNativePdbSupport, symbolReaderProvider: typeof (PdbReaderProvider), symbolWriterProvider: typeof (PdbWriterProvider));
		}

		[Fact]
		public void CreateMethodFromScratch ()
		{
			if (!Platform.HasNativePdbSupport)
				Assert.Skip("This test was skipped in the original Mono.Cecil codebase.");

			var module = ModuleDefinition.CreateModule ("Pan", ModuleKind.Dll);
			var type = new TypeDefinition ("Pin", "Pon", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.ImportReference (typeof (object)));
			module.Types.Add (type);

			var method = new MethodDefinition ("Pang", MethodAttributes.Public | MethodAttributes.Static, module.ImportReference (typeof (string)));
			type.Methods.Add (method);

			var body = method.Body;

			body.InitLocals = true;

			var il = body.GetILProcessor ();
			var temp = new VariableDefinition (module.ImportReference (typeof (string)));
			body.Variables.Add (temp);

			il.Emit (OpCodes.Nop);
			il.Emit (OpCodes.Ldstr, "hello");
			il.Emit (OpCodes.Stloc, temp);
			il.Emit (OpCodes.Ldloc, temp);
			il.Emit (OpCodes.Ret);

			var sequence_point = new SequencePoint (body.Instructions [0], new Document (@"C:\test.cs")) {
				StartLine = 0,
				StartColumn = 0,
				EndLine = 0,
				EndColumn = 4,
			};

			method.DebugInformation.SequencePoints.Add (sequence_point);

			method.DebugInformation.Scope = new ScopeDebugInformation (body.Instructions [0], null) {
				Variables = { new VariableDebugInformation (temp, "temp") }
			};

			var file = Path.Combine (Path.GetTempPath (), "Pan.dll");
			module.Write (file, new WriterParameters {
				SymbolWriterProvider = new PdbWriterProvider (),
			});

			module = ModuleDefinition.ReadModule (file, new ReaderParameters {
				SymbolReaderProvider = new PdbReaderProvider (),
			});

			method = module.GetType ("Pin.Pon").GetMethod ("Pang");

			(method.DebugInformation.Scope.Variables [0].Name).Should().Be("temp");
		}

		[Fact]
		public void TypeNameExceedingMaxPdbPath ()
		{
			if (!Platform.HasNativePdbSupport)
				Assert.Skip("This test was skipped in the original Mono.Cecil codebase.");

			TestModule ("longtypename.dll", module => {
				Assert.True (module.HasSymbols);
			}, symbolReaderProvider: typeof (NativePdbReaderProvider), symbolWriterProvider: typeof (NativePdbWriterProvider));
		}

		[Fact]
		public void ReadPdbMixedNativeCLIModule ()
		{
			// MixedNativeCLI.exe was copy/pasted from from https://docs.microsoft.com/en-us/cpp/preprocessor/managed-unmanaged?view=msvc-170#example
			TestModule ("MixedNativeCLI.exe", module => {
				module.ReadSymbols ();
			}, readOnly: true);
		}

	}
