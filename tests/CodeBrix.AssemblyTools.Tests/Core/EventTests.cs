using System;

using CodeBrix.AssemblyTools;
using CodeBrix.AssemblyTools.Metadata;
using Xunit;
using SilverAssertions;
namespace CodeBrix.AssemblyTools.Tests.Core; //was previously: Mono.Cecil.Tests;
public class EventTests : BaseTestFixture {

	[Fact]
	public void AbstractMethod ()
	{
		TestCSharp ("Events.cs", module => {
			var type = module.GetType ("Foo");

			Assert.True (type.HasEvents);

			var events = type.Events;

			(events.Count).Should().Be(1);

			var @event = events [0];

			Assert.NotNull (@event);
			(@event.Name).Should().Be("Bar");
			Assert.NotNull (@event.EventType);
			(@event.EventType.FullName).Should().Be("Pan");

			Assert.NotNull (@event.AddMethod);
			(@event.AddMethod.SemanticsAttributes).Should().Be(MethodSemanticsAttributes.AddOn);
			Assert.NotNull (@event.RemoveMethod);
			(@event.RemoveMethod.SemanticsAttributes).Should().Be(MethodSemanticsAttributes.RemoveOn);
		});
	}

	[Fact]
	public void OtherMethod ()
	{
		TestIL ("others.il", module => {
			var type = module.GetType ("Others");

			Assert.True (type.HasEvents);

			var events = type.Events;

			(events.Count).Should().Be(1);

			var @event = events [0];

			Assert.NotNull (@event);
			(@event.Name).Should().Be("Handler");
			Assert.NotNull (@event.EventType);
			(@event.EventType.FullName).Should().Be("System.EventHandler");

			Assert.True (@event.HasOtherMethods);

			(@event.OtherMethods.Count).Should().Be(2);

			var other = @event.OtherMethods [0];
			(other.Name).Should().Be("dang_Handler");

			other = @event.OtherMethods [1];
			(other.Name).Should().Be("fang_Handler");
		});
	}

	[Fact]
	public void UnattachedEvent ()
	{
		var int32 = typeof (int).ToDefinition ();
		var evt = new EventDefinition ("Event", EventAttributes.None, int32);

		(evt.AddMethod).Should().Be(null);
	}
}
