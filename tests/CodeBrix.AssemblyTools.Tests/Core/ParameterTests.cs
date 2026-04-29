using System;
using Xunit;
using SilverAssertions;
namespace CodeBrix.AssemblyTools.Tests.Core; //was previously: Mono.Cecil.Tests;
public class ParameterTests : BaseTestFixture {

	[Fact]
	public void MarshalAsI4 ()
	{
		TestModule ("marshal.dll", module => {
			var bar = module.GetType ("Bar");
			var pan = bar.GetMethod ("Pan");

			(pan.Parameters.Count).Should().Be(1);

			var parameter = pan.Parameters [0];

			Assert.True (parameter.HasMarshalInfo);
			var info = parameter.MarshalInfo;

			(info.GetType ()).Should().Be(typeof (MarshalInfo));
			(info.NativeType).Should().Be(NativeType.I4);
		});
	}

	[Fact]
	public void CustomMarshaler ()
	{
		TestModule ("marshal.dll", module => {
			var bar = module.GetType ("Bar");
			var pan = bar.GetMethod ("PanPan");

			var parameter = pan.Parameters [0];

			Assert.True (parameter.HasMarshalInfo);

			var info = (CustomMarshalInfo) parameter.MarshalInfo;

			(info.Guid).Should().Be(Guid.Empty);
			(info.UnmanagedType).Should().Be(string.Empty);
			(info.NativeType).Should().Be(NativeType.CustomMarshaler);
			(info.Cookie).Should().Be("nomnom");

			(info.ManagedType.FullName).Should().Be("Boc");
			(info.ManagedType.Scope).Should().Be(module);
		});
	}

	[Fact]
	public void SafeArrayMarshaler ()
	{
		TestModule ("marshal.dll", module => {
			var bar = module.GetType ("Bar");
			var pan = bar.GetMethod ("PanPan");

			Assert.True (pan.MethodReturnType.HasMarshalInfo);

			var info = (SafeArrayMarshalInfo) pan.MethodReturnType.MarshalInfo;

			(info.ElementType).Should().Be(VariantType.Dispatch);
		});
	}

	[Fact]
	public void ArrayMarshaler ()
	{
		TestModule ("marshal.dll", module => {
			var bar = module.GetType ("Bar");
			var pan = bar.GetMethod ("PanPan");

			var parameter = pan.Parameters [1];

			Assert.True (parameter.HasMarshalInfo);

			var info = (ArrayMarshalInfo) parameter.MarshalInfo;

			(info.ElementType).Should().Be(NativeType.I8);
			(info.Size).Should().Be(66);
			(info.SizeParameterIndex).Should().Be(2);

			parameter = pan.Parameters [3];

			Assert.True (parameter.HasMarshalInfo);

			info = (ArrayMarshalInfo) parameter.MarshalInfo;

			(info.ElementType).Should().Be(NativeType.I2);
			(info.Size).Should().Be(-1);
			(info.SizeParameterIndex).Should().Be(-1);
		});
	}

	[Fact]
	public void ArrayMarshalerSized ()
	{
		TestModule ("marshal.dll", module => {
			var delegate_type = module.GetType ("SomeMethod");
			var parameter = delegate_type.GetMethod ("Invoke").Parameters [1];

			Assert.True (parameter.HasMarshalInfo);
			var array_info = (ArrayMarshalInfo) parameter.MarshalInfo;

			Assert.NotNull (array_info);

			(array_info.SizeParameterMultiplier).Should().Be(0);
		});
	}

	[Fact]
	public void NullableConstant ()
	{
		TestModule ("nullable-constant.exe", module => {
			var type = module.GetType ("Program");
			var method = type.GetMethod ("Foo");

			Assert.True (method.Parameters [0].HasConstant);
			Assert.True (method.Parameters [1].HasConstant);
			Assert.True (method.Parameters [2].HasConstant);

			(method.Parameters [0].Constant).Should().Be(1234);
			(method.Parameters [1].Constant).Should().Be(null);
			(method.Parameters [2].Constant).Should().Be(12);
		});
	}

	[Fact]
	public void BoxedDefaultArgumentValue ()
	{
		TestModule ("boxedoptarg.dll", module => {
			var foo = module.GetType ("Foo");
			var bar = foo.GetMethod ("Bar");
			var baz = bar.Parameters [0];

			Assert.True (baz.HasConstant);
			(baz.Constant).Should().Be(-1);
		});
	}

