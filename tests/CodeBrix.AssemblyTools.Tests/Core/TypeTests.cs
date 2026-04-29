using System;
using System.Linq;

using CodeBrix.AssemblyTools;
using CodeBrix.AssemblyTools.Cil;
using CodeBrix.AssemblyTools.Metadata;
using Xunit;
using SilverAssertions;
namespace CodeBrix.AssemblyTools.Tests.Core; //was previously: Mono.Cecil.Tests;
public class TypeTests : BaseTestFixture {

	[Fact]
	public void TypeLayout ()
	{
		TestCSharp ("Layouts.cs", module => {
			var foo = module.GetType ("Foo");
			Assert.NotNull (foo);
			Assert.True (foo.IsValueType);

			Assert.True (foo.HasLayoutInfo);
			(foo.ClassSize).Should().Be(16);

			var babar = module.GetType ("Babar");
			Assert.NotNull (babar);
			Assert.False (babar.IsValueType);
			Assert.False (babar.HasLayoutInfo);
		});
	}

	[Fact]
	public void SimpleInterfaces ()
	{
		TestIL ("types.il", module => {
			var ibaz = module.GetType ("IBaz");
			Assert.NotNull (ibaz);

			Assert.True (ibaz.HasInterfaces);

			var interfaces = ibaz.Interfaces;

			(interfaces.Count).Should().Be(2);

			// Mono's ilasm and .NET's are ordering interfaces differently
			Assert.NotNull (interfaces.Single (i => i.InterfaceType.FullName == "IBar"));
			Assert.NotNull (interfaces.Single (i => i.InterfaceType.FullName == "IFoo"));
		});
	}

	[Fact]
	public void GenericTypeDefinition ()
	{
		TestCSharp ("Generics.cs", module => {
			var foo = module.GetType ("Foo`2");
			Assert.NotNull (foo);

			Assert.True (foo.HasGenericParameters);
			(foo.GenericParameters.Count).Should().Be(2);

			var tbar = foo.GenericParameters [0];

			(tbar.Name).Should().Be("TBar");
			(tbar.Owner).Should().Be(foo);

			var tbaz = foo.GenericParameters [1];

			(tbaz.Name).Should().Be("TBaz");
			(tbaz.Owner).Should().Be(foo);
		});
	}

	[Fact]
	public void ConstrainedGenericType ()
	{
		TestCSharp ("Generics.cs", module => {
			var bongo_t = module.GetType ("Bongo`1");
			Assert.NotNull (bongo_t);

			var t = bongo_t.GenericParameters [0];
			Assert.NotNull (t);
			(t.Name).Should().Be("T");

			Assert.True (t.HasConstraints);
			(t.Constraints.Count).Should().Be(2);

			(t.Constraints [0].ConstraintType.FullName).Should().Be("Zap");
			(t.Constraints [1].ConstraintType.FullName).Should().Be("IZoom");
		});
	}

	[Fact]
	public void GenericBaseType ()
	{
		TestCSharp ("Generics.cs", module => {
			var child = module.GetType ("Child`1");

			var child_t = child.GenericParameters [0];
			Assert.NotNull (child_t);

			var instance = child.BaseType as GenericInstanceType;
			Assert.NotNull (instance);
			(instance.MetadataToken.RID).Should().NotBe(0);

			(instance.GenericArguments [0]).Should().Be(child_t);
		});
	}

	[Fact]
	public void GenericConstraintOnGenericParameter ()
	{
		TestCSharp ("Generics.cs", module => {
			var duel = module.GetType ("Duel`3");

			(duel.GenericParameters.Count).Should().Be(3);

			var t1 = duel.GenericParameters [0];
			var t2 = duel.GenericParameters [1];
			var t3 = duel.GenericParameters [2];

			(t2.Constraints [0].ConstraintType).Should().Be(t1);
			(t3.Constraints [0].ConstraintType).Should().Be(t2);
		});
	}

	[Fact]
	public void GenericForwardBaseType ()
	{
		TestCSharp ("Generics.cs", module => {
			var tamchild = module.GetType ("TamChild");

			Assert.NotNull (tamchild);
			Assert.NotNull (tamchild.BaseType);

			var generic_instance = tamchild.BaseType as GenericInstanceType;

			Assert.NotNull (generic_instance);

			(generic_instance.GenericArguments.Count).Should().Be(1);
			(generic_instance.GenericArguments [0]).Should().Be(module.GetType ("Tamtam"));
		});
	}

