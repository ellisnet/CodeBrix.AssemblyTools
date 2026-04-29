using System;
using System.Linq;

using CodeBrix.AssemblyTools;
using CodeBrix.AssemblyTools.Metadata;
using CodeBrix.AssemblyTools.Collections.Generic;
using Xunit;
using SilverAssertions;
namespace CodeBrix.AssemblyTools.Tests.Core; //was previously: Mono.Cecil.Tests;
public class MethodTests : BaseTestFixture {

	[Fact]
	public void AbstractMethod ()
	{
		TestCSharp ("Methods.cs", module => {
			var type = module.Types [1];
			(type.Name).Should().Be("Foo");
			(type.Methods.Count).Should().Be(2);

			var method = type.GetMethod ("Bar");
			(method.Name).Should().Be("Bar");
			Assert.True (method.IsAbstract);
			Assert.NotNull (method.ReturnType);

			(method.Parameters.Count).Should().Be(1);

			var parameter = method.Parameters [0];

			(parameter.Name).Should().Be("a");
			(parameter.ParameterType.FullName).Should().Be("System.Int32");
		});
	}

	[Fact]
	public void SimplePInvoke ()
	{
		TestCSharp ("Methods.cs", module => {
			var bar = module.GetType ("Bar");
			var pan = bar.GetMethod ("Pan");

			Assert.True (pan.IsPInvokeImpl);
			Assert.NotNull (pan.PInvokeInfo);

			(pan.PInvokeInfo.EntryPoint).Should().Be("Pan");
			Assert.NotNull (pan.PInvokeInfo.Module);
			(pan.PInvokeInfo.Module.Name).Should().Be("foo.dll");
		});
	}

	[Fact]
	public void GenericMethodDefinition ()
	{
		TestCSharp ("Generics.cs", module => {
			var baz = module.GetType ("Baz");

			var gazonk = baz.GetMethod ("Gazonk");

			Assert.NotNull (gazonk);

			Assert.True (gazonk.HasGenericParameters);
			(gazonk.GenericParameters.Count).Should().Be(1);
			(gazonk.GenericParameters [0].Name).Should().Be("TBang");
		});
	}

	[Fact]
	public void ReturnGenericInstance ()
	{
		TestCSharp ("Generics.cs", module => {
			var bar = module.GetType ("Bar`1");

			var self = bar.GetMethod ("Self");
			Assert.NotNull (self);

			var bar_t = self.ReturnType;

			Assert.True (bar_t.IsGenericInstance);

			var bar_t_instance = (GenericInstanceType) bar_t;

			(bar_t_instance.GenericArguments [0]).Should().Be(bar.GenericParameters [0]);

			var self_str = bar.GetMethod ("SelfString");
			Assert.NotNull (self_str);

			var bar_str = self_str.ReturnType;
			Assert.True (bar_str.IsGenericInstance);

			var bar_str_instance = (GenericInstanceType) bar_str;

			(bar_str_instance.GenericArguments [0].FullName).Should().Be("System.String");
		});
	}

	[Fact]
	public void ReturnGenericInstanceWithMethodParameter ()
	{
		TestCSharp ("Generics.cs", module => {
			var baz = module.GetType ("Baz");

			var gazoo = baz.GetMethod ("Gazoo");
			Assert.NotNull (gazoo);

			var bar_bingo = gazoo.ReturnType;

			Assert.True (bar_bingo.IsGenericInstance);

			var bar_bingo_instance = (GenericInstanceType) bar_bingo;

			(bar_bingo_instance.GenericArguments [0]).Should().Be(gazoo.GenericParameters [0]);
		});
	}

	[Fact]
	public void SimpleOverrides ()
	{
		TestCSharp ("Interfaces.cs", module => {
			var ibingo = module.GetType ("IBingo");
			var ibingo_foo = ibingo.GetMethod ("Foo");
			Assert.NotNull (ibingo_foo);

			var ibingo_bar = ibingo.GetMethod ("Bar");
			Assert.NotNull (ibingo_bar);

			var bingo = module.GetType ("Bingo");

			var foo = bingo.GetMethod ("IBingo.Foo");
			Assert.NotNull (foo);

			Assert.True (foo.HasOverrides);
			(foo.Overrides [0]).Should().Be(ibingo_foo);

			var bar = bingo.GetMethod ("IBingo.Bar");
			Assert.NotNull (bar);

			Assert.True (bar.HasOverrides);
			(bar.Overrides [0]).Should().Be(ibingo_bar);
		});
	}