	[Fact]
	public void AddParameterIndex ()
	{
		var object_ref = new TypeReference ("System", "Object", null, null, false);
		var method = new MethodDefinition ("foo", MethodAttributes.Static, object_ref);

		var x = new ParameterDefinition ("x", ParameterAttributes.None, object_ref);
		var y = new ParameterDefinition ("y", ParameterAttributes.None, object_ref);

		method.Parameters.Add (x);
		method.Parameters.Add (y);

		(x.Index).Should().Be(0);
		(y.Index).Should().Be(1);

		(x.Method).Should().Be(method);
		(y.Method).Should().Be(method);
	}

	[Fact]
	public void RemoveAtParameterIndex ()
	{
		var object_ref = new TypeReference ("System", "Object", null, null, false);
		var method = new MethodDefinition ("foo", MethodAttributes.Static, object_ref);

		var x = new ParameterDefinition ("x", ParameterAttributes.None, object_ref);
		var y = new ParameterDefinition ("y", ParameterAttributes.None, object_ref);
		var z = new ParameterDefinition ("y", ParameterAttributes.None, object_ref);

		method.Parameters.Add (x);
		method.Parameters.Add (y);
		method.Parameters.Add (z);

		(x.Index).Should().Be(0);
		(y.Index).Should().Be(1);
		(z.Index).Should().Be(2);

		method.Parameters.RemoveAt (1);

		(x.Index).Should().Be(0);
		(y.Index).Should().Be(-1);
		(z.Index).Should().Be(1);
	}

	[Fact]
	public void RemoveParameterIndex ()
	{
		var object_ref = new TypeReference ("System", "Object", null, null, false);
		var method = new MethodDefinition ("foo", MethodAttributes.Static, object_ref);

		var x = new ParameterDefinition ("x", ParameterAttributes.None, object_ref);
		var y = new ParameterDefinition ("y", ParameterAttributes.None, object_ref);
		var z = new ParameterDefinition ("y", ParameterAttributes.None, object_ref);

		method.Parameters.Add (x);
		method.Parameters.Add (y);
		method.Parameters.Add (z);

		(x.Index).Should().Be(0);
		(y.Index).Should().Be(1);
		(z.Index).Should().Be(2);

		method.Parameters.Remove (y);

		(x.Index).Should().Be(0);
		(y.Index).Should().Be(-1);
		(z.Index).Should().Be(1);
	}

	[Fact]
	public void InsertParameterIndex ()
	{
		var object_ref = new TypeReference ("System", "Object", null, null, false);
		var method = new MethodDefinition ("foo", MethodAttributes.Static, object_ref);

		var x = new ParameterDefinition ("x", ParameterAttributes.None, object_ref);
		var y = new ParameterDefinition ("y", ParameterAttributes.None, object_ref);
		var z = new ParameterDefinition ("y", ParameterAttributes.None, object_ref);

		method.Parameters.Add (x);
		method.Parameters.Add (z);

		(x.Index).Should().Be(0);
		(y.Index).Should().Be(-1);
		(z.Index).Should().Be(1);

		method.Parameters.Insert (1, y);

		(x.Index).Should().Be(0);
		(y.Index).Should().Be(1);
		(z.Index).Should().Be(2);
	}

	[Fact]
	public void GenericParameterConstant ()
	{
		TestIL ("hello.il", module => {
			var foo = module.GetType ("Foo");
			var method = foo.GetMethod ("GetState");

			Assert.NotNull (method);

			var parameter = method.Parameters [1];

			Assert.True (parameter.HasConstant);
			Assert.Null (parameter.Constant);
		});
	}

	[Fact]
	public void NullablePrimitiveParameterConstant ()
	{
		TestModule ("nullable-parameter.dll", module => {
			var test = module.GetType ("Test");
			var method = test.GetMethod ("Foo");

			Assert.NotNull (method);

			var param = method.Parameters [0];
			Assert.True (param.HasConstant);
			(param.Constant).Should().Be(1234);

			param = method.Parameters [1];
			Assert.True (param.HasConstant);
			(param.Constant).Should().Be(null);

			param = method.Parameters [2];
			Assert.True (param.HasConstant);
			(param.Constant).Should().Be(12);
		});
	}
}
