using System;
using System.IO;

using CodeBrix.AssemblyTools.PE;
using Xunit;
using SilverAssertions;
namespace CodeBrix.AssemblyTools.Tests.Core; //was previously: Mono.Cecil.Tests;
public class FieldTests : BaseTestFixture {

	[Fact]
	public void TypeDefField ()
	{
		TestCSharp ("Fields.cs", module => {
			var type = module.Types [1];
			(type.Name).Should().Be("Foo");
			(type.Fields.Count).Should().Be(1);

			var field = type.Fields [0];
			(field.Name).Should().Be("bar");
			(field.MetadataToken.RID).Should().Be(1);
			Assert.NotNull (field.FieldType);
			(field.FieldType.FullName).Should().Be("Bar");
			(field.MetadataToken.TokenType).Should().Be(TokenType.Field);
			Assert.False (field.HasConstant);
			Assert.Null (field.Constant);
		});
	}

	[Fact]
	public void PrimitiveTypes ()
	{
		TestCSharp ("Fields.cs", module => {
			var type = module.GetType ("Baz");

			AssertField (type, "char", typeof (char));
			AssertField (type, "bool", typeof (bool));
			AssertField (type, "sbyte", typeof (sbyte));
			AssertField (type, "byte", typeof (byte));
			AssertField (type, "int16", typeof (short));
			AssertField (type, "uint16", typeof (ushort));
			AssertField (type, "int32", typeof (int));
			AssertField (type, "uint32", typeof (uint));
			AssertField (type, "int64", typeof (long));
			AssertField (type, "uint64", typeof (ulong));
			AssertField (type, "single", typeof (float));
			AssertField (type, "double", typeof (double));
			AssertField (type, "string", typeof (string));
			AssertField (type, "object", typeof (object));
		});
	}

	[Fact]
	public void VolatileField ()
	{
		TestCSharp ("Fields.cs", module => {
			var type = module.GetType ("Bar");

			Assert.True (type.HasFields);
			(type.Fields.Count).Should().Be(1);

			var field = type.Fields [0];

			(field.Name).Should().Be("oiseau");
			(field.FieldType.FullName).Should().Be("System.Int32 modreq(System.Runtime.CompilerServices.IsVolatile)");

			Assert.False (field.HasConstant);
		});
	}

	[Fact]
	public void FieldLayout ()
	{
		TestCSharp ("Layouts.cs", module => {
			var foo = module.GetType ("Foo");
			Assert.NotNull (foo);

			Assert.True (foo.HasFields);

			var fields = foo.Fields;

			var field = fields [0];

			(field.Name).Should().Be("Bar");
			Assert.True (field.HasLayoutInfo);
			(field.Offset).Should().Be(0);

			field = fields [1];

			(field.Name).Should().Be("Baz");
			Assert.True (field.HasLayoutInfo);
			(field.Offset).Should().Be(2);

			field = fields [2];

			(field.Name).Should().Be("Gazonk");
			Assert.True (field.HasLayoutInfo);
			(field.Offset).Should().Be(4);
		});
	}

	[Fact]
	public void FieldRVA ()
	{
		TestCSharp ("Layouts.cs", module => {
			var priv_impl = GetPrivateImplementationType (module);
			Assert.NotNull (priv_impl);

			(priv_impl.Fields.Count).Should().Be(1);

			var field = priv_impl.Fields [0];

			Assert.NotNull (field);
			(field.RVA).Should().NotBe(0);
			Assert.NotNull (field.InitialValue);
			(field.InitialValue.Length).Should().Be(16);

			var buffer = new ByteBuffer (field.InitialValue);

			(buffer.ReadUInt32 ()).Should().Be(1);
			(buffer.ReadUInt32 ()).Should().Be(2);
			(buffer.ReadUInt32 ()).Should().Be(3);
			(buffer.ReadUInt32 ()).Should().Be(4);

			var intialValue = field.InitialValue;
			field.InitialValue = null;
			Assert.False (field.Attributes.HasFlag (FieldAttributes.HasFieldRVA));

			field.InitialValue = intialValue;

			Assert.True (field.Attributes.HasFlag (FieldAttributes.HasFieldRVA));
		});
	}

