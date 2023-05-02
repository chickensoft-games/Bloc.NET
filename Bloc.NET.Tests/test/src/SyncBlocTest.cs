namespace Bloc.NET.Tests;

using System;
using System.Collections.Generic;
using Shouldly;
using Xunit;

public class SyncBlocTest {
  public static bool WasBlocClosed { get; set; }

  [Fact]
  public void Initializes() {
    using var bloc = new TestBloc();
    bloc.State.ShouldBe(TestBloc.INITIAL_STATE);
  }

  [Fact]
  public void ListenHandlesError() {
    using var bloc = new TestErrorBloc();
    var onErrorCalled = false;
    var state = TestErrorBloc.INITIAL_STATE;
    using var subscription = bloc.Listen(
      s => state = s,
      onError: (_) => onErrorCalled = true
    );

    Should.Throw<Exception>(() => bloc.Add("+"));

    onErrorCalled.ShouldBeTrue();
  }

  [Fact]
  public void SyncBlocClassicCannotTriggerAction() {
    using var bloc = new TestBlocClassicTrigger();
    Should.Throw<InvalidOperationException>(() => bloc.Trigger());
  }

  public class TestBlocClassicTrigger : SyncBlocClassic<string, int> {
    public const int INITIAL_STATE = 0;

    public TestBlocClassicTrigger() : base(INITIAL_STATE) { }

    public override IEnumerable<int> MapEventToState(string @event) =>
      throw new NotImplementedException();

    public void Trigger() => Trigger("action");
  }

  public class TestErrorBloc : SyncBlocClassic<string, int> {
    public const int INITIAL_STATE = 0;

    public TestErrorBloc() : base(INITIAL_STATE) { }

    public override IEnumerable<int> MapEventToState(string @event)
      => throw new InvalidOperationException();
  }

  public class TestBloc : SyncBlocClassic<string, int> {
    public const int INITIAL_STATE = 0;

    public TestBloc() : base(INITIAL_STATE) { }

    public override IEnumerable<int> MapEventToState(string @event) {
      switch (@event) {
        case "+":
          yield return State + 1;
          break;
        case "-":
          yield return State - 1;
          break;
        default:
          yield return State;
          break;
      }
    }
  }
}
