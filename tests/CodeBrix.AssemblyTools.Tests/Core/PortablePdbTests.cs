using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using SilverAssertions;
using CodeBrix.AssemblyTools.Cil;
using CodeBrix.AssemblyTools.Pdb;
using CodeBrix.AssemblyTools.PE;

namespace CodeBrix.AssemblyTools.Tests.Core; //was previously: Mono.Cecil.Tests;
	public class PortablePdbTests : BaseTestFixture {

		[Fact]
		public void SequencePoints ()
		{
			TestPortablePdbModule (module => {
				var type = module.GetType ("PdbTarget.Program");
				var main = type.GetMethod ("Main");

				AssertCode (@"
	.locals init (System.Int32 a, System.String[] V_1, System.Int32 V_2, System.String arg)
	.line 21,21:3,4 'C:\sources\PdbTarget\Program.cs'
	IL_0000: nop
	.line 22,22:4,11 'C:\sources\PdbTarget\Program.cs'
	IL_0001: nop
	.line 22,22:24,28 'C:\sources\PdbTarget\Program.cs'
	IL_0002: ldarg.0
	IL_0003: stloc.1
	IL_0004: ldc.i4.0
	IL_0005: stloc.2
	.line hidden 'C:\sources\PdbTarget\Program.cs'
	IL_0006: br.s IL_0017
	.line 22,22:13,20 'C:\sources\PdbTarget\Program.cs'
	IL_0008: ldloc.1
	IL_0009: ldloc.2
	IL_000a: ldelem.ref
	IL_000b: stloc.3
	.line 23,23:5,20 'C:\sources\PdbTarget\Program.cs'
	IL_000c: ldloc.3
	IL_000d: call System.Void System.Console::WriteLine(System.String)
	IL_0012: nop
	.line hidden 'C:\sources\PdbTarget\Program.cs'
	IL_0013: ldloc.2
	IL_0014: ldc.i4.1
	IL_0015: add
	IL_0016: stloc.2
	.line 22,22:21,23 'C:\sources\PdbTarget\Program.cs'
	IL_0017: ldloc.2
	IL_0018: ldloc.1
	IL_0019: ldlen
	IL_001a: conv.i4
	IL_001b: blt.s IL_0008
	.line 25,25:4,22 'C:\sources\PdbTarget\Program.cs'
	IL_001d: ldc.i4.1
	IL_001e: ldc.i4.2
	IL_001f: call System.Int32 System.Math::Min(System.Int32,System.Int32)
	IL_0024: stloc.0
	.line 26,26:3,4 'C:\sources\PdbTarget\Program.cs'
	IL_0025: ret
", main);
			});
		}

		[Fact]
		public void SequencePointsMultipleDocument ()
		{
			TestPortablePdbModule (module => {
				var type = module.GetType ("PdbTarget.B");
				var main = type.GetMethod (".ctor");

				AssertCode (@"
	.locals ()
	.line 7,7:3,25 'C:\sources\PdbTarget\B.cs'
	IL_0000: ldarg.0
	IL_0001: ldstr """"
	IL_0006: stfld System.String PdbTarget.B::s
	.line 110,110:3,21 'C:\sources\PdbTarget\Program.cs'
	IL_000b: ldarg.0
	IL_000c: ldc.i4.2
	IL_000d: stfld System.Int32 PdbTarget.B::a
	.line 111,111:3,21 'C:\sources\PdbTarget\Program.cs'
	IL_0012: ldarg.0
	IL_0013: ldc.i4.3
	IL_0014: stfld System.Int32 PdbTarget.B::b
	.line 9,9:3,13 'C:\sources\PdbTarget\B.cs'
	IL_0019: ldarg.0
	IL_001a: call System.Void System.Object::.ctor()
	IL_001f: nop
	.line 10,10:3,4 'C:\sources\PdbTarget\B.cs'
	IL_0020: nop
	.line 11,11:4,19 'C:\sources\PdbTarget\B.cs'
	IL_0021: ldstr ""B""
	IL_0026: call System.Void System.Console::WriteLine(System.String)
	IL_002b: nop
	.line 12,12:3,4 'C:\sources\PdbTarget\B.cs'
	IL_002c: ret
", main);
			});
		}

		[Fact]
		public void LocalVariables ()
		{
			TestPortablePdbModule (module => {
				var type = module.GetType ("PdbTarget.Program");
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
			});
		}

		[Fact]
		public void LocalConstants ()
		{
			TestPortablePdbModule (module => {
				var type = module.GetType ("PdbTarget.Program");
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
				(constant.Value).Should().Be((decimal) 74);
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
			});
		}

		[Fact]
		public void ImportScope ()
		{
			TestPortablePdbModule (module => {
				var type = module.GetType ("PdbTarget.Program");
				var method = type.GetMethod ("Bar");
				var debug_info = method.DebugInformation;

				Assert.NotNull (debug_info.Scope);

				var import = debug_info.Scope.Import;
				Assert.NotNull (import);

				Assert.False (import.HasTargets);
				Assert.NotNull (import.Parent);

				import = import.Parent;

				Assert.True (import.HasTargets);
				(import.Targets.Count).Should().Be(9);
				var target = import.Targets [0];

				(target.Kind).Should().Be(ImportTargetKind.ImportAlias);
				(target.Alias).Should().Be("XML");

				target = import.Targets [1];

				(target.Kind).Should().Be(ImportTargetKind.ImportNamespace);
				(target.Namespace).Should().Be("System");

				target = import.Targets [2];

				(target.Kind).Should().Be(ImportTargetKind.ImportNamespace);
				(target.Namespace).Should().Be("System.Collections.Generic");

				target = import.Targets [3];

				(target.Kind).Should().Be(ImportTargetKind.ImportNamespace);
				(target.Namespace).Should().Be("System.IO");

				target = import.Targets [4];

				(target.Kind).Should().Be(ImportTargetKind.ImportNamespace);
				(target.Namespace).Should().Be("System.Threading.Tasks");

				target = import.Targets [5];

				(target.Kind).Should().Be(ImportTargetKind.ImportNamespaceInAssembly);
				(target.Namespace).Should().Be("System.Xml.Resolvers");
				(target.AssemblyReference.Name).Should().Be("System.Xml");


				target = import.Targets [6];

				(target.Kind).Should().Be(ImportTargetKind.ImportType);
				(target.Type.FullName).Should().Be("System.Console");

				target = import.Targets [7];

				(target.Kind).Should().Be(ImportTargetKind.ImportType);
				(target.Type.FullName).Should().Be("System.Math");

				target = import.Targets [8];

				(target.Kind).Should().Be(ImportTargetKind.DefineTypeAlias);
				(target.Alias).Should().Be("Foo");
				(target.Type.FullName).Should().Be("System.Xml.XmlDocumentType");

				Assert.NotNull (import.Parent);

				import = import.Parent;

				Assert.True (import.HasTargets);
				(import.Targets.Count).Should().Be(1);
				Assert.Null (import.Parent);

				target = import.Targets [0];

				(target.Kind).Should().Be(ImportTargetKind.DefineAssemblyAlias);
				(target.Alias).Should().Be("XML");
				(target.AssemblyReference.Name).Should().Be("System.Xml");
			});
		}

		[Fact]
		public void StateMachineKickOff ()
		{
			TestPortablePdbModule (module => {
				var state_machine = module.GetType ("PdbTarget.Program/<Baz>d__7");
				var main = state_machine.GetMethod ("MoveNext");
				var symbol = main.DebugInformation;

				Assert.NotNull (symbol);
				Assert.NotNull (symbol.StateMachineKickOffMethod);
				(symbol.StateMachineKickOffMethod.FullName).Should().Be("System.Threading.Tasks.Task PdbTarget.Program::Baz(System.IO.StreamReader)");
			});
		}

		[Fact]
		public void StateMachineCustomDebugInformation ()
		{
			TestPortablePdbModule (module => {
				var state_machine = module.GetType ("PdbTarget.Program/<Baz>d__7");
				var move_next = state_machine.GetMethod ("MoveNext");

				Assert.True (move_next.HasCustomDebugInformations);

				var state_machine_scope = move_next.CustomDebugInformations.OfType<StateMachineScopeDebugInformation> ().FirstOrDefault ();
				Assert.NotNull (state_machine_scope);
				(state_machine_scope.Scopes.Count).Should().Be(3);
				(state_machine_scope.Scopes [0].Start.Offset).Should().Be(0);
				Assert.True (state_machine_scope.Scopes [0].End.IsEndOfMethod);

				(state_machine_scope.Scopes [1].Start.Offset).Should().Be(0);
				(state_machine_scope.Scopes [1].End.Offset).Should().Be(0);

				(state_machine_scope.Scopes [2].Start.Offset).Should().Be(184);
				(state_machine_scope.Scopes [2].End.Offset).Should().Be(343);

				var async_body = move_next.CustomDebugInformations.OfType<AsyncMethodBodyDebugInformation> ().FirstOrDefault ();
				Assert.NotNull (async_body);
				(async_body.CatchHandler.Offset).Should().Be(-1);

				(async_body.Yields.Count).Should().Be(2);
				(async_body.Yields [0].Offset).Should().Be(61);
				(async_body.Yields [1].Offset).Should().Be(221);

				(async_body.Resumes.Count).Should().Be(2);
				(async_body.Resumes [0].Offset).Should().Be(91);
				(async_body.Resumes [1].Offset).Should().Be(252);

				(async_body.ResumeMethods [0]).Should().Be(move_next);
				(async_body.ResumeMethods [1]).Should().Be(move_next);
			});
		}

		[Fact]
		public void EmbeddedCompressedPortablePdb ()
		{
			TestModule("EmbeddedCompressedPdbTarget.exe", module => {
				Assert.True (module.HasDebugHeader);

				var header = module.GetDebugHeader ();

				Assert.NotNull (header);
				Assert.True (header.Entries.Length >= 2);

				int i = 0;
				var cv = header.Entries [i++];
				(cv.Directory.Type).Should().Be(ImageDebugType.CodeView);

				if (header.Entries.Length > 2) {
					(header.Entries.Length).Should().Be(3);
					var pdbChecksum = header.Entries [i++];
					(pdbChecksum.Directory.Type).Should().Be(ImageDebugType.PdbChecksum);
				}

				var eppdb = header.Entries [i++];
				(eppdb.Directory.Type).Should().Be(ImageDebugType.EmbeddedPortablePdb);
				(eppdb.Directory.MajorVersion).Should().Be(0x0100);
				(eppdb.Directory.MinorVersion).Should().Be(0x0100);

			}, symbolReaderProvider: typeof (EmbeddedPortablePdbReaderProvider), symbolWriterProvider: typeof (EmbeddedPortablePdbWriterProvider));
		}

		[Fact]
		public void EmbeddedCompressedPortablePdbFromStream ()
		{
			var bytes = File.ReadAllBytes (GetAssemblyResourcePath ("EmbeddedCompressedPdbTarget.exe"));
			var parameters = new ReaderParameters {
				ReadSymbols = true,
				SymbolReaderProvider = new PdbReaderProvider ()
			};

			var module = ModuleDefinition.ReadModule (new MemoryStream(bytes), parameters);
			Assert.True (module.HasDebugHeader);

			var header = module.GetDebugHeader ();

			Assert.NotNull (header);
			(header.Entries.Length).Should().Be(2);

			var cv = header.Entries [0];
			(cv.Directory.Type).Should().Be(ImageDebugType.CodeView);

			var eppdb = header.Entries [1];
			(eppdb.Directory.Type).Should().Be(ImageDebugType.EmbeddedPortablePdb);
			(eppdb.Directory.MajorVersion).Should().Be(0x0100);
			(eppdb.Directory.MinorVersion).Should().Be(0x0100);
		}


		void TestPortablePdbModule (Action<ModuleDefinition> test)
		{
			TestModule ("PdbTarget.exe", test, symbolReaderProvider: typeof (PortablePdbReaderProvider), symbolWriterProvider: typeof (PortablePdbWriterProvider));
			TestModule ("EmbeddedPdbTarget.exe", test, verify: !Platform.OnMono);
			TestModule ("EmbeddedCompressedPdbTarget.exe", test, symbolReaderProvider: typeof(EmbeddedPortablePdbReaderProvider), symbolWriterProvider: typeof (EmbeddedPortablePdbWriterProvider));
		}

		[Fact]
		public void RoundTripCecilPortablePdb ()
		{
			TestModule ("cecil.dll", module => {
				Assert.True (module.HasSymbols);
			}, symbolReaderProvider: typeof (PortablePdbReaderProvider), symbolWriterProvider: typeof (PortablePdbWriterProvider));
		}

		[Fact]
		public void RoundTripLargePortablePdb ()
		{
			TestModule ("Mono.Android.dll", module => {
				Assert.True (module.HasSymbols);
			}, verify: false, symbolReaderProvider: typeof (PortablePdbReaderProvider), symbolWriterProvider: typeof (PortablePdbWriterProvider));
		}

		[Fact]
		public void EmptyPortablePdb ()
		{
			TestModule ("EmptyPdb.dll", module => {
				Assert.True (module.HasSymbols);
			}, symbolReaderProvider: typeof (PortablePdbReaderProvider), symbolWriterProvider: typeof (PortablePdbWriterProvider));
		}

		[Fact]
		public void NullClassConstant ()
		{
			TestModule ("xattr.dll", module => {
				var type = module.GetType ("Library");
				var method = type.GetMethod ("NullXAttributeConstant");
				var symbol = method.DebugInformation;

				Assert.NotNull (symbol);
				(symbol.Scope.Constants.Count).Should().Be(1);

				var a = symbol.Scope.Constants [0];
				(a.Name).Should().Be("a");
			}, symbolReaderProvider: typeof (PortablePdbReaderProvider), symbolWriterProvider: typeof (PortablePdbWriterProvider));
		}

		[Fact]
		public void NullGenericInstConstant ()
		{
			TestModule ("NullConst.dll", module => {
				var type = module.GetType ("NullConst.Program");
				var method = type.GetMethod ("MakeConst");
				var symbol = method.DebugInformation;

				Assert.NotNull (symbol);
				(symbol.Scope.Constants.Count).Should().Be(1);

				var a = symbol.Scope.Constants [0];
				(a.Name).Should().Be("thing");
				(a.Value).Should().Be(null);
			}, verify: false, symbolReaderProvider: typeof (PortablePdbReaderProvider), symbolWriterProvider: typeof (PortablePdbWriterProvider));
		}

		[Fact]
		public void InvalidConstantRecord ()
		{
			using (var module = GetResourceModule ("mylib.dll", new ReaderParameters { SymbolReaderProvider = new PortablePdbReaderProvider () })) {
				var type = module.GetType ("mylib.Say");
				var method = type.GetMethod ("hello");
				var symbol = method.DebugInformation;

				Assert.NotNull (symbol);
				(symbol.Scope.Constants.Count).Should().Be(0);
			}
		}

		[Fact]
		public void GenericInstConstantRecord ()
		{
			using (var module = GetResourceModule ("ReproConstGenericInst.dll", new ReaderParameters { SymbolReaderProvider = new PortablePdbReaderProvider () })) {
				var type = module.GetType ("ReproConstGenericInst.Program");
				var method = type.GetMethod ("Main");
				var symbol = method.DebugInformation;

				Assert.NotNull (symbol);
				(symbol.Scope.Constants.Count).Should().Be(1);

				var list = symbol.Scope.Constants [0];
				(list.Name).Should().Be("list");

				(list.ConstantType.FullName).Should().Be("System.Collections.Generic.List`1<System.String>");
			}
		}

		[Fact]
		public void EmptyStringLocalConstant ()
		{
			TestModule ("empty-str-const.exe", module => {
				var type = module.GetType ("<Program>$");
				var method = type.GetMethod ("<Main>$");
				var symbol = method.DebugInformation;

				Assert.NotNull (symbol);
				(symbol.Scope.Constants.Count).Should().Be(1);

				var a = symbol.Scope.Constants [0];
				(a.Name).Should().Be("value");
				(a.Value).Should().Be("");
			}, symbolReaderProvider: typeof (PortablePdbReaderProvider), symbolWriterProvider: typeof (PortablePdbWriterProvider));
		}

		[Fact]
		public void SourceLink ()
		{
			TestModule ("TargetLib.dll", module => {
				Assert.True (module.HasCustomDebugInformations);
				(module.CustomDebugInformations.Count).Should().Be(1);

				var source_link = module.CustomDebugInformations [0] as SourceLinkDebugInformation;
				Assert.NotNull (source_link);
				(source_link.Content).Should().Be("{\"documents\":{\"C:\\\\tmp\\\\SourceLinkProblem\\\\*\":\"https://raw.githubusercontent.com/bording/SourceLinkProblem/197d965ee7f1e7f8bd3cea55b5f904aeeb8cd51e/*\"}}");
			}, symbolReaderProvider: typeof (PortablePdbReaderProvider), symbolWriterProvider: typeof (PortablePdbWriterProvider));
		}

		[Fact]
		public void EmbeddedSource ()
		{
			TestModule ("embedcs.exe", module => {
			}, symbolReaderProvider: typeof (PortablePdbReaderProvider), symbolWriterProvider: typeof (PortablePdbWriterProvider));

			TestModule ("embedcs.exe", module => {
				var program = GetDocument (module.GetType ("Program"));
				var program_src = GetSourceDebugInfo (program);
				Assert.True (program_src.Compress);
				var program_src_content = Encoding.UTF8.GetString (program_src.Content);
				(Normalize (program_src_content)).Should().Be(Normalize (@"using System;

class Program
{
    static void Main()
    {
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        // Hello hello hello hello hello hello
        Console.WriteLine(B.Do());
        Console.WriteLine(A.Do());
    }
}
"));

				var a = GetDocument (module.GetType ("A"));
				var a_src = GetSourceDebugInfo (a);
				Assert.False (a_src.Compress);
				var a_src_content = Encoding.UTF8.GetString (a_src.Content);
				(Normalize (a_src_content)).Should().Be(Normalize (@"class A
{
    public static string Do()
    {
        return ""A::Do"";
    }
}"));

				var b = GetDocument(module.GetType ("B"));
				var b_src = GetSourceDebugInfo (b);
				Assert.False (b_src.compress);
				var b_src_content = Encoding.UTF8.GetString (b_src.Content);
				(Normalize (b_src_content)).Should().Be(Normalize (@"class B
{
    public static string Do()
    {
        return ""B::Do"";
    }
}"));
			}, symbolReaderProvider: typeof (PortablePdbReaderProvider), symbolWriterProvider: typeof (PortablePdbWriterProvider));
		}

		static Document GetDocument (TypeDefinition type)
		{
			foreach (var method in type.Methods) {
				if (!method.HasBody)
					continue;

				foreach (var instruction in method.Body.Instructions) {
					var sp = method.DebugInformation.GetSequencePoint (instruction);
					if (sp != null && sp.Document != null)
						return sp.Document;
				}
			}

			return null;
		}

		static EmbeddedSourceDebugInformation GetSourceDebugInfo (Document document)
		{
			Assert.True (document.HasCustomDebugInformations);
			(document.CustomDebugInformations.Count).Should().Be(1);

			var source = document.CustomDebugInformations [0] as EmbeddedSourceDebugInformation;
			Assert.NotNull (source);
			return source;
		}

		[Fact]
		public void PortablePdbLineInfo()
		{
			TestModule ("line.exe", module => {
				var type = module.GetType ("Tests");
				var main = type.GetMethod ("Main");

				AssertCode (@"
	.locals ()
	.line 4,4:42,43 '/foo/bar.cs'
	IL_0000: nop
	.line 5,5:2,3 '/foo/bar.cs'
	IL_0001: ret", main);
			}, symbolReaderProvider: typeof (PortablePdbReaderProvider), symbolWriterProvider: typeof (PortablePdbWriterProvider));
		}

		public sealed class SymbolWriterProvider : ISymbolWriterProvider {

			readonly DefaultSymbolWriterProvider writer_provider = new DefaultSymbolWriterProvider ();

			public ISymbolWriter GetSymbolWriter (ModuleDefinition module, string fileName)
			{
				return new SymbolWriter (writer_provider.GetSymbolWriter (module, fileName));
			}

			public ISymbolWriter GetSymbolWriter (ModuleDefinition module, Stream symbolStream)
			{
				return new SymbolWriter (writer_provider.GetSymbolWriter (module, symbolStream));
			}
		}

		public sealed class SymbolWriter : ISymbolWriter {

			readonly ISymbolWriter symbol_writer;

			public SymbolWriter (ISymbolWriter symbolWriter)
			{
				this.symbol_writer = symbolWriter;
			}

			public ImageDebugHeader GetDebugHeader ()
			{
				var header = symbol_writer.GetDebugHeader ();
				if (!header.HasEntries)
					return header;

				for (int i = 0; i < header.Entries.Length; i++) {
					header.Entries [i] = ProcessEntry (header.Entries [i]);
				}

				return header;
			}

			private static ImageDebugHeaderEntry ProcessEntry (ImageDebugHeaderEntry entry)
			{
				if (entry.Directory.Type != ImageDebugType.CodeView)
					return entry;

				var reader = new ByteBuffer (entry.Data);
				var writer = new ByteBuffer ();

				var sig = reader.ReadUInt32 ();
				if (sig != 0x53445352)
					return entry;

				writer.WriteUInt32 (sig); // RSDS
				writer.WriteBytes (reader.ReadBytes (16)); // MVID
				writer.WriteUInt32 (reader.ReadUInt32 ()); // Age

				var length = Array.IndexOf (entry.Data, (byte) 0, reader.position) - reader.position;

				var fullPath = Encoding.UTF8.GetString (reader.ReadBytes (length));

				writer.WriteBytes (Encoding.UTF8.GetBytes (Path.GetFileName (fullPath)));
				writer.WriteByte (0);

				var newData = new byte [writer.length];
				Buffer.BlockCopy (writer.buffer, 0, newData, 0, writer.length);

				var directory = entry.Directory;
				directory.SizeOfData = newData.Length;

				return new ImageDebugHeaderEntry (directory, newData);
			}

			public ISymbolReaderProvider GetReaderProvider ()
			{
				return symbol_writer.GetReaderProvider ();
			}

			public void Write (MethodDebugInformation info)
			{
				symbol_writer.Write (info);
			}

			public void Write ()
			{
				symbol_writer.Write ();
			}

			public void Write (ICustomDebugInformationProvider provider)
			{
				symbol_writer.Write (provider);
			}

			public void Dispose ()
			{
				symbol_writer.Dispose ();
			}
		}

		static string GetDebugHeaderPdbPath (ModuleDefinition module)
		{
			var header = module.GetDebugHeader ();
			var cv = Mixin.GetCodeViewEntry (header);
			Assert.NotNull (cv);
			var length = Array.IndexOf (cv.Data, (byte)0, 24) - 24;
			var bytes = new byte [length];
			Buffer.BlockCopy (cv.Data, 24, bytes, 0, length);
			return Encoding.UTF8.GetString (bytes);
		}

		[Fact]
		public void UseCustomSymbolWriterToChangeDebugHeaderPdbPath ()
		{
			const string resource = "mylib.dll";

			string debug_header_pdb_path;
			string dest = Path.Combine (Path.GetTempPath (), resource);

			using (var module = GetResourceModule (resource, new ReaderParameters { SymbolReaderProvider = new PortablePdbReaderProvider () })) {
				debug_header_pdb_path = GetDebugHeaderPdbPath (module);
				Assert.True (Path.IsPathRooted (debug_header_pdb_path));
				module.Write (dest, new WriterParameters { SymbolWriterProvider = new SymbolWriterProvider () });
			}

			using (var module = ModuleDefinition.ReadModule (dest, new ReaderParameters { SymbolReaderProvider = new PortablePdbReaderProvider () })) {
				var pdb_path = GetDebugHeaderPdbPath (module);
				Assert.False (Path.IsPathRooted (pdb_path));
				(pdb_path).Should().Be(Path.GetFileName (debug_header_pdb_path));
			}
		}

		[Fact]
		public void WriteAndReadAgainModuleWithDeterministicMvid ()
		{
			const string resource = "mylib.dll";
			string destination = Path.GetTempFileName ();

			using (var module = GetResourceModule (resource, new ReaderParameters { SymbolReaderProvider = new PortablePdbReaderProvider () })) {
				module.Write (destination, new WriterParameters { DeterministicMvid = true, SymbolWriterProvider = new SymbolWriterProvider () });
			}

			using (var module = ModuleDefinition.ReadModule (destination, new ReaderParameters { SymbolReaderProvider = new PortablePdbReaderProvider () })) {
			}
		}

		[Fact]
		public void DoubleWriteAndReadAgainModuleWithDeterministicMvid ()
		{
			Guid mvid1_in, mvid1_out, mvid2_in, mvid2_out;

			{
				const string resource = "foo.dll";
				string destination = Path.GetTempFileName ();

				using (var module = GetResourceModule (resource, new ReaderParameters {  })) {
					mvid1_in = module.Mvid;
					module.Write (destination, new WriterParameters { DeterministicMvid = true });
				}

				using (var module = ModuleDefinition.ReadModule (destination, new ReaderParameters { })) {
					mvid1_out = module.Mvid;
				}
			}

			{
				const string resource = "hello2.exe";
				string destination = Path.GetTempFileName ();

				using (var module = GetResourceModule (resource, new ReaderParameters {  })) {
					mvid2_in = module.Mvid;
					module.Write (destination, new WriterParameters { DeterministicMvid = true });
				}

				using (var module = ModuleDefinition.ReadModule (destination, new ReaderParameters { })) {
					mvid2_out = module.Mvid;
				}
			}

			(mvid2_in).Should().NotBe(mvid1_in);
			(mvid2_out).Should().NotBe(mvid1_out);
		}

		[Fact]
		public void ClearSequencePoints ()
		{
			TestPortablePdbModule (module => {
				var type = module.GetType ("PdbTarget.Program");
				var main = type.GetMethod ("Main");

				main.DebugInformation.SequencePoints.Clear ();

				var destination = Path.Combine (Path.GetTempPath (), "mylib.dll");
				module.Write(destination, new WriterParameters { WriteSymbols = true });

				(main.DebugInformation.SequencePoints.Count).Should().Be(0);

				using (var resultModule = ModuleDefinition.ReadModule (destination, new ReaderParameters { ReadSymbols = true })) {
					type = resultModule.GetType ("PdbTarget.Program");
					main = type.GetMethod ("Main");

					(main.DebugInformation.SequencePoints.Count).Should().Be(0);
				}
			});
		}

		[Fact]
		public void DoubleWriteAndReadWithDeterministicMvidAndVariousChanges ()
		{
			Guid mvidIn, mvidARM64Out, mvidX64Out;

			const string resource = "mylib.dll";
			{
				string destination = Path.GetTempFileName ();

				using (var module = GetResourceModule (resource, new ReaderParameters { ReadSymbols = true })) {
					mvidIn = module.Mvid;
					module.Architecture = TargetArchitecture.ARM64; // Can't use I386 as it writes different import table size -> differnt MVID
					module.Write (destination, new WriterParameters { DeterministicMvid = true, WriteSymbols = true });
				}

				using (var module = ModuleDefinition.ReadModule (destination, new ReaderParameters { ReadSymbols = true })) {
					mvidARM64Out = module.Mvid;
				}

				(mvidARM64Out).Should().NotBe(mvidIn);
			}

			{
				string destination = Path.GetTempFileName ();

				using (var module = GetResourceModule (resource, new ReaderParameters { ReadSymbols = true })) {
					(module.Mvid).Should().Be(mvidIn);
					module.Architecture = TargetArchitecture.AMD64;
					module.Write (destination, new WriterParameters { DeterministicMvid = true, WriteSymbols = true });
				}

				using (var module = ModuleDefinition.ReadModule (destination, new ReaderParameters { ReadSymbols = true })) {
					mvidX64Out = module.Mvid;
				}

				(mvidX64Out).Should().NotBe(mvidARM64Out);
			}

			{
				string destination = Path.GetTempFileName ();

				using (var module = GetResourceModule (resource, new ReaderParameters { ReadSymbols = true })) {
					(module.Mvid).Should().Be(mvidIn);
					module.Architecture = TargetArchitecture.AMD64;
					module.timestamp = 42;
					module.Write (destination, new WriterParameters { DeterministicMvid = true, WriteSymbols = true });
				}

				Guid mvidDifferentTimeStamp;
				using (var module = ModuleDefinition.ReadModule (destination, new ReaderParameters { ReadSymbols = true })) {
					mvidDifferentTimeStamp = module.Mvid;
				}

				(mvidDifferentTimeStamp).Should().NotBe(mvidX64Out);
			}
		}

		[Fact]
		public void ReadPortablePdbChecksum ()
		{
			const string resource = "PdbChecksumLib.dll";

			using (var module = GetResourceModule (resource, new ReaderParameters { ReadSymbols = true })) {
				GetPdbChecksumData (module.GetDebugHeader (), out string algorithmName, out byte [] checksum);
				(algorithmName).Should().Be("SHA256");
				GetCodeViewPdbId (module, out byte[] pdbId);

				string pdbPath = Mixin.GetPdbFileName (module.FileName);
				CalculatePdbChecksumAndId (pdbPath, out byte [] expectedChecksum, out byte [] expectedPdbId);

				(checksum).Should().Equal(expectedChecksum);
				(pdbId).Should().Equal(expectedPdbId);
			}
		}

		[Fact]
		public void ReadEmbeddedPortablePdbChecksum ()
		{
			const string resource = "EmbeddedPdbChecksumLib.dll";

			using (var module = GetResourceModule (resource, new ReaderParameters { ReadSymbols = true })) {
				var debugHeader = module.GetDebugHeader ();
				GetPdbChecksumData (debugHeader, out string algorithmName, out byte [] checksum);
				(algorithmName).Should().Be("SHA256");
				GetCodeViewPdbId (module, out byte [] pdbId);

				GetEmbeddedPdb (module.Image, debugHeader, out byte [] embeddedPdb);
				CalculatePdbChecksumAndId (embeddedPdb, out byte [] expectedChecksum, out byte [] expectedPdbId);

				(checksum).Should().Equal(expectedChecksum);
				(pdbId).Should().Equal(expectedPdbId);
			}
		}

		[Fact]
		public void WritePortablePdbChecksum ()
		{
			const string resource = "PdbChecksumLib.dll";
			string destination = Path.GetTempFileName ();

			using (var module = GetResourceModule (resource, new ReaderParameters { ReadSymbols = true })) {
				module.Write (destination, new WriterParameters { DeterministicMvid = true, WriteSymbols = true });
			}

			using (var module = ModuleDefinition.ReadModule (destination, new ReaderParameters { ReadSymbols = true })) {
				GetPdbChecksumData (module.GetDebugHeader (), out string algorithmName, out byte [] checksum);
				(algorithmName).Should().Be("SHA256");
				GetCodeViewPdbId (module, out byte [] pdbId);

				string pdbPath = Mixin.GetPdbFileName (module.FileName);
				CalculatePdbChecksumAndId (pdbPath, out byte [] expectedChecksum, out byte [] expectedPdbId);

				(checksum).Should().Equal(expectedChecksum);
				(pdbId).Should().Equal(expectedPdbId);
			}
		}

		[Fact]
		public void WritePortablePdbToWriteOnlyStream ()
		{
			const string resource = "PdbChecksumLib.dll";
			string destination = Path.GetTempFileName ();

			// Note that the module stream already requires read access even on writing to be able to compute strong name
			using (var module = GetResourceModule (resource, new ReaderParameters { ReadSymbols = true }))
			using (var pdbStream = new FileStream (destination + ".pdb", FileMode.Create, FileAccess.Write)) {
				module.Write (destination, new WriterParameters {
					DeterministicMvid = true,
					WriteSymbols = true,
					SymbolWriterProvider = new PortablePdbWriterProvider (),
					SymbolStream = pdbStream
				});
			}
		}

		[Fact]
		public void DoubleWritePortablePdbDeterministicPdbId ()
		{
			const string resource = "PdbChecksumLib.dll";
			string destination = Path.GetTempFileName ();

			using (var module = GetResourceModule (resource, new ReaderParameters { ReadSymbols = true })) {
				module.Write (destination, new WriterParameters { DeterministicMvid = true, WriteSymbols = true });
			}

			byte [] pdbIdOne;
			using (var module = ModuleDefinition.ReadModule (destination, new ReaderParameters { ReadSymbols = true })) {
				string pdbPath = Mixin.GetPdbFileName (module.FileName);
				CalculatePdbChecksumAndId (pdbPath, out byte [] expectedChecksum, out pdbIdOne);
			}

			using (var module = GetResourceModule (resource, new ReaderParameters { ReadSymbols = true })) {
				module.Write (destination, new WriterParameters { DeterministicMvid = true, WriteSymbols = true });
			}

			byte [] pdbIdTwo;
			using (var module = ModuleDefinition.ReadModule (destination, new ReaderParameters { ReadSymbols = true })) {
				string pdbPath = Mixin.GetPdbFileName (module.FileName);
				CalculatePdbChecksumAndId (pdbPath, out byte [] expectedChecksum, out pdbIdTwo);
			}

			(pdbIdTwo).Should().Equal(pdbIdOne);
		}

		[Fact]
		public void WriteEmbeddedPortablePdbChecksum ()
		{
			const string resource = "EmbeddedPdbChecksumLib.dll";
			string destination = Path.GetTempFileName ();

			using (var module = GetResourceModule (resource, new ReaderParameters { ReadSymbols = true })) {
				module.Write (destination, new WriterParameters { DeterministicMvid = true, WriteSymbols = true });
			}

			using (var module = ModuleDefinition.ReadModule (destination, new ReaderParameters { ReadSymbols = true })) {
				var debugHeader = module.GetDebugHeader ();
				GetPdbChecksumData (debugHeader, out string algorithmName, out byte [] checksum);
				(algorithmName).Should().Be("SHA256");
				GetCodeViewPdbId (module, out byte [] pdbId);

				GetEmbeddedPdb (module.Image, debugHeader, out byte [] embeddedPdb);
				CalculatePdbChecksumAndId (embeddedPdb, out byte [] expectedChecksum, out byte [] expectedPdbId);

				(checksum).Should().Equal(expectedChecksum);
				(pdbId).Should().Equal(expectedPdbId);
			}
		}

		[Fact]
		public void DoubleWriteEmbeddedPortablePdbChecksum ()
		{
			const string resource = "EmbeddedPdbChecksumLib.dll";
			string destination = Path.GetTempFileName ();

			using (var module = GetResourceModule (resource, new ReaderParameters { ReadSymbols = true })) {
				module.Write (destination, new WriterParameters { DeterministicMvid = true, WriteSymbols = true });
			}

			byte [] pdbIdOne;
			using (var module = ModuleDefinition.ReadModule (destination, new ReaderParameters { ReadSymbols = true })) {
				var debugHeader = module.GetDebugHeader ();
				GetEmbeddedPdb (module.Image, debugHeader, out byte [] embeddedPdb);
				CalculatePdbChecksumAndId (embeddedPdb, out byte [] expectedChecksum, out pdbIdOne);
			}

			using (var module = GetResourceModule (resource, new ReaderParameters { ReadSymbols = true })) {
				module.Write (destination, new WriterParameters { DeterministicMvid = true, WriteSymbols = true });
			}

			byte [] pdbIdTwo;
			using (var module = ModuleDefinition.ReadModule (destination, new ReaderParameters { ReadSymbols = true })) {
				var debugHeader = module.GetDebugHeader ();
				GetEmbeddedPdb (module.Image, debugHeader, out byte [] embeddedPdb);
				CalculatePdbChecksumAndId (embeddedPdb, out byte [] expectedChecksum, out pdbIdTwo);
			}

			(pdbIdTwo).Should().Equal(pdbIdOne);
		}

		private void GetEmbeddedPdb (Image image, ImageDebugHeader debugHeader, out byte [] embeddedPdb)
		{
			var entry = Mixin.GetEmbeddedPortablePdbEntry (debugHeader);
			Assert.NotNull (entry);

			(image.ResolveVirtualAddress ((uint)entry.Directory.AddressOfRawData)).Should().Be((uint)entry.Directory.PointerToRawData);

			var compressed_stream = new MemoryStream (entry.Data);
			var reader = new BinaryStreamReader (compressed_stream);
			(reader.ReadInt32 ()).Should().Be(0x4244504D);
			var length = reader.ReadInt32 ();
			var decompressed_stream = new MemoryStream (length);

			using (var deflate = new DeflateStream (compressed_stream, CompressionMode.Decompress, leaveOpen: true))
				deflate.CopyTo (decompressed_stream);

			embeddedPdb = decompressed_stream.ToArray ();
		}

		private void GetPdbChecksumData (ImageDebugHeader debugHeader, out string algorithmName, out byte [] checksum)
		{
			var entry = Mixin.GetPdbChecksumEntry (debugHeader);
			Assert.NotNull (entry);

			var length = Array.IndexOf (entry.Data, (byte)0, 0);
			var bytes = new byte [length];
			Buffer.BlockCopy (entry.Data, 0, bytes, 0, length);
			algorithmName = Encoding.UTF8.GetString (bytes);
			int checksumSize = 0;
			switch (algorithmName) {
			case "SHA256": checksumSize = 32; break;
			case "SHA384": checksumSize = 48; break;
			case "SHA512": checksumSize = 64; break;
			}
			checksum = new byte [checksumSize];
			Buffer.BlockCopy (entry.Data, length + 1, checksum, 0, checksumSize);
		}

		private void CalculatePdbChecksumAndId (string filePath, out byte [] pdbChecksum, out byte [] pdbId)
		{
			using (var fs = File.OpenRead (filePath))
				CalculatePdbChecksumAndId (fs, out pdbChecksum, out pdbId);
		}

		private void CalculatePdbChecksumAndId (byte [] data, out byte [] pdbChecksum, out byte [] pdbId)
		{
			using (var pdb = new MemoryStream (data))
				CalculatePdbChecksumAndId (pdb, out pdbChecksum, out pdbId);
		}

		private void CalculatePdbChecksumAndId (Stream pdbStream, out byte [] pdbChecksum, out byte [] pdbId)
		{
			// Get the offset of the PDB heap (this requires parsing several headers
			// so it's easier to use the ImageReader directly for this)
			Image image = ImageReader.ReadPortablePdb (new Disposable<Stream> (pdbStream, false), "test.pdb", out uint pdbHeapOffset);
			pdbId = new byte [20];
			Array.Copy (image.PdbHeap.data, 0, pdbId, 0, 20);

			pdbStream.Seek (0, SeekOrigin.Begin);
			byte [] rawBytes = pdbStream.ReadAll ();

			var bytes = new byte [rawBytes.Length];

			Array.Copy (rawBytes, 0, bytes, 0, pdbHeapOffset);

			// Zero out the PDB ID (20 bytes)
			for (int i = 0; i < 20; bytes [i + pdbHeapOffset] = 0, i++) ;

			Array.Copy (rawBytes, pdbHeapOffset + 20, bytes, pdbHeapOffset + 20, rawBytes.Length - pdbHeapOffset - 20);

			var sha256 = SHA256.Create ();
			pdbChecksum = sha256.ComputeHash (bytes);
		}

		static void GetCodeViewPdbId (ModuleDefinition module, out byte[] pdbId)
		{
			var header = module.GetDebugHeader ();
			var cv = Mixin.GetCodeViewEntry (header);
			Assert.NotNull (cv);

			(cv.Data.Take (4)).Should().Equal(new byte [] { 0x52, 0x53, 0x44, 0x53 });

			ByteBuffer buffer = new ByteBuffer (20);
			buffer.WriteBytes (cv.Data.Skip (4).Take (16).ToArray ());
			buffer.WriteInt32 (cv.Directory.TimeDateStamp);
			pdbId = buffer.buffer;
		}
	}
