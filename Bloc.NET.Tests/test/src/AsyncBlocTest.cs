namespace Bloc.NET.Tests;

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

public class AsyncBlocTest {
  public static bool WasBlocClosed { get; set; }

  [Fact]
  public void Initializes() {
    using var bloc = new TestBloc();
    bloc.State.ShouldBe(TestBloc.INITIAL_STATE);
  }

  [Fact]
  public void Finalizes() {
    WasBlocClosed = false;
    // Weak reference has to be created and cleared from a static function
    // or else the GC won't ever collect it :P
    var weakBloc = CreateWeakReference();
    Utils.ClearWeakReference(weakBloc);

    // Our weak reference to our bloc will have triggered its finalizers now.
    WasBlocClosed.ShouldBeTrue();
  }

  [Fact]
  public void DoesNothingOnRepeatedDispose() {
    var bloc = new TestBloc();
    bloc.Dispose();
    Should.NotThrow(() => bloc.Dispose());
  }

  [Fact]
  public async Task EnumeratesAsync() {
    using var bloc = new TestBloc();
    var enumerator = bloc.States.ToAsyncEnumerable().GetAsyncEnumerator();

    await enumerator.MoveNextAsync();
    enumerator.Current.ShouldBe(TestBloc.INITIAL_STATE);

    bloc.Add(new ITestEvent.Increment());
    await enumerator.MoveNextAsync();
    enumerator.Current.ShouldBe(TestBloc.INITIAL_STATE + 1);
    bloc.Add(new ITestEvent.Increment());
    await enumerator.MoveNextAsync();
    enumerator.Current.ShouldBe(TestBloc.INITIAL_STATE + 2);
    bloc.Add(new ITestEvent.Increment());
    await enumerator.MoveNextAsync();
    enumerator.Current.ShouldBe(TestBloc.INITIAL_STATE + 3);
  }

  [Fact]
  public async Task UpdatesEventObservers() {
    using var bloc = new TestBloc();
    var enumerator = bloc.States.ToAsyncEnumerable().GetAsyncEnumerator();

    var @event = new ITestEvent.Increment();
    var streamWasCalled = false;
    using var subscription = bloc.Events.Subscribe(
      onNext: (@e) => {
        @e.ShouldBe(@event);
        streamWasCalled = true;
      }
    );

    var handlerWasCalled = false;
    var eventHandler = new EventHandler<ITestEvent>((b, e) => {
      b.ShouldBe(bloc);
      e.ShouldBe(@event);
      handlerWasCalled = true;
    });

    bloc.OnEvent += eventHandler;

    bloc.Add(@event);

    await enumerator.MoveNextAsync();
    streamWasCalled.ShouldBeTrue();
    handlerWasCalled.ShouldBeTrue();

    bloc.OnEvent -= eventHandler;
  }

  [Fact]
  public async Task UpdatesErrorObservers() {
    using var bloc = new TestErrorBloc();
    var enumerator = bloc.States.ToAsyncEnumerable().GetAsyncEnumerator();

    var streamWasCalled = false;
    using var subscription = bloc.Errors.Subscribe(
      onNext: (@e) => {
        @e.ShouldBe(TestErrorBloc.Error);
        streamWasCalled = true;
      }
    );

    var handlerWasCalled = false;
    var errorHandler = new EventHandler<Exception>((b, e) => {
      b.ShouldBe(bloc);
      e.ShouldBe(TestErrorBloc.Error);
      handlerWasCalled = true;
    });

    bloc.OnNextError += errorHandler;

    bloc.Add(new ITestEvent.Increment());

    await enumerator.MoveNextAsync();
    streamWasCalled.ShouldBeTrue();
    handlerWasCalled.ShouldBeTrue();

    bloc.OnNextError -= errorHandler;
  }

