using System;
using System.IO;
using Xunit;
using SilverAssertions;
using CodeBrix.AssemblyTools.Cil;
using CodeBrix.AssemblyTools.Mdb;
using CodeBrix.AssemblyTools.Pdb;

namespace CodeBrix.AssemblyTools.Tests.Core; //was previously: Mono.Cecil.Tests;
public class SymbolTests : BaseTestFixture {

	[Fact]
	public void DefaultPdb ()
	{
		TestModule ("libpdb.dll", module => {
			Assert.True (module.HasSymbols);
			(module.SymbolReader.GetType ()).Should().Be(typeof (NativePdbReader));
		}, readOnly: !Platform.HasNativePdbSupport, symbolReaderProvider: typeof (DefaultSymbolReaderProvider), symbolWriterProvider: typeof (DefaultSymbolWriterProvider));
	}

	[Fact]
	public void DefaultMdb ()
	{
		TestModule ("libmdb.dll", module => {
			Assert.True (module.HasSymbols);
			(module.SymbolReader.GetType ()).Should().Be(typeof (MdbReader));
		}, symbolReaderProvider: typeof (DefaultSymbolReaderProvider), symbolWriterProvider: typeof (DefaultSymbolWriterProvider));
	}

	[Fact]
	public void DefaultPortablePdb ()
	{
		TestModule ("PdbTarget.exe", module => {
			Assert.True (module.HasSymbols);
			(module.SymbolReader.GetType ()).Should().Be(typeof (PortablePdbReader));
		}, symbolReaderProvider: typeof (DefaultSymbolReaderProvider), symbolWriterProvider: typeof (DefaultSymbolWriterProvider));
	}

	[Fact]
	public void DefaultEmbeddedPortablePdb ()
	{
		TestModule ("EmbeddedPdbTarget.exe", module => {
			Assert.True (module.HasSymbols);
			(module.SymbolReader.GetType ()).Should().Be(typeof (PortablePdbReader));
		}, symbolReaderProvider: typeof (DefaultSymbolReaderProvider), symbolWriterProvider: typeof (DefaultSymbolWriterProvider), verify: !Platform.OnMono);
	}

	[Fact]
	public void MdbMismatch ()
	{
		Assert.Throws<SymbolsNotMatchingException> (() => GetResourceModule ("mdb-mismatch.dll", new ReaderParameters { SymbolReaderProvider = new MdbReaderProvider () }));
	}

	[Fact]
	public void MdbIgnoreMismatch()
	{
		using (var module = GetResourceModule ("mdb-mismatch.dll", new ReaderParameters { SymbolReaderProvider = new MdbReaderProvider (), ThrowIfSymbolsAreNotMatching = false })) {
			Assert.Null (module.SymbolReader);
			Assert.False (module.HasSymbols);
		}
	}

	[Fact]
	public void PortablePdbMismatch ()
	{
		Assert.Throws<SymbolsNotMatchingException> (() => GetResourceModule ("pdb-mismatch.dll", new ReaderParameters { SymbolReaderProvider = new PortablePdbReaderProvider () }));
	}

	[Fact]
	public void PortablePdbIgnoreMismatch()
	{
		using (var module = GetResourceModule ("pdb-mismatch.dll", new ReaderParameters { SymbolReaderProvider = new PortablePdbReaderProvider (), ThrowIfSymbolsAreNotMatching = false })) {
			Assert.Null (module.SymbolReader);
			Assert.False (module.HasSymbols);
		}
	}

	[Fact]
	public void DefaultPortablePdbStream ()
	{
		using (var symbolStream = GetResourceStream ("PdbTarget.pdb")) {
			var parameters = new ReaderParameters {
				SymbolReaderProvider = new PortablePdbReaderProvider (),
				SymbolStream = symbolStream,
			};

			using (var module = GetResourceModule ("PdbTarget.exe", parameters)) {
				Assert.NotNull (module.SymbolReader);
				Assert.True (module.HasSymbols);
				(module.SymbolReader.GetType ()).Should().Be(typeof (PortablePdbReader));
			}
		}
	}

	[Fact]
	public void DefaultPdbStream ()
	{
		using (var symbolStream = GetResourceStream ("libpdb.pdb")) {
			var parameters = new ReaderParameters {
				SymbolReaderProvider = new NativePdbReaderProvider (),
				SymbolStream = symbolStream,
			};

			using (var module = GetResourceModule ("libpdb.dll", parameters)) {
				Assert.NotNull (module.SymbolReader);
				Assert.True (module.HasSymbols);
				(module.SymbolReader.GetType ()).Should().Be(typeof (NativePdbReader));
			}
		}
	}

	[Fact]
	public void DefaultMdbStream ()
	{
		using (var symbolStream = GetResourceStream ("libmdb.dll.mdb")) {
			var parameters = new ReaderParameters {
				SymbolReaderProvider = new MdbReaderProvider (),
				SymbolStream = symbolStream,
			};

			using (var module = GetResourceModule ("libmdb.dll", parameters)) {
				Assert.NotNull (module.SymbolReader);
				Assert.True (module.HasSymbols);
				(module.SymbolReader.GetType ()).Should().Be(typeof (MdbReader));
			}
		}
	}

	[Fact]
	public void MultipleCodeViewEntries ()
	{
		using (var module = GetResourceModule ("System.Private.Xml.dll", new ReaderParameters { ReadSymbols = true })) {
			Assert.True (module.HasSymbols);
			Assert.NotNull (module.SymbolReader);
		}
	}
}