	[Fact]
	public void TypeExtentingGenericOfSelf ()
	{
		TestCSharp ("Generics.cs", module => {
			var rec_child = module.GetType ("RecChild");

			Assert.NotNull (rec_child);
			Assert.NotNull (rec_child.BaseType);

			var generic_instance = rec_child.BaseType as GenericInstanceType;

			Assert.NotNull (generic_instance);

			(generic_instance.GenericArguments.Count).Should().Be(1);
			(generic_instance.GenericArguments [0]).Should().Be(rec_child);
		});
	}

	[Fact]
	public void TypeReferenceValueType ()
	{
		TestCSharp ("Methods.cs", module => {
			var baz = module.GetType ("Baz");
			var method = baz.GetMethod ("PrintAnswer");

			var box = method.Body.Instructions.Where (i => i.OpCode == OpCodes.Box).First ();
			var int32 = (TypeReference) box.Operand;

			Assert.True (int32.IsValueType);
		});
	}

	[Fact]
	public void GenericInterfaceReference ()
	{
		TestModule ("gifaceref.exe", module => {
			var type = module.GetType ("Program");
			var iface = type.Interfaces [0];

			var instance = (GenericInstanceType) iface.InterfaceType;
			var owner = instance.ElementType;

			(instance.GenericArguments.Count).Should().Be(1);
			(owner.GenericParameters.Count).Should().Be(1);
		});
	}

	[Fact]
	public void UnboundGenericParameter ()
	{
		TestModule ("cscgpbug.dll", module => {
			var type = module.GetType ("ListViewModel");
			var method = type.GetMethod ("<>n__FabricatedMethod1");

			var parameter = method.ReturnType as GenericParameter;

			Assert.NotNull (parameter);
			(parameter.Position).Should().Be(0);
			Assert.Null (parameter.Owner);
		}, verify: false);
	}

	[Fact]
	public void GenericMultidimensionalArray ()
	{
		TestCSharp ("Generics.cs", module => {
			var type = module.GetType ("LaMatrix");
			var method = type.GetMethod ("At");

			var call = method.Body.Instructions.Where (i => i.Operand is MethodReference).First ();
			var get = (MethodReference) call.Operand;

			Assert.NotNull (get);
			(get.GenericParameters.Count).Should().Be(0);
			(get.CallingConvention).Should().Be(MethodCallingConvention.Default);
			(get.ReturnType).Should().Be(method.GenericParameters [0]);
		});
	}

	[Fact]
	public void CorlibPrimitive ()
	{
		var module = typeof (TypeTests).ToDefinition ().Module;

		var int32 = module.TypeSystem.Int32;
		Assert.True (int32.IsPrimitive);
		(int32.MetadataType).Should().Be(MetadataType.Int32);

		var int32_def = int32.Resolve ();
		Assert.True (int32_def.IsPrimitive);
		(int32_def.MetadataType).Should().Be(MetadataType.Int32);
	}

	[Fact]
	public void ExplicitThis ()
	{
		TestIL ("explicitthis.il", module => {
			var type = module.GetType ("MakeDecision");
			var method = type.GetMethod ("Decide");
			var fptr = method.ReturnType as FunctionPointerType;

			Assert.NotNull (fptr);
			Assert.True (fptr.HasThis);
			Assert.True (fptr.ExplicitThis);

			(fptr.Parameters [0].Sequence).Should().Be(0);
			(fptr.Parameters [1].Sequence).Should().Be(1);
		}, verify: false);
	}

	[Fact]
	public void DeferredCorlibTypeDef ()
	{
		using (var module = ModuleDefinition.ReadModule (typeof (object).Assembly.Location, new ReaderParameters (ReadingMode.Deferred))) {
			var object_type = module.TypeSystem.Object;
			Assert.IsAssignableFrom<TypeDefinition> (object_type);
		}
	}

	[Fact]
	public void CorlibTypesMetadataType ()
	{
		using (var module = ModuleDefinition.ReadModule (typeof (object).Assembly.Location)) {
			var type = module.GetType ("System.String");
			Assert.NotNull (type);
			Assert.NotNull (type.BaseType);
			(type.BaseType.FullName).Should().Be("System.Object");
			Assert.IsAssignableFrom<TypeDefinition> (type.BaseType);
			(type.MetadataType).Should().Be(MetadataType.String);
			(type.BaseType.MetadataType).Should().Be(MetadataType.Object);
		}
	}

	[Fact]
	public void SelfReferencingTypeRef ()
	{
		TestModule ("self-ref-typeref.dll", module => {
		}, verify: false);
	}
}
