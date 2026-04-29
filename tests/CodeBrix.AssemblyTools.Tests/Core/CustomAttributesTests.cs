using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using CodeBrix.AssemblyTools;
using CodeBrix.AssemblyTools.Metadata;
using CodeBrix.AssemblyTools.PE;
using Xunit;
using SilverAssertions;
namespace CodeBrix.AssemblyTools.Tests.Core; //was previously: Mono.Cecil.Tests;
public class CustomAttributesTests : BaseTestFixture {

	[Fact]
	public void StringArgumentOnType ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var hamster = module.GetType ("Hamster");

			Assert.True (hamster.HasCustomAttributes);
			(hamster.CustomAttributes.Count).Should().Be(1);

			var attribute = hamster.CustomAttributes [0];
			(attribute.Constructor.FullName).Should().Be("System.Void FooAttribute::.ctor(System.String)");

			Assert.True (attribute.HasConstructorArguments);
			(attribute.ConstructorArguments.Count).Should().Be(1);

			AssertArgument ("bar", attribute.ConstructorArguments [0]);
		});
	}

	[Fact]
	public void NullString ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var dentist = module.GetType ("Dentist");

			var attribute = GetAttribute (dentist, "Foo");
			Assert.NotNull (attribute);

			AssertArgument<string> (null, attribute.ConstructorArguments [0]);
		});
	}

	[Fact]
	public void Primitives1 ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var steven = module.GetType ("Steven");

			var attribute = GetAttribute (steven, "Foo");
			Assert.NotNull (attribute);

			AssertArgument<sbyte> (-12, attribute.ConstructorArguments [0]);
			AssertArgument<byte> (242, attribute.ConstructorArguments [1]);
			AssertArgument<bool> (true, attribute.ConstructorArguments [2]);
			AssertArgument<bool> (false, attribute.ConstructorArguments [3]);
			AssertArgument<ushort> (4242, attribute.ConstructorArguments [4]);
			AssertArgument<short> (-1983, attribute.ConstructorArguments [5]);
			AssertArgument<char> ('c', attribute.ConstructorArguments [6]);
		});
	}

	[Fact]
	public void Primitives2 ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var seagull = module.GetType ("Seagull");

			var attribute = GetAttribute (seagull, "Foo");
			Assert.NotNull (attribute);

			AssertArgument<int> (-100000, attribute.ConstructorArguments [0]);
			AssertArgument<uint> (200000, attribute.ConstructorArguments [1]);
			AssertArgument<float> (12.12f, attribute.ConstructorArguments [2]);
			AssertArgument<long> (long.MaxValue, attribute.ConstructorArguments [3]);
			AssertArgument<ulong> (ulong.MaxValue, attribute.ConstructorArguments [4]);
			AssertArgument<double> (64.646464, attribute.ConstructorArguments [5]);
		});
	}

	[Fact]
	public void StringArgumentOnAssembly ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var assembly = module.Assembly;

			var attribute = GetAttribute (assembly, "Foo");
			Assert.NotNull (attribute);

			AssertArgument ("bingo", attribute.ConstructorArguments [0]);
		});
	}

	[Fact]
	public void CharArray ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var rifle = module.GetType ("Rifle");

			var attribute = GetAttribute (rifle, "Foo");
			Assert.NotNull (attribute);

			var argument = attribute.ConstructorArguments [0];

			(argument.Type.FullName).Should().Be("System.Char[]");

			var array = argument.Value as CustomAttributeArgument [];
			Assert.NotNull (array);

			var str = "cecil";

			(str.Length).Should().Be(array.Length);

			for (int i = 0; i < str.Length; i++)
			AssertArgument (str [i], array [i]);
		});
	}

	[Fact]
	public void BoxedArguments ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var worm = module.GetType ("Worm");

			var attribute = GetAttribute (worm, "Foo");
			Assert.NotNull (attribute);

			(PrettyPrint (attribute)).Should().Be(".ctor ((Object:(String:\"2\")), (Object:(I4:2)))");
		});
	}

	[Fact]
	public void BoxedArraysArguments ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var sheep = module.GetType ("Sheep");

			var attribute = GetAttribute (sheep, "Foo");
			Assert.NotNull (attribute);

			// [Foo (new object [] { "2", 2, 'c' }, new object [] { new object [] { 1, 2, 3}, null })]
			AssertCustomAttribute (".ctor ((Object:(Object[]:{(Object:(String:\"2\")), (Object:(I4:2)), (Object:(Char:'c'))})), (Object:(Object[]:{(Object:(Object[]:{(Object:(I4:1)), (Object:(I4:2)), (Object:(I4:3))})), (Object:(String:null))})))", attribute);
		});
	}

	[Fact]
	public void FieldsAndProperties ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var angola = module.GetType ("Angola");

			var attribute = GetAttribute (angola, "Foo");
			Assert.NotNull (attribute);

			(attribute.Fields.Count).Should().Be(2);

			var argument = attribute.Fields.Where (a => a.Name == "Pan").First ();
			AssertCustomAttributeArgument ("(Object:(Object[]:{(Object:(I4:1)), (Object:(String:\"2\")), (Object:(Char:'3'))}))", argument);

			argument = attribute.Fields.Where (a => a.Name == "PanPan").First ();
			AssertCustomAttributeArgument ("(String[]:{(String:\"yo\"), (String:\"yo\")})", argument);

			(attribute.Properties.Count).Should().Be(2);

			argument = attribute.Properties.Where (a => a.Name == "Bang").First ();
			AssertArgument (42, argument);

			argument = attribute.Properties.Where (a => a.Name == "Fiou").First ();
			AssertArgument<string> (null, argument);
		});
	}

	[Fact]
	public void BoxedStringField ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var type = module.GetType ("BoxedStringField");

			var attribute = GetAttribute (type, "Foo");
			Assert.NotNull (attribute);

			(attribute.Fields.Count).Should().Be(1);

			var argument = attribute.Fields.Where (a => a.Name == "Pan").First ();
			AssertCustomAttributeArgument ("(Object:(String:\"fiouuu\"))", argument);
		});
	}

	[Fact]
	public void TypeDefinitionEnum ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var zero = module.GetType ("Zero");

			var attribute = GetAttribute (zero, "Foo");
			Assert.NotNull (attribute);

			(attribute.ConstructorArguments.Count).Should().Be(1);

			(attribute.ConstructorArguments [0].Value).Should().Be((short) 2);
			(attribute.ConstructorArguments [0].Type.FullName).Should().Be("Bingo");
		});
	}

	[Fact]
	public void TypeReferenceEnum ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var ace = module.GetType ("Ace");

			var attribute = GetAttribute (ace, "Foo");
			Assert.NotNull (attribute);

			(attribute.ConstructorArguments.Count).Should().Be(1);

			(attribute.ConstructorArguments [0].Value).Should().Be((byte) 0x04);
			(attribute.ConstructorArguments [0].Type.FullName).Should().Be("System.Security.AccessControl.AceFlags");
			(attribute.ConstructorArguments [0].Type.Module).Should().Be(module);
		});
	}

	[Fact]
	public void BoxedEnumReference ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var bzzz = module.GetType ("Bzzz");

			var attribute = GetAttribute (bzzz, "Foo");
			Assert.NotNull (attribute);

			// [Foo (new object [] { Bingo.Fuel, Bingo.Binga }, null, Pan = System.Security.AccessControl.AceFlags.NoPropagateInherit)]

			(attribute.ConstructorArguments.Count).Should().Be(2);

			var argument = attribute.ConstructorArguments [0];

			AssertCustomAttributeArgument ("(Object:(Object[]:{(Object:(Bingo:2)), (Object:(Bingo:4))}))", argument);

			argument = attribute.ConstructorArguments [1];

			AssertCustomAttributeArgument ("(Object:(String:null))", argument);

			argument = attribute.Fields.Where (a => a.Name == "Pan").First ().Argument;

			AssertCustomAttributeArgument ("(Object:(System.Security.AccessControl.AceFlags:4))", argument);
		});
	}

	[Fact]
	public void TypeOfTypeDefinition ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var typed = module.GetType ("Typed");

			var attribute = GetAttribute (typed, "Foo");
			Assert.NotNull (attribute);

			(attribute.ConstructorArguments.Count).Should().Be(1);

			var argument = attribute.ConstructorArguments [0];

			(argument.Type.FullName).Should().Be("System.Type");

			var type = argument.Value as TypeDefinition;
			Assert.NotNull (type);

			(type.FullName).Should().Be("Bingo");
		});
	}

	[Fact]
	public void TypeOfNestedTypeDefinition ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var typed = module.GetType ("NestedTyped");

			var attribute = GetAttribute (typed, "Foo");
			Assert.NotNull (attribute);

			(attribute.ConstructorArguments.Count).Should().Be(1);

			var argument = attribute.ConstructorArguments [0];

			(argument.Type.FullName).Should().Be("System.Type");

			var type = argument.Value as TypeDefinition;
			Assert.NotNull (type);

			(type.FullName).Should().Be("FooAttribute/Token");
		});
	}

	[Fact]
	public void FieldTypeOf ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var truc = module.GetType ("Truc");

			var attribute = GetAttribute (truc, "Foo");
			Assert.NotNull (attribute);

			var argument = attribute.Fields.Where (a => a.Name == "Chose").First ().Argument;

			(argument.Type.FullName).Should().Be("System.Type");

			var type = argument.Value as TypeDefinition;
			Assert.NotNull (type);

			(type.FullName).Should().Be("Typed");
		});
	}

	[Fact]
	public void EscapedTypeName ()
	{
		TestModule ("bug-185.dll", module => {
			var foo = module.GetType ("Foo");
			var foo_do = foo.Methods.Where (m => !m.IsConstructor).First ();

			var attribute = foo_do.CustomAttributes.Where (ca => ca.AttributeType.Name == "AsyncStateMachineAttribute").First ();

			(attribute.ConstructorArguments [0].Value).Should().Be(foo.NestedTypes [0]);

			var function = module.GetType ("Function`1");

			var apply = function.Methods.Where(m => !m.IsConstructor).First ();

			attribute = apply.CustomAttributes.Where (ca => ca.AttributeType.Name == "AsyncStateMachineAttribute").First ();

			(attribute.ConstructorArguments [0].Value).Should().Be(function.NestedTypes [0]);
		});
	}

	[Fact]
	public void FieldNullTypeOf ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var truc = module.GetType ("Machin");

			var attribute = GetAttribute (truc, "Foo");
			Assert.NotNull (attribute);

			var argument = attribute.Fields.Where (a => a.Name == "Chose").First ().Argument;

			(argument.Type.FullName).Should().Be("System.Type");

			Assert.Null (argument.Value);
		});
	}

	[Fact]
	public void OpenGenericTypeOf ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var open_generic = module.GetType ("OpenGeneric`2");
			Assert.NotNull (open_generic);

			var attribute = GetAttribute (open_generic, "Foo");
			Assert.NotNull (attribute);

			(attribute.ConstructorArguments.Count).Should().Be(1);

			var argument = attribute.ConstructorArguments [0];

			(argument.Type.FullName).Should().Be("System.Type");

			var type = argument.Value as TypeReference;
			Assert.NotNull (type);

			(type.FullName).Should().Be("System.Collections.Generic.Dictionary`2");
		});
	}

	[Fact]
	public void ClosedGenericTypeOf ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var closed_generic = module.GetType ("ClosedGeneric");
			Assert.NotNull (closed_generic);

			var attribute = GetAttribute (closed_generic, "Foo");
			Assert.NotNull (attribute);

			(attribute.ConstructorArguments.Count).Should().Be(1);

			var argument = attribute.ConstructorArguments [0];

			(argument.Type.FullName).Should().Be("System.Type");

			var type = argument.Value as TypeReference;
			Assert.NotNull (type);

			(type.FullName).Should().Be("System.Collections.Generic.Dictionary`2<System.String,OpenGeneric`2<Machin,System.Int32>[,]>");
		});
	}

	[Fact]
	public void TypeOfArrayOfNestedClass ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var parent = module.GetType ("Parent");
			Assert.NotNull (parent);

			var attribute = GetAttribute (parent, "Foo");
			Assert.NotNull (attribute);

			(attribute.ConstructorArguments.Count).Should().Be(1);

			var argument = attribute.ConstructorArguments [0];

			(argument.Type.FullName).Should().Be("System.Type");

			var type = argument.Value as TypeReference;
			Assert.NotNull (type);

			(type.FullName).Should().Be("Parent/Child[]");
		});
	}

	[Fact]
	public void EmptyBlob ()
	{
		TestIL ("ca-empty-blob.il", module => {
			var attribute = module.GetType ("CustomAttribute");
			(attribute.CustomAttributes.Count).Should().Be(1);
			(attribute.CustomAttributes [0].ConstructorArguments.Count).Should().Be(0);
		}, verify: !Platform.OnMono);
	}

	[Fact]
	public void InterfaceImplementation ()
	{
		OnlyOnWindows (); // Mono's ilasm doesn't support .interfaceimpl

		TestIL ("ca-iface-impl.il", module => {
			var type = module.GetType ("FooType");
			var iface = type.Interfaces.Single (i => i.InterfaceType.FullName == "IFoo");
			Assert.True (iface.HasCustomAttributes);
			var attributes = iface.CustomAttributes;
			(attributes.Count).Should().Be(1);
			(attributes [0].AttributeType.FullName).Should().Be("FooAttribute");
		});
	}

	[Fact]
	public void GenericParameterConstraint ()
	{
		TestModule ("GenericParameterConstraintAttributes.dll", module => {
			var type = module.GetType ("Foo.Library`1");
			var gp = type.GenericParameters.Single ();
			var constraint = gp.Constraints.Single ();

			Assert.True (constraint.HasCustomAttributes);
			var attributes = constraint.CustomAttributes;
			(attributes.Count).Should().Be(1);
			(attributes [0].AttributeType.FullName).Should().Be("System.Runtime.CompilerServices.NullableAttribute");
		}, verify: !Platform.OnMono);
	}

	[Fact]
	public void GenericAttributeString ()
	{
		TestModule ("GenericAttributes.dll", module => {
			var type = module.GetType ("WithGenericAttribute_OfString");
			Assert.True (type.HasCustomAttributes);
			var attributes = type.CustomAttributes;
			(attributes.Count).Should().Be(1);
			(attributes [0].AttributeType.FullName).Should().Be("GenericAttribute`1<System.String>");
			var attribute = attributes [0];
			// constructor arguments
			(attribute.HasConstructorArguments).Should().Be(true);
			var argument = attribute.ConstructorArguments.Single ();
			(argument.Type.FullName).Should().Be("System.String");
			(argument.Value).Should().Be("t");
			// named field argument
			(attribute.HasFields).Should().Be(true);
			var field = attribute.Fields.Single ();
			(field.Name).Should().Be("F");
			(field.Argument.Type.FullName).Should().Be("System.String");
			(field.Argument.Value).Should().Be("f");
			// named property argument
			(attribute.HasProperties).Should().Be(true);
			var property = attribute.Properties.Single ();
			(property.Name).Should().Be("P");
			(property.Argument.Type.FullName).Should().Be("System.String");
			(property.Argument.Value).Should().Be("p");
			
		}, verify: !Platform.OnMono);
	}

	[Fact]
	public void GenericAttributeInt ()
	{
		TestModule ("GenericAttributes.dll", module => {
			var type = module.GetType ("WithGenericAttribute_OfInt");
			Assert.True (type.HasCustomAttributes);
			var attributes = type.CustomAttributes;
			(attributes.Count).Should().Be(1);
			(attributes [0].AttributeType.FullName).Should().Be("GenericAttribute`1<System.Int32>");
			var attribute = attributes [0];
			// constructor arguments
			(attribute.HasConstructorArguments).Should().Be(true);
			var argument = attribute.ConstructorArguments.Single ();
			(argument.Type.FullName).Should().Be("System.Int32");
			(argument.Value).Should().Be(1);
			// named field argument
			(attribute.HasFields).Should().Be(true);
			var field = attribute.Fields.Single ();
			(field.Name).Should().Be("F");
			(field.Argument.Type.FullName).Should().Be("System.Int32");
			(field.Argument.Value).Should().Be(2);
			// named property argument
			(attribute.HasProperties).Should().Be(true);
			var property = attribute.Properties.Single ();
			(property.Name).Should().Be("P");
			(property.Argument.Type.FullName).Should().Be("System.Int32");
			(property.Argument.Value).Should().Be(3);				
		}, verify: !Platform.OnMono);
	}

	[Fact]
	public void ConstrainedGenericAttribute ()
	{
		TestModule ("GenericAttributes.dll", module => {
			var type = module.GetType ("WithConstrainedGenericAttribute");
			Assert.True (type.HasCustomAttributes);
			var attributes = type.CustomAttributes;
			(attributes.Count).Should().Be(1);
			var attribute = attributes [0];
			(attribute.AttributeType.FullName).Should().Be("ConstrainedGenericAttribute`1<DerivedFromConstraintType>");
		}, verify: !Platform.OnMono);
	}

	[Fact]
	public void NullCharInString ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var type = module.GetType ("NullCharInString");
			var attributes = type.CustomAttributes;
			(attributes.Count).Should().Be(1);
			var attribute = attributes [0];
			(attribute.ConstructorArguments.Count).Should().Be(1);
			var value = (string) attribute.ConstructorArguments [0].Value;
			(value.Length).Should().Be(8);
			(value [7]).Should().Be('\0');
		});
	}

	[Fact]
	public void OrderedAttributes ()
	{
		TestModule ("ordered-attrs.exe", module => {
			var type = module.GetType ("Program");
			var method = type.GetMethod ("Main");
			var attributes = method.CustomAttributes;
			(attributes.Count).Should().Be(6);

			(attributes [0].AttributeType.Name).Should().Be("AAttribute");
			(attributes [0].Fields [0].Argument.Value as string).Should().Be("Main.A1");

			(attributes [1].AttributeType.Name).Should().Be("AAttribute");
			(attributes [1].Fields [0].Argument.Value as string).Should().Be("Main.A2");

			(attributes [2].AttributeType.Name).Should().Be("BAttribute");
			(attributes [2].Fields [0].Argument.Value as string).Should().Be("Main.B1");

			(attributes [3].AttributeType.Name).Should().Be("AAttribute");
			(attributes [3].Fields [0].Argument.Value as string).Should().Be("Main.A3");

			(attributes [4].AttributeType.Name).Should().Be("BAttribute");
			(attributes [4].Fields [0].Argument.Value as string).Should().Be("Main.B2");

			(attributes [5].AttributeType.Name).Should().Be("BAttribute");
			(attributes [5].Fields [0].Argument.Value as string).Should().Be("Main.B3");
		});
	}

	[Fact]
	public void DefineCustomAttributeFromBlob ()
	{
		var file = Path.Combine (Path.GetTempPath (), "CaBlob.dll");

		var module = ModuleDefinition.CreateModule ("CaBlob.dll", new ModuleParameters { Kind = ModuleKind.Dll, Runtime = TargetRuntime.Net_2_0 });
		var assembly_title_ctor = module.ImportReference (typeof (System.Reflection.AssemblyTitleAttribute).GetConstructor (new [] {typeof (string)}));

		Assert.NotNull (assembly_title_ctor);

		var buffer = new ByteBuffer ();
		buffer.WriteUInt16 (1); // ca signature

		var title = Encoding.UTF8.GetBytes ("CaBlob");

		buffer.WriteCompressedUInt32 ((uint) title.Length);
		buffer.WriteBytes (title);

		buffer.WriteUInt16 (0); // named arguments

		var blob = new byte [buffer.length];
		Buffer.BlockCopy (buffer.buffer, 0, blob, 0, buffer.length);

		var attribute = new CustomAttribute (assembly_title_ctor, blob);
		module.Assembly.CustomAttributes.Add (attribute);

		module.Write (file);

		module = ModuleDefinition.ReadModule (file);

		attribute = GetAttribute (module.Assembly, "AssemblyTitle");

		Assert.NotNull (attribute);
		((string) attribute.ConstructorArguments [0].Value).Should().Be("CaBlob");

		module.Dispose ();
	}

	[Fact]
	public void BoxedEnumOnGenericArgumentOnType ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var valueEnumGenericType = module.GetType ("BoxedValueEnumOnGenericType");

			Assert.True (valueEnumGenericType.HasCustomAttributes);
			(valueEnumGenericType.CustomAttributes.Count).Should().Be(1);

			var attribute = valueEnumGenericType.CustomAttributes [0];
			(attribute.Constructor.FullName).Should().Be("System.Void FooAttribute::.ctor(System.Object,System.Object)");

			Assert.True (attribute.HasConstructorArguments);
			(attribute.ConstructorArguments.Count).Should().Be(2);

			AssertCustomAttributeArgument ("(Object:(GenericWithEnum`1/OnGenericNumber<System.Int32>:0))", attribute.ConstructorArguments [0]);
			AssertCustomAttributeArgument ("(Object:(GenericWithEnum`1/OnGenericNumber<System.String>:1))", attribute.ConstructorArguments [1]);
		});
	}

	[Fact]
	public void EnumOnGenericArgumentOnType ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var valueEnumGenericType = module.GetType ("ValueEnumOnGenericType");

			Assert.True (valueEnumGenericType.HasCustomAttributes);
			(valueEnumGenericType.CustomAttributes.Count).Should().Be(1);

			var attribute = valueEnumGenericType.CustomAttributes [0];
			(attribute.Constructor.FullName).Should().Be("System.Void FooAttribute::.ctor(GenericWithEnum`1/OnGenericNumber<Bingo>)");

			Assert.True (attribute.HasConstructorArguments);
			(attribute.ConstructorArguments.Count).Should().Be(1);

			AssertCustomAttributeArgument ("(GenericWithEnum`1/OnGenericNumber<Bingo>:1)", attribute.ConstructorArguments [0]);
		});
	}

	[Fact]
	public void EnumOnGenericFieldOnType ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var valueEnumGenericType = module.GetType ("FieldEnumOnGenericType");

			Assert.True (valueEnumGenericType.HasCustomAttributes);
			(valueEnumGenericType.CustomAttributes.Count).Should().Be(1);

			var attribute = valueEnumGenericType.CustomAttributes [0];
			var argument = attribute.Fields.Where (a => a.Name == "NumberEnumField").First ().Argument;

			AssertCustomAttributeArgument ("(GenericWithEnum`1/OnGenericNumber<System.Byte>:0)", argument);
		});
	}

	[Fact]
	public void EnumOnGenericPropertyOnType ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var valueEnumGenericType = module.GetType ("PropertyEnumOnGenericType");

			Assert.True (valueEnumGenericType.HasCustomAttributes);
			(valueEnumGenericType.CustomAttributes.Count).Should().Be(1);

			var attribute = valueEnumGenericType.CustomAttributes [0];
			var argument = attribute.Properties.Where (a => a.Name == "NumberEnumProperty").First ().Argument;

			AssertCustomAttributeArgument ("(GenericWithEnum`1/OnGenericNumber<System.Byte>:1)", argument);
		});
	}

	[Fact]
	public void EnumDeclaredInGenericTypeArray ()
	{
		TestCSharp ("CustomAttributes.cs", module => {
			var type = module.GetType ("WithAttributeUsingNestedEnumArray");
			var attributes = type.CustomAttributes;
			(attributes.Count).Should().Be(1);
			var attribute = attributes [0];
			(attribute.Fields.Count).Should().Be(1);
			var arg = attribute.Fields [0].Argument;
			(arg.Type.FullName).Should().Be("System.Object");

			var argumentValue = (CustomAttributeArgument)arg.Value;
			(argumentValue.Type.FullName).Should().Be("GenericWithEnum`1/OnGenericNumber<System.String>[]");
			var argumentValues = (CustomAttributeArgument [])argumentValue.Value;

			(argumentValues [0].Type.FullName).Should().Be("GenericWithEnum`1/OnGenericNumber<System.String>");
			((int)argumentValues [0].Value).Should().Be(0);

			(argumentValues [1].Type.FullName).Should().Be("GenericWithEnum`1/OnGenericNumber<System.String>");
			((int)argumentValues [1].Value).Should().Be(1);
		});
	}

	static void AssertCustomAttribute (string expected, CustomAttribute attribute)
	{
		(PrettyPrint (attribute)).Should().Be(expected);
	}

	static void AssertCustomAttributeArgument (string expected, CustomAttributeNamedArgument named_argument)
	{
		AssertCustomAttributeArgument (expected, named_argument.Argument);
	}

	static void AssertCustomAttributeArgument (string expected, CustomAttributeArgument argument)
	{
		var result = new StringBuilder ();
		PrettyPrint (argument, result);

		(result.ToString ()).Should().Be(expected);
	}

	static string PrettyPrint (CustomAttribute attribute)
	{
		var signature = new StringBuilder ();
		signature.Append (".ctor (");

		for (int i = 0; i < attribute.ConstructorArguments.Count; i++) {
			if (i > 0)
				signature.Append (", ");

			PrettyPrint (attribute.ConstructorArguments [i], signature);
		}

		signature.Append (")");
		return signature.ToString ();
	}

	static void PrettyPrint (CustomAttributeArgument argument, StringBuilder signature)
	{
		var value = argument.Value;

		signature.Append ("(");

		PrettyPrint (argument.Type, signature);

		signature.Append (":");

		PrettyPrintValue (argument.Value, signature);

		signature.Append (")");
	}

	static void PrettyPrintValue (object value, StringBuilder signature)
	{
		if (value == null) {
			signature.Append ("null");
			return;
		}

		var arguments = value as CustomAttributeArgument [];
		if (arguments != null) {
			signature.Append ("{");
			for (int i = 0; i < arguments.Length; i++) {
				if (i > 0)
					signature.Append (", ");

				PrettyPrint (arguments [i], signature);
			}
			signature.Append ("}");

			return;
		}

		switch (Type.GetTypeCode (value.GetType ())) {
		case System.TypeCode.String:
			signature.AppendFormat ("\"{0}\"", value);
			break;
		case System.TypeCode.Char:
			signature.AppendFormat ("'{0}'", (char) value);
			break;
		default:
			var formattable = value as IFormattable;
			if (formattable != null) {
				signature.Append (formattable.ToString (null, CultureInfo.InvariantCulture));
				return;
			}

			if (value is CustomAttributeArgument) {
				PrettyPrint ((CustomAttributeArgument) value, signature);
				return;
			}
			break;
		}
	}

	static void PrettyPrint (TypeReference type, StringBuilder signature)
	{
		if (type.IsArray) {
			ArrayType array = (ArrayType) type;
			signature.AppendFormat ("{0}[]", array.ElementType.etype.ToString ());
		} else if (type.etype == ElementType.None || type.etype == ElementType.GenericInst) {
			signature.Append (type.FullName);
		} else
			signature.Append (type.etype.ToString ());
	}

	static void AssertArgument<T> (T value, CustomAttributeNamedArgument named_argument)
	{
		AssertArgument (value, named_argument.Argument);
	}

	static void AssertArgument<T> (T value, CustomAttributeArgument argument)
	{
		AssertArgument (typeof (T).FullName, (object) value, argument);
	}

	static void AssertArgument (string type, object value, CustomAttributeArgument argument)
	{
		(argument.Type.FullName).Should().Be(type);
		(argument.Value).Should().Be(value);
	}

	static CustomAttribute GetAttribute (ICustomAttributeProvider owner, string type)
	{
		Assert.True (owner.HasCustomAttributes);

		foreach (var attribute in owner.CustomAttributes)
			if (attribute.Constructor.DeclaringType.Name.StartsWith (type))
				return attribute;

		return null;
	}
}
