namespace Bloc.NET.Tests;

using Shouldly;
using Xunit;

public partial class BlocGlueTests {
  [Fact]
  public void CallsCorrectActionsOnEventUpdates() {
    using var bloc = new FakeBloc();
    using var glue = bloc.Glue();

    var callA1 = 0;
    var callA2 = 0;

    var a1 = 3;
    var a2 = 4;

    glue.When<FakeBlocState.StateA>()
      .Use(
        data: (state) => state.Value1,
        to: (value1) => { callA1++; value1.ShouldBe(a1); })
      .Use(
        data: (state) => state.Value2,
        to: (value2) => { callA2++; value2.ShouldBe(a2); }
      );

    callA1.ShouldBe(0);
    callA2.ShouldBe(0);

    bloc.Add(new IFakeBlocEvent.EventOne(a1, a2));

    callA1.ShouldBe(1);
    callA2.ShouldBe(1);

    // Make sure the same values don't trigger the actions again

    a1 = 5;
    bloc.Add(new IFakeBlocEvent.EventOne(a1, a2));

    callA1.ShouldBe(2);
    callA2.ShouldBe(1);

    // Make sure unrelated events don't trigger the actions

    bloc.Add(new IFakeBlocEvent.EventTwo("a", "b"));

    callA1.ShouldBe(2);
    callA2.ShouldBe(1);

    // Make sure that previous unrelated states cause actions for new state
    // to be called

    bloc.Add(new IFakeBlocEvent.EventOne(a1, a2));

    callA1.ShouldBe(3);
    callA2.ShouldBe(2);
  }
}