  [Fact]
  public async Task UpdatesActionObservers() {
    using var bloc = new TestBlocWithActions();
    var enumerator = bloc.States.ToAsyncEnumerable().GetAsyncEnumerator();

    var streamWasCalled = false;
    var expectedAction = 1;
    using var subscription = bloc.Actions.Subscribe(
      onNext: (action) => {
        action.ShouldBe(expectedAction);
        streamWasCalled = true;
      }
    );

    var handlerWasCalled = false;
    var actionHandler = new EventHandler<int>((b, action) => {
      b.ShouldBe(bloc);
      action.ShouldBe(expectedAction);
      handlerWasCalled = true;
    });

    bloc.OnAction += actionHandler;

    bloc.Add(new ITestEvent.Increment());

    await enumerator.MoveNextAsync();
    streamWasCalled.ShouldBeTrue();
    handlerWasCalled.ShouldBeTrue();

    bloc.OnAction -= actionHandler;
  }

  [Fact]
  public void OnNextStateIsInvoked() {
    using var bloc = new TestBloc();
    var onNextStateCalled = 0;
    void onNextState(object? bloc, int s) => onNextStateCalled++;
    bloc.OnNextState += onNextState;

    bloc.Add(new ITestEvent.Increment());
    onNextStateCalled.ShouldBe(1);

    bloc.OnNextState -= onNextState;
    bloc.Add(new ITestEvent.Increment());
    onNextStateCalled.ShouldBe(1);
  }

  [Fact]
  public void OnStateIsInvoked() {
    using var bloc = new TestBloc();
    var onStateCalled = 0;
    void onState(object? bloc, int s) => onStateCalled++;
    bloc.OnState += onState;

    bloc.Add(new ITestEvent.Increment());
    onStateCalled.ShouldBe(2);

    bloc.OnState -= onState;
    bloc.Add(new ITestEvent.Increment());
    onStateCalled.ShouldBe(2);
  }

  [Fact]
  public void OnNextErrorIsInvoked() {
    using var bloc = new TestErrorBloc();
    var onNextErrorCalled = 0;
    void onNextError(object? bloc, Exception e) => onNextErrorCalled++;
    bloc.OnNextError += onNextError;

    bloc.Add(new ITestEvent.Increment());
    onNextErrorCalled.ShouldBe(1);

    bloc.OnNextError -= onNextError;
    bloc.Add(new ITestEvent.Increment());
    onNextErrorCalled.ShouldBe(1);
  }

  [Fact]
  public void ListenRespondsToEvents() {
    using var bloc = new TestBloc();
    var state = TestBloc.INITIAL_STATE;
    using var subscription = bloc.Listen(s => state = s);
    bloc.Add(new ITestEvent.Increment());
    state.ShouldBe(TestBloc.INITIAL_STATE + 1);
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

    bloc.Add(new ITestEvent.Increment());

    onErrorCalled.ShouldBeTrue();
  }

  [Fact]
  public void ListenHandlesErrorWithDefaultHandler() {
    using var bloc = new TestErrorBloc();
    var state = TestErrorBloc.INITIAL_STATE;
    using var subscription = bloc.Listen(s => state = s);

    bloc.Add(new ITestEvent.Increment());

    // Events that cause errors to be thrown in OnEvent should not affect
    // state.
    bloc.State.ShouldBe(TestErrorBloc.INITIAL_STATE);
  }

  [Fact]
  public Task AddsError() {
    using var bloc = new TestError2Bloc();

    var states = new List<int>();
    using var subscription = bloc.Listen(s => states.Add(s));

    var errorEventCalled = false;
    bloc.OnNextError += (_, e) => {
      e.ShouldBeOfType<InvalidOperationException>();
      errorEventCalled = true;
    };

    bloc.Add(new ITestEvent.Increment());

    states.ShouldBe(new[] { TestError2Bloc.INITIAL_STATE, 1, 2, 3 });
    errorEventCalled.ShouldBeTrue();
    return Task.CompletedTask;
  }

  [Fact]
  public void ListenHandlesCompletion() {
    var bloc = new TestBloc();
    var onCompletedCalled = false;
    using var subscription = bloc.Listen(
      _ => { }, onCompleted: () => onCompletedCalled = true
    );
    bloc.Add(new ITestEvent.Increment());
    bloc.Dispose();
    onCompletedCalled.ShouldBeTrue();
  }

  [Fact]
  public void ListenHandlesCompletionWithDefaultHandler() {
    var bloc = new TestBloc();
    using var subscription = bloc.Listen(_ => { });
    bloc.Add(new ITestEvent.Increment());
    bloc.Dispose();
  }

