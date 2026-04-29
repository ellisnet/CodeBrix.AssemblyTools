using CodeBrix.AssemblyTools.Cil;
using CodeBrix.AssemblyTools.Mdb;
using Xunit;
using SilverAssertions;
using System.IO;

namespace CodeBrix.AssemblyTools.Tests.Mdb; //was previously: Mono.Cecil.Tests;
	public class MdbTests : BaseTestFixture {

		[Fact]
		public void MdbWithJustLineInfo ()
		{
			TestModule ("simplemdb.exe", module => {
				var type = module.GetType ("Program");
				var main = type.GetMethod ("Main");

				AssertCode (@"
	.locals init (System.Int32 i)
	.line 6,-1:-1,-1 'C:\sources\cecil\symbols\Mono.Cecil.Mdb\Test\Resources\assemblies\hello.cs'
	IL_0000: ldc.i4.0
	IL_0001: stloc.0
	.line 7,-1:-1,-1 'C:\sources\cecil\symbols\Mono.Cecil.Mdb\Test\Resources\assemblies\hello.cs'
	IL_0002: br IL_0013
	.line 8,-1:-1,-1 'C:\sources\cecil\symbols\Mono.Cecil.Mdb\Test\Resources\assemblies\hello.cs'
	IL_0007: ldarg.0
	IL_0008: ldloc.0
	IL_0009: ldelem.ref
	IL_000a: call System.Void Program::Print(System.String)
	.line 7,-1:-1,-1 'C:\sources\cecil\symbols\Mono.Cecil.Mdb\Test\Resources\assemblies\hello.cs'
	IL_000f: ldloc.0
	IL_0010: ldc.i4.1
	IL_0011: add
	IL_0012: stloc.0
	IL_0013: ldloc.0
	IL_0014: ldarg.0
	IL_0015: ldlen
	IL_0016: conv.i4
	IL_0017: blt IL_0007
	.line 10,-1:-1,-1 'C:\sources\cecil\symbols\Mono.Cecil.Mdb\Test\Resources\assemblies\hello.cs'
	IL_001c: ldc.i4.0
	IL_001d: ret
", main);
			}, symbolReaderProvider: typeof(MdbReaderProvider), symbolWriterProvider: typeof(MdbWriterProvider));
		}

		[Fact]
		public void RoundTripCoreLib ()
		{
			TestModule ("mscorlib.dll", module => {
				var type = module.GetType ("System.IO.__Error");
				var method = type.GetMethod ("WinIOError");

				Assert.NotNull (method.Body);
			}, verify: !Platform.OnMono, symbolReaderProvider: typeof(MdbReaderProvider), symbolWriterProvider: typeof(MdbWriterProvider));
		}

		[Fact]
		public void PartialClass ()
		{
			TestModule ("BreakpointTest.Portable.dll", module => {
				var type = module.GetType ("BreakpointTest.Portable.TestService/<MyAsyncAction1>c__async3");
				var method = type.GetMethod ("MoveNext");

				Assert.NotNull (method);

				var info = method.DebugInformation;
				(info.SequencePoints.Count).Should().Be(5);
				foreach (var sp in info.SequencePoints)
					(sp.Document.Url).Should().Be(@"C:\tmp\repropartial\BreakpointTest.Portable\TestService.Actions.cs");

				type = module.GetType("BreakpointTest.Portable.TestService/<MyAsyncAction2>c__async2");
				method = type.GetMethod("MoveNext");

				Assert.NotNull(method);

				info = method.DebugInformation;
				(info.SequencePoints.Count).Should().Be(5);
				foreach (var sp in info.SequencePoints)
					(sp.Document.Url).Should().Be(@"C:\tmp\repropartial\BreakpointTest.Portable\TestService.cs");

			}, symbolReaderProvider: typeof(MdbReaderProvider), symbolWriterProvider: typeof(MdbWriterProvider));
		}

		[Fact]
		public void WriteAndReadAgainModuleWithDeterministicMvid ()
		{
			const string resource = "simplemdb.exe";
			string destination = Path.GetTempFileName ();

			using (var module = GetResourceModule (resource, new ReaderParameters { SymbolReaderProvider = new DefaultSymbolReaderProvider (true) })) {
				module.Write (destination, new WriterParameters { WriteSymbols = true, DeterministicMvid = true });
			}

			using (var module = ModuleDefinition.ReadModule (destination, new ReaderParameters { SymbolReaderProvider = new DefaultSymbolReaderProvider (true) })) {
			}
		}
	}
