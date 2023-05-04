namespace Bloc.NET.Tests;

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
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
  public void Enumerates() {
    using var bloc = new TestBloc();
    var enumerator = bloc.States.ToEnumerable().GetEnumerator();

    enumerator.MoveNext();
    enumerator.Current.ShouldBe(TestBloc.INITIAL_STATE);

    bloc.Add(new ITestEvent.Increment());
    enumerator.MoveNext();
    enumerator.Current.ShouldBe(TestBloc.INITIAL_STATE + 1);
    bloc.Add(new ITestEvent.Increment());
    enumerator.MoveNext();
    enumerator.Current.ShouldBe(TestBloc.INITIAL_STATE + 2);

    bloc.Add(new ITestEvent.Increment());
    enumerator.MoveNext();
    enumerator.Current.ShouldBe(TestBloc.INITIAL_STATE + 3);
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

    Should.Throw<Exception>(() => bloc.Add(new ITestEvent.Increment()));

    onErrorCalled.ShouldBeTrue();
  }

  [Fact]
  public void SyncBlocClassicCannotTriggerEffect() {
    using var bloc = new TestBlocClassicTrigger();
    Should.Throw<InvalidOperationException>(() => bloc.Trigger());
  }

  public interface ITestEvent {
    public struct Increment : ITestEvent { }
    public struct Decrement : ITestEvent { }
    public struct Other : ITestEvent { }
  }

  public class TestBlocClassicTrigger : SyncBlocClassic<ITestEvent, int> {
    public const int INITIAL_STATE = 0;

    public TestBlocClassicTrigger() : base(INITIAL_STATE) { }

    public void Trigger() => Trigger("effect");
  }

  public class TestErrorBloc : SyncBlocClassic<ITestEvent, int> {
    public const int INITIAL_STATE = 0;

    public TestErrorBloc() : base(INITIAL_STATE) {
      On<ITestEvent.Increment>((@e) => throw new InvalidOperationException());
    }
  }

  public class TestBloc : SyncBlocClassic<ITestEvent, int> {
    public const int INITIAL_STATE = 0;

    public TestBloc() : base(INITIAL_STATE) {
      On<ITestEvent.Increment>(Increment);
      On<ITestEvent.Decrement>(Decrement);
    }

    private IEnumerable<int> Increment(ITestEvent.Increment _) {
      yield return State + 1;
    }

    private IEnumerable<int> Decrement(ITestEvent.Decrement _) {
      yield return State - 1;
    }
  }
}
