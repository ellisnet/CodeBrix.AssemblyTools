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
public class SecurityDeclarationTests : BaseTestFixture {

	[Fact]
	public void XmlSecurityDeclaration ()
	{
		TestModule ("decsec-xml.dll", module => {
			var type = module.GetType ("SubLibrary");

			Assert.True (type.HasSecurityDeclarations);

			(type.SecurityDeclarations.Count).Should().Be(1);

			var declaration = type.SecurityDeclarations [0];
			(declaration.Action).Should().Be(SecurityAction.Deny);

			(declaration.SecurityAttributes.Count).Should().Be(1);

			var attribute = declaration.SecurityAttributes [0];

			(attribute.AttributeType.FullName).Should().Be("System.Security.Permissions.PermissionSetAttribute");

			(attribute.Properties.Count).Should().Be(1);

			var named_argument = attribute.Properties [0];

			(named_argument.Name).Should().Be("XML");

			var argument = named_argument.Argument;

			(argument.Type.FullName).Should().Be("System.String");

			const string permission_set = "<PermissionSet class=\"System.Security.PermissionSe"
			+ "t\"\r\nversion=\"1\">\r\n<IPermission class=\"System.Security.Permis"
			+ "sions.SecurityPermission, mscorlib, Version=2.0.0.0, Culture"
			+ "=neutral, PublicKeyToken=b77a5c561934e089\"\r\nversion=\"1\"\r\nFla"
			+ "gs=\"UnmanagedCode\"/>\r\n</PermissionSet>\r\n";

			(argument.Value).Should().Be(permission_set);
		});
	}

	[Fact]
	public void XmlNet_1_1SecurityDeclaration ()
	{
		TestModule ("decsec1-xml.dll", module => {
			var type = module.GetType ("SubLibrary");

			Assert.True (type.HasSecurityDeclarations);

			(type.SecurityDeclarations.Count).Should().Be(1);

			var declaration = type.SecurityDeclarations [0];
			(declaration.Action).Should().Be(SecurityAction.Deny);

			(declaration.SecurityAttributes.Count).Should().Be(1);

			var attribute = declaration.SecurityAttributes [0];

			(attribute.AttributeType.FullName).Should().Be("System.Security.Permissions.PermissionSetAttribute");

			(attribute.Properties.Count).Should().Be(1);

			var named_argument = attribute.Properties [0];

			(named_argument.Name).Should().Be("XML");

			var argument = named_argument.Argument;

			(argument.Type.FullName).Should().Be("System.String");

			const string permission_set = "<PermissionSet class=\"System.Security.PermissionSe"
			+ "t\"\r\nversion=\"1\">\r\n<IPermission class=\"System.Security.Permis"
			+ "sions.SecurityPermission, mscorlib, Version=1.0.0.0, Culture"
			+ "=neutral, PublicKeyToken=b77a5c561934e089\"\r\nversion=\"1\"\r\nFla"
			+ "gs=\"UnmanagedCode\"/>\r\n</PermissionSet>\r\n";

			(argument.Value).Should().Be(permission_set);
		});
	}

	[Fact]
	public void DefineSecurityDeclarationByBlob ()
	{
		var file = Path.Combine(Path.GetTempPath(), "SecDecBlob.dll");
		var module = ModuleDefinition.CreateModule ("SecDecBlob.dll", new ModuleParameters { Kind = ModuleKind.Dll, Runtime = TargetRuntime.Net_2_0 });

		const string permission_set = "<PermissionSet class=\"System.Security.PermissionSe"
			+ "t\"\r\nversion=\"1\">\r\n<IPermission class=\"System.Security.Permis"
			+ "sions.SecurityPermission, mscorlib, Version=2.0.0.0, Culture"
			+ "=neutral, PublicKeyToken=b77a5c561934e089\"\r\nversion=\"1\"\r\nFla"
			+ "gs=\"UnmanagedCode\"/>\r\n</PermissionSet>\r\n";

		var declaration = new SecurityDeclaration (SecurityAction.Deny, Encoding.Unicode.GetBytes (permission_set));
		module.Assembly.SecurityDeclarations.Add (declaration);

		module.Write (file);
		module = ModuleDefinition.ReadModule (file);

		declaration = module.Assembly.SecurityDeclarations [0];
		(declaration.Action).Should().Be(SecurityAction.Deny);
		(declaration.SecurityAttributes.Count).Should().Be(1);

		var attribute = declaration.SecurityAttributes [0];
		(attribute.AttributeType.FullName).Should().Be("System.Security.Permissions.PermissionSetAttribute");
		(attribute.Properties.Count).Should().Be(1);

		var named_argument = attribute.Properties [0];
		(named_argument.Name).Should().Be("XML");
		var argument = named_argument.Argument;
		(argument.Type.FullName).Should().Be("System.String");
		(argument.Value).Should().Be(permission_set);
	}

	[Fact]
	public void SecurityDeclarationWithoutAttributes ()
	{
		TestModule ("empty-decsec-att.dll", module => {
			var type = module.GetType ("TestSecurityAction.ModalUITypeEditor");
			var method = type.GetMethod ("GetEditStyle");

			Assert.NotNull (method);

			(method.SecurityDeclarations.Count).Should().Be(1);

			var declaration = method.SecurityDeclarations [0];
			(declaration.Action).Should().Be(SecurityAction.LinkDemand);
			(declaration.SecurityAttributes.Count).Should().Be(1);

			var attribute = declaration.SecurityAttributes [0];
			(attribute.AttributeType.FullName).Should().Be("System.Security.Permissions.SecurityPermissionAttribute");
			(attribute.Fields.Count).Should().Be(0);
			(attribute.Properties.Count).Should().Be(0);
		});
	}

	[Fact]
	public void AttributeSecurityDeclaration ()
	{
		TestModule ("decsec-att.dll", module => {
			var type = module.GetType ("SubLibrary");

			Assert.True (type.HasSecurityDeclarations);

			(type.SecurityDeclarations.Count).Should().Be(1);

			var declaration = type.SecurityDeclarations [0];
			(declaration.Action).Should().Be(SecurityAction.Deny);

			(declaration.SecurityAttributes.Count).Should().Be(1);

			var attribute = declaration.SecurityAttributes [0];

			(attribute.AttributeType.FullName).Should().Be("System.Security.Permissions.SecurityPermissionAttribute");

			(attribute.Properties.Count).Should().Be(1);

			var named_argument = attribute.Properties [0];

			(named_argument.Name).Should().Be("UnmanagedCode");

			var argument = named_argument.Argument;

			(argument.Type.FullName).Should().Be("System.Boolean");

			(argument.Value).Should().Be(true);
		});
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
		} else if (type.etype == ElementType.None) {
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
}