	[Fact]
	public void VarArgs ()
	{
		TestModule ("varargs.exe", module => {
			var module_type = module.Types [0];

			(module_type.Methods.Count).Should().Be(3);

			var bar = module_type.GetMethod ("Bar");
			var baz = module_type.GetMethod ("Baz");
			var foo = module_type.GetMethod ("Foo");

			Assert.True (bar.IsVarArg ());
			Assert.False (baz.IsVarArg ());

			Assert.True (foo.IsVarArg ());

			var foo_reference = (MethodReference) baz.Body.Instructions.First (i => i.Offset == 0x000a).Operand;

			Assert.True (foo_reference.IsVarArg ());
			(foo_reference.GetSentinelPosition ()).Should().Be(0);

			(foo_reference.Resolve ()).Should().Be(foo);

			var bar_reference = (MethodReference) baz.Body.Instructions.First (i => i.Offset == 0x0023).Operand;

			Assert.True (bar_reference.IsVarArg ());

			(bar_reference.GetSentinelPosition ()).Should().Be(1);

			(bar_reference.Resolve ()).Should().Be(bar);
		});
	}

	[Fact]
	public void GenericInstanceMethod ()
	{
		TestCSharp ("Generics.cs", module => {
			var type = module.GetType ("It");
			var method = type.GetMethod ("ReadPwow");

			GenericInstanceMethod instance = null;

			foreach (var instruction in method.Body.Instructions) {
				instance = instruction.Operand as GenericInstanceMethod;
				if (instance != null)
					break;
			}

			Assert.NotNull (instance);

			(instance.MetadataToken.TokenType).Should().Be(TokenType.MethodSpec);
			(instance.MetadataToken.RID).Should().NotBe(0);
		});
	}

	[Fact]
	public void MethodRefDeclaredOnGenerics ()
	{
		TestCSharp ("Generics.cs", module => {
			var type = module.GetType ("Tamtam");
			var beta = type.GetMethod ("Beta");
			var charlie = type.GetMethod ("Charlie");

			// Note that the test depends on the C# compiler emitting the constructor call instruction as
			// the first instruction of the method body. This requires optimizations to be enabled.
			var new_list_beta = (MethodReference) beta.Body.Instructions [0].Operand;
			var new_list_charlie = (MethodReference) charlie.Body.Instructions [0].Operand;

			(new_list_beta.DeclaringType.FullName).Should().Be("System.Collections.Generic.List`1<TBeta>");
			(new_list_charlie.DeclaringType.FullName).Should().Be("System.Collections.Generic.List`1<TCharlie>");
		});
	}

	[Fact]
	public void ReturnParameterMethod ()
	{
		var method = typeof (MethodTests).ToDefinition ().GetMethod ("ReturnParameterMethod");
		Assert.NotNull (method);
		(method.MethodReturnType.Parameter.Method).Should().Be(method);
	}

	[Fact]
	public void InstanceAndStaticMethodComparison ()
	{
		TestIL ("others.il", module => {
			var others = module.GetType ("Others");
			var instance_method = others.Methods.Single (m => m.Name == "SameMethodNameInstanceStatic" && m.HasThis);
			var static_method_reference = new MethodReference ("SameMethodNameInstanceStatic", instance_method.ReturnType, others)
				{
					HasThis = false
				};

			(static_method_reference.Resolve ()).Should().NotBe(instance_method);
		});
	}

	[Fact]
	public void FunctionPointerArgumentOverload ()
	{
		TestIL ("others.il", module => {
			var others = module.GetType ("Others");
			var overloaded_methods = others.Methods.Where (m => m.Name == "OverloadedWithFpArg").ToArray ();
			// Manually create the function-pointer type so `AreSame` won't exit early due to reference equality
			var overloaded_method_int_reference = new MethodReference ("OverloadedWithFpArg", module.TypeSystem.Void, others) 
			{
				HasThis = false,
				Parameters = { new ParameterDefinition ("X", ParameterAttributes.None, new FunctionPointerType () {
					HasThis = false,
					ReturnType = module.TypeSystem.Int32,
					Parameters = { new ParameterDefinition (module.TypeSystem.Int32) }
				}) }
			};
			
			var overloaded_method_long_reference = new MethodReference ("OverloadedWithFpArg", module.TypeSystem.Void, others) 
			{
				HasThis = false,
				Parameters = { new ParameterDefinition ("X", ParameterAttributes.None, new FunctionPointerType () {
					HasThis = false,
					ReturnType = module.TypeSystem.Int32,
					Parameters = { new ParameterDefinition (module.TypeSystem.Int64) }
				}) }
			};
			
			var overloaded_method_cdecl_reference = new MethodReference ("OverloadedWithFpArg", module.TypeSystem.Void, others) 
			{
				HasThis = false,
				Parameters = { new ParameterDefinition ("X", ParameterAttributes.None, new FunctionPointerType () {
					CallingConvention = MethodCallingConvention.C,
					HasThis = false,
					ReturnType = module.TypeSystem.Int32,
					Parameters = { new ParameterDefinition (module.TypeSystem.Int32) }
				}) } 
			};
			

			(overloaded_method_int_reference.Resolve ()).Should().Be(overloaded_methods[0]); 
			(overloaded_method_long_reference.Resolve ()).Should().Be(overloaded_methods[1]); 
			(overloaded_method_cdecl_reference.Resolve ()).Should().Be(overloaded_methods[2]); 
		});
	}
}
