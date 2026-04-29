using System;
using System.Linq;

using CodeBrix.AssemblyTools;
using CodeBrix.AssemblyTools.Cil;
using Xunit;
using SilverAssertions;
namespace CodeBrix.AssemblyTools.Tests.Core; //was previously: Mono.Cecil.Tests;
public class VariableTests : BaseTestFixture {

	[Fact]
	public void AddVariableIndex ()
	{
		var object_ref = new TypeReference ("System", "Object", null, null, false);
		var method = new MethodDefinition ("foo", MethodAttributes.Static, object_ref);
		var body = new MethodBody (method);

		var x = new VariableDefinition (object_ref);
		var y = new VariableDefinition (object_ref);

		body.Variables.Add (x);
		body.Variables.Add (y);

		(x.Index).Should().Be(0);
		(y.Index).Should().Be(1);
	}

	[Fact]
	public void RemoveAtVariableIndex ()
	{
		var object_ref = new TypeReference ("System", "Object", null, null, false);
		var method = new MethodDefinition ("foo", MethodAttributes.Static, object_ref);
		var body = new MethodBody (method);

		var x = new VariableDefinition (object_ref);
		var y = new VariableDefinition (object_ref);
		var z = new VariableDefinition (object_ref);

		body.Variables.Add (x);
		body.Variables.Add (y);
		body.Variables.Add (z);

		(x.Index).Should().Be(0);
		(y.Index).Should().Be(1);
		(z.Index).Should().Be(2);

		body.Variables.RemoveAt (1);

		(x.Index).Should().Be(0);
		(y.Index).Should().Be(-1);
		(z.Index).Should().Be(1);
	}

	[Fact]
	public void RemoveVariableIndex ()
	{
		var object_ref = new TypeReference ("System", "Object", null, null, false);
		var method = new MethodDefinition ("foo", MethodAttributes.Static, object_ref);
		var body = new MethodBody (method);

		var x = new VariableDefinition (object_ref);
		var y = new VariableDefinition (object_ref);
		var z = new VariableDefinition (object_ref);

		body.Variables.Add (x);
		body.Variables.Add (y);
		body.Variables.Add (z);

		(x.Index).Should().Be(0);
		(y.Index).Should().Be(1);
		(z.Index).Should().Be(2);

		body.Variables.Remove (y);

		(x.Index).Should().Be(0);
		(y.Index).Should().Be(-1);
		(z.Index).Should().Be(1);
	}

	[Fact]
	public void RemoveVariableWithDebugInfo ()
	{
		var object_ref = new TypeReference ("System", "Object", null, null, false);
		var method = new MethodDefinition ("foo", MethodAttributes.Static, object_ref);
		var body = new MethodBody (method);
		var il = body.GetILProcessor ();
		il.Emit (OpCodes.Ret);

		var x = new VariableDefinition (object_ref);
		var y = new VariableDefinition (object_ref);
		var z = new VariableDefinition (object_ref);
		var z2 = new VariableDefinition (object_ref);

		body.Variables.Add (x);
		body.Variables.Add (y);
		body.Variables.Add (z);
		body.Variables.Add (z2);

		var scope = new ScopeDebugInformation (body.Instructions [0], body.Instructions [0]);
		method.DebugInformation = new MethodDebugInformation (method) {
			Scope = scope
		};
		scope.Variables.Add (new VariableDebugInformation (x.index, nameof (x)));
		scope.Variables.Add (new VariableDebugInformation (y.index, nameof (y)));
		scope.Variables.Add (new VariableDebugInformation (z.index, nameof (z)));
		scope.Variables.Add (new VariableDebugInformation (z2, nameof (z2)));

		body.Variables.Remove (y);

		(scope.Variables.Count).Should().Be(3);
		(scope.Variables [0].Index).Should().Be(x.Index);
		(scope.Variables [0].Name).Should().Be(nameof (x));
		(scope.Variables [1].Index).Should().Be(z.Index);
		(scope.Variables [1].Name).Should().Be(nameof (z));
		(scope.Variables [2].Index).Should().Be(z2.Index);
		(scope.Variables [2].Name).Should().Be(nameof (z2));
	}

	[Fact]
	public void InsertVariableIndex ()
	{
		var object_ref = new TypeReference ("System", "Object", null, null, false);
		var method = new MethodDefinition ("foo", MethodAttributes.Static, object_ref);
		var body = new MethodBody (method);

		var x = new VariableDefinition (object_ref);
		var y = new VariableDefinition (object_ref);
		var z = new VariableDefinition (object_ref);

		body.Variables.Add (x);
		body.Variables.Add (z);

		(x.Index).Should().Be(0);
		(y.Index).Should().Be(-1);
		(z.Index).Should().Be(1);

		body.Variables.Insert (1, y);

		(x.Index).Should().Be(0);
		(y.Index).Should().Be(1);
		(z.Index).Should().Be(2);
	}

	[Fact]
	public void InsertVariableWithDebugInfo ()
	{
		var object_ref = new TypeReference ("System", "Object", null, null, false);
		var method = new MethodDefinition ("foo", MethodAttributes.Static, object_ref);
		var body = new MethodBody (method);
		var il = body.GetILProcessor ();
		il.Emit (OpCodes.Ret);

		var x = new VariableDefinition (object_ref);
		var y = new VariableDefinition (object_ref);
		var z = new VariableDefinition (object_ref);
		var z2 = new VariableDefinition (object_ref);

		body.Variables.Add (x);
		body.Variables.Add (z);
		body.Variables.Add (z2);

		var scope = new ScopeDebugInformation (body.Instructions [0], body.Instructions [0]);
		method.DebugInformation = new MethodDebugInformation (method) {
			Scope = scope
		};
		scope.Variables.Add (new VariableDebugInformation (x.index, nameof (x)));
		scope.Variables.Add (new VariableDebugInformation (z.index, nameof (z)));
		scope.Variables.Add (new VariableDebugInformation (z2, nameof (z2)));

		body.Variables.Insert (1, y);

		// Adding local variable doesn't add debug info for it (since there's no way to deduce the name of the variable)
		(scope.Variables.Count).Should().Be(3);
		(scope.Variables [0].Index).Should().Be(x.Index);
		(scope.Variables [0].Name).Should().Be(nameof (x));
		(scope.Variables [1].Index).Should().Be(z.Index);
		(scope.Variables [1].Name).Should().Be(nameof (z));
		(scope.Variables [2].Index).Should().Be(z2.Index);
		(scope.Variables [2].Name).Should().Be(nameof (z2));
	}
}