  [Fact]
  public void ListenHandlesCancellation() {
    using var bloc = new TestBloc();
    var onCanceledCalled = false;
    var cancellation = new CancellationTokenSource();
    using var subscription = bloc.Listen(_ => { });
    cancellation.Token.Register(() => onCanceledCalled = true);
    bloc.Add(new ITestEvent.Increment());
    cancellation.Cancel();
    onCanceledCalled.ShouldBeTrue();
  }

  [Fact]
  public void BlocClassicCannotTriggerAction() {
    using var bloc = new TestBlocClassicTrigger();
    Should.Throw<InvalidOperationException>(() => bloc.Trigger());
  }

  [Fact]
  public void CannotRegisterMoreThanOneEventHandlerPerType() =>
    Should.Throw<InvalidOperationException>(() => {
      using var bloc = new TestBlocDuplicateRegistrations();
    });

  [Fact]
  public void ThrowsExceptionWhenNoEventHandlerRegistered() {
    using var bloc = new TestErrorBloc();
    Should.Throw<InvalidOperationException>(
      () => bloc.Add(new ITestEvent.Decrement())
    );
  }

  public static class Utils {
    public static void ClearWeakReference(WeakReference weakReference) {
      weakReference.Target = null;
      GC.Collect();
      GC.WaitForPendingFinalizers();
    }
  }

  public static WeakReference CreateWeakReference() => new(new TestBloc());

  public interface ITestEvent {
    public struct Increment : ITestEvent { }
    public struct Decrement : ITestEvent { }
  }

  public class TestErrorBloc : Bloc<ITestEvent, int> {
    public const int INITIAL_STATE = 0;

    public static Exception Error { get; } = new InvalidOperationException();

    public TestErrorBloc() : base(INITIAL_STATE) {
      On<ITestEvent.Increment>((_) => throw Error);
    }
  }

  public class TestError2Bloc : Bloc<ITestEvent, int> {
    public const int INITIAL_STATE = 0;

    public TestError2Bloc() : base(INITIAL_STATE) {
      On<ITestEvent.Increment>(Increment);
    }

    private async IAsyncEnumerable<int> Increment(ITestEvent.Increment _) {
      yield return 1;
      AddError(new InvalidOperationException());
      yield return 2;
      await Task.CompletedTask;
      yield return 3;
    }
  }

  public class TestBloc : Bloc<ITestEvent, int> {
    public const int INITIAL_STATE = 0;

    public TestBloc() : base(INITIAL_STATE) {
      On<ITestEvent.Increment>(Increment);
      On<ITestEvent.Decrement>(Decrement);
    }

    private async IAsyncEnumerable<int> Increment(ITestEvent.Increment _) {
      var state = State + 1;
      yield return state;
      await Task.CompletedTask;
    }

    private async IAsyncEnumerable<int> Decrement(ITestEvent.Decrement _) {
      var state = State - 1;
      yield return state;
      await Task.CompletedTask;
    }

    ~TestBloc() {
      WasBlocClosed = true;
    }
  }

  public class TestBlocWithActions : AsyncBloc<ITestEvent, int, int> {
    public const int INITIAL_STATE = 0;

    public TestBlocWithActions() : base(INITIAL_STATE) {
      On<ITestEvent.Increment>(Increment);
      On<ITestEvent.Decrement>(Decrement);
    }

    private async IAsyncEnumerable<int> Increment(ITestEvent.Increment inc) {
      var state = State + 1;
      Trigger(state);
      yield return state;
      await Task.CompletedTask;
    }

    private async IAsyncEnumerable<int> Decrement(ITestEvent.Decrement _) {
      var state = State - 1;
      Trigger(state);
      yield return state;
      await Task.CompletedTask;
    }
  }

  public class TestBlocClassicTrigger : Bloc<string, int> {
    public const int INITIAL_STATE = 0;

    public TestBlocClassicTrigger() : base(INITIAL_STATE) { }

    public void Trigger() => Trigger("action");
  }

  public class TestBlocDuplicateRegistrations : Bloc<string, int> {
    public const int INITIAL_STATE = 0;

    public TestBlocDuplicateRegistrations() : base(INITIAL_STATE) {
      On<string>((_) => AsyncEnumerable.Empty<int>());
      On<string>((_) => AsyncEnumerable.Empty<int>());
    }
  }
}