	int AlignmentOfInteger(int input)
	{
		if (input == 0)
			return 0x40000000;
		if (input < 0)
			Assert.Fail ();
		int alignment = 1;
		while ((input & alignment) == 0)
			alignment *= 2;

		return alignment;
	}

	[Fact]
	public void FieldRVAAlignment ()
	{
		TestIL ("FieldRVAAlignment.il", ilmodule => {

			var path = Path.GetTempFileName ();

			ilmodule.Write (path);

			using (var module = ModuleDefinition.ReadModule (path, new ReaderParameters { ReadWrite = true })) {
				var priv_impl = GetPrivateImplementationType (module);
				Assert.NotNull (priv_impl);

				(priv_impl.Fields.Count).Should().Be(8);

				foreach (var field in priv_impl.Fields)
				{
					Assert.NotNull (field);

					(field.RVA).Should().NotBe(0);
					Assert.NotNull (field.InitialValue);

					int rvaAlignment = AlignmentOfInteger (field.RVA);
					var fieldType = field.FieldType.Resolve ();
					int desiredAlignment = fieldType.PackingSize;
					Assert.True(rvaAlignment >= desiredAlignment);
				}
			}
		});
	}

	[Fact]
	public void GenericFieldDefinition ()
	{
		TestCSharp ("Generics.cs", module => {
			var bar = module.GetType ("Bar`1");
			Assert.NotNull (bar);

			Assert.True (bar.HasGenericParameters);
			var t = bar.GenericParameters [0];

			(t.Name).Should().Be("T");
			(bar).Should().Be(t.Owner);

			var bang = bar.GetField ("bang");

			Assert.NotNull (bang);

			(bang.FieldType).Should().Be(t);
		});
	}

	[Fact]
	public void ArrayFields ()
	{
		TestIL ("types.il", module => {
			var types = module.GetType ("Types");
			Assert.NotNull (types);

			var rank_two = types.GetField ("rank_two");

			var array = rank_two.FieldType as ArrayType;
			Assert.NotNull (array);

			(array.Rank).Should().Be(2);
			Assert.False (array.Dimensions [0].IsSized);
			Assert.False (array.Dimensions [1].IsSized);

			var rank_two_low_bound_zero = types.GetField ("rank_two_low_bound_zero");

			array = rank_two_low_bound_zero.FieldType as ArrayType;
			Assert.NotNull (array);

			(array.Rank).Should().Be(2);
			Assert.True (array.Dimensions [0].IsSized);
			(array.Dimensions [0].LowerBound).Should().Be(0);
			(array.Dimensions [0].UpperBound).Should().Be(null);
			Assert.True (array.Dimensions [1].IsSized);
			(array.Dimensions [1].LowerBound).Should().Be(0);
			(array.Dimensions [1].UpperBound).Should().Be(null);

			var rank_one_low_bound_m1 = types.GetField ("rank_one_low_bound_m1");
			array = rank_one_low_bound_m1.FieldType as ArrayType;
			Assert.NotNull (array);

			(array.Rank).Should().Be(1);
			Assert.True (array.Dimensions [0].IsSized);
			(array.Dimensions [0].LowerBound).Should().Be(-1);
			(array.Dimensions [0].UpperBound).Should().Be(4);
		});
	}

	[Fact]
	public void EnumFieldsConstant ()
	{
		TestCSharp ("Fields.cs", module => {
			var pim = module.GetType ("Pim");
			Assert.NotNull (pim);

			var field = pim.GetField ("Pam");
			Assert.True (field.HasConstant);
			((int) field.Constant).Should().Be(1);

			field = pim.GetField ("Poum");
			((int) field.Constant).Should().Be(2);
		});
	}

	[Fact]
	public void StringAndClassConstant ()
	{
		TestCSharp ("Fields.cs", module => {
			var panpan = module.GetType ("PanPan");
			Assert.NotNull (panpan);

			var field = panpan.GetField ("Peter");
			Assert.True (field.HasConstant);
			Assert.Null (field.Constant);

			field = panpan.GetField ("QQ");
			((string) field.Constant).Should().Be("qq");

			field = panpan.GetField ("nil");
			((string) field.Constant).Should().Be(null);
		});
	}

