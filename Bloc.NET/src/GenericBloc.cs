namespace Chickensoft.GoDotNet;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using WeakEvent;

/// <summary>
/// A bloc is a component that processes events and maintains a state. In
/// game development, blocs can be used in place of traditional state machines,
/// HSM's, or state charts. While blocs are not as rigorously defined as state
/// machines, they are easy to test and offer increased flexibility (and often
/// require less code).
/// </summary>
/// <typeparam name="TEvent">Type of events that the bloc receives.</typeparam>
/// <typeparam name="TState">Type of state that bloc maintains.</typeparam>
public abstract class GenericBloc<TEvent, TState> : BlocBase<TEvent, TState>
  where TState : IEquatable<TState> {
  private readonly ISubject<TEvent> _eventController
    = new Subject<TEvent>();
  private readonly BehaviorSubject<TState> _stateController;
  private readonly ISubject<Exception> _errorController
    = new Subject<Exception>();
  private readonly CancellationTokenSource _eventSubscription;
  private readonly WeakEventSource<TState> _stateEventSource = new();
  private readonly WeakEventSource<Exception> _errorEventSource = new();
  private readonly IDisposable _stateSubscription;
  private readonly IDisposable _errorSubscription;
  private bool _isDisposed;

  /// <inheritdoc/>
  public override TState State => _stateController.Value;

  /// <inheritdoc/>
  public override IAsyncEnumerable<TState> Stream
    => _stateController.ToAsyncEnumerable();

  /// <inheritdoc/>
  public override IEnumerable<TState> States => _stateController.ToEnumerable();

  /// <inheritdoc/>
  public override event EventHandler<TState> OnNextState {
    add => _stateEventSource.Subscribe(value);
    remove => _stateEventSource.Unsubscribe(value);
  }

  /// <inheritdoc/>
  public override event EventHandler<TState> OnState {
    add {
      _stateEventSource.Subscribe(value);
      // Invoke the event with the current state when a listener is added.
      _stateEventSource.Raise(this, State);
    }
    remove => _stateEventSource.Unsubscribe(value);
  }

  /// <inheritdoc/>
  public override event EventHandler<Exception> OnNextError {
    add => _errorEventSource.Subscribe(value);
    remove => _errorEventSource.Unsubscribe(value);
  }

  /// <summary>
  /// Creates a new bloc with the given initial state.
  /// </summary>
  /// <param name="initialState">Initial state of the bloc.</param>
  public GenericBloc(TState initialState) {
    _stateController = new BehaviorSubject<TState>(initialState);
    _eventSubscription = new CancellationTokenSource();

    _eventController
      .SelectMany((@event) => {
        try {
          return ConvertEvent(@event);
        }
        catch (Exception e) {
          _errorController.OnNext(e);
          if (ShouldThrow(e)) { throw; }
        }
        return Observable.Empty<TState>();
      })
      .Where(s => s is not null).Select(s => s!)
      .DistinctUntilChanged()
      .Subscribe(
        onNext: _stateController.OnNext,
        onCompleted: _stateController.OnCompleted,
        token: _eventSubscription.Token
      );

    _stateSubscription = _stateController.Subscribe(
      onNext: (s) => _stateEventSource.Raise(this, s)
    );

    _errorSubscription = _errorController.Subscribe(onNext: AddError);

    _stateController.OnNext(initialState);
  }

  /// <summary>
  /// Converts an event into an observable stream of states.
  /// </summary>
  /// <param name="event">Event to process.</param>
  protected abstract IObservable<TState> ConvertEvent(TEvent @event);

  /// <summary>
  /// Gives bloc implementations a chance to determine if an exception should
  /// be thrown. Throwing an exception breaks further event processing.
  /// </summary>
  /// <param name="e">Exception to be thrown.</param>
  /// <returns>True if the exception should be thrown. False to simply add it
  /// as an error to the bloc error stream.</returns>
  protected abstract bool ShouldThrow(Exception e);

  /// <inheritdoc/>
  public override void Add(TEvent @event) => _eventController.OnNext(@event);

  /// <inheritdoc/>
  protected override void AddError(Exception e) {
    _errorEventSource.Raise(this, e);
    OnError(e);
  }

  /// <inheritdoc/>
  public override IDisposable Listen(
    Action<TState> onNext,
    Action<Exception>? onError = null,
    Action? onCompleted = null
  ) {
    var stateSubscription = _stateController.Subscribe(
      onNext: onNext,
      onCompleted: onCompleted ?? (static () => { })
    );
    var errorSubscription = _errorController.Subscribe(
      onNext: onError ?? (static (e) => { })
    );
    return new CompositeDisposable(stateSubscription, errorSubscription);
  }

  /// <inheritdoc/>
  protected override void OnError(Exception e) { }

  /// <inheritdoc/>
  public override void Dispose() {
    Dispose(true);
    // GC doesn't need to call our finalizer since we've already cleaned
    // everything up.
    GC.SuppressFinalize(this);
  }

  private void Dispose(bool disposing) {
    if (_isDisposed) { return; }

    _eventController.OnCompleted();
    _stateController.OnCompleted();
    _errorController.OnCompleted();

    // Unsubscribe from events.
    _eventSubscription.Cancel();
    _eventSubscription.Dispose();

    // Unsubscribe from state changes.
    _stateSubscription.Dispose();
    _errorSubscription.Dispose();

    _isDisposed = true;
  }

  /// <summary>
  /// Finalizer for the bloc — cleans up resources before garbage collection.
  /// </summary>
  ~GenericBloc() => Dispose(false);
}