	[Fact]
	public void ObjectConstant ()
	{
		TestCSharp ("Fields.cs", module => {
			var panpan = module.GetType ("PanPan");
			Assert.NotNull (panpan);

			var field = panpan.GetField ("obj");
			Assert.True (field.HasConstant);
			Assert.Null (field.Constant);
		});
	}

	[Fact]
	public void NullPrimitiveConstant ()
	{
		TestIL ("types.il", module => {
			var fields = module.GetType ("Fields");

			var field = fields.GetField ("int32_nullref");
			Assert.True (field.HasConstant);
			(field.Constant).Should().Be(null);
		});
	}

	[Fact]
	public void ArrayConstant ()
	{
		TestCSharp ("Fields.cs", module => {
			var panpan = module.GetType ("PanPan");
			Assert.NotNull (panpan);

			var field = panpan.GetField ("ints");
			Assert.True (field.HasConstant);
			Assert.Null (field.Constant);
		});
	}

	[Fact]
	public void ConstantCoalescing ()
	{
		TestIL ("types.il", module => {
			var fields = module.GetType ("Fields");

			var field = fields.GetField ("int32_int16");
			(field.FieldType.FullName).Should().Be("System.Int32");
			Assert.True (field.HasConstant);
			Assert.IsAssignableFrom<short>(field.Constant);
			(field.Constant).Should().Be((short) 1);

			field = fields.GetField ("int16_int32");
			(field.FieldType.FullName).Should().Be("System.Int16");
			Assert.True (field.HasConstant);
			Assert.IsAssignableFrom<int>(field.Constant);
			(field.Constant).Should().Be(1);

			field = fields.GetField ("char_int16");
			(field.FieldType.FullName).Should().Be("System.Char");
			Assert.True (field.HasConstant);
			Assert.IsAssignableFrom<short>(field.Constant);
			(field.Constant).Should().Be((short) 1);

			field = fields.GetField ("int16_char");
			(field.FieldType.FullName).Should().Be("System.Int16");
			Assert.True (field.HasConstant);
			Assert.IsAssignableFrom<char>(field.Constant);
			(field.Constant).Should().Be('s');
		});
	}

	[Fact]
	public void NestedEnumOfGenericTypeDefinition ()
	{
		TestCSharp ("Generics.cs", module => {
			var dang = module.GetType ("Bongo`1/Dang");
			Assert.NotNull (dang);

			var field = dang.GetField ("Ding");
			Assert.NotNull (field);
			(field.Constant).Should().Be(2);

			field = dang.GetField ("Dong");
			Assert.NotNull (field);
			(field.Constant).Should().Be(12);
		});
	}

	[Fact]
	public void MarshalAsFixedStr ()
	{
		TestModule ("marshal.dll", module => {
			var boc = module.GetType ("Boc");
			var field = boc.GetField ("a");

			Assert.NotNull (field);

			Assert.True (field.HasMarshalInfo);

			var info = (FixedSysStringMarshalInfo) field.MarshalInfo;

			(info.Size).Should().Be(42);
		});
	}

	[Fact]
	public void MarshalAsFixedArray ()
	{
		TestModule ("marshal.dll", module => {
			var boc = module.GetType ("Boc");
			var field = boc.GetField ("b");

			Assert.NotNull (field);

			Assert.True (field.HasMarshalInfo);

			var info = (FixedArrayMarshalInfo) field.MarshalInfo;

			(info.Size).Should().Be(12);
			(info.ElementType).Should().Be(NativeType.Boolean);
		});
	}

	[Fact]
	public void UnattachedField ()
	{
		var field = new FieldDefinition ("Field", FieldAttributes.Public, typeof (int).ToDefinition ());

		Assert.False (field.HasConstant);
		Assert.Null (field.Constant);
	}

	static TypeDefinition GetPrivateImplementationType (ModuleDefinition module)
	{
		foreach (var type in module.Types)
			if (type.FullName.Contains ("<PrivateImplementationDetails>"))
				return type;

		return null;
	}

	static void AssertField (TypeDefinition type, string name, Type expected)
	{
		var field = type.GetField (name);
		(field).Should().NotBeNull(name);

		(field.FieldType.FullName).Should().Be(expected.FullName);
	}
}
