namespace Bloc.NET;

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
/// <typeparam name="TAction">Type of actions the bloc can trigger.</typeparam>
public abstract class GenericBloc<TEvent, TState, TAction> :
  BlocBase<TEvent, TState, TAction>
  where TEvent : notnull
  where TState : IEquatable<TState>
  where TAction : notnull {
  private readonly ISubject<TEvent> _eventController = new Subject<TEvent>();
  private readonly BehaviorSubject<TState> _stateController;
  private readonly ISubject<Exception> _errorController
    = new Subject<Exception>();
  private readonly CancellationTokenSource _eventSubscription;
  private readonly WeakEventSource<TEvent> _eventEventSource = new();
  private readonly WeakEventSource<TState> _stateEventSource = new();
  private readonly WeakEventSource<Exception> _errorEventSource = new();
  private readonly IDisposable _stateSubscription;
  private readonly IDisposable _errorSubscription;
  private readonly ISubject<TAction> _actionController = new Subject<TAction>();
  private readonly WeakEventSource<TAction> _actionEventSource = new();
  private readonly IDisposable _actionSubscription;
  private readonly Dictionary<Type, Func<dynamic, IObservable<TState>>>
    _handlers = new();

  /// <inheritdoc/>
  public override TState State => _stateController.Value;

  /// <inheritdoc/>
  public override IObservable<TEvent> Events => _eventController.AsObservable();

  /// <inheritdoc/>
  public override IObservable<TState> States => _stateController.AsObservable();

  /// <inheritdoc/>
  public override IObservable<TAction> Actions =>
    _actionController.AsObservable();

  /// <inheritdoc/>
  public override IObservable<Exception> Errors =>
    _errorController.AsObservable();

  /// <summary>
  /// Whether or not the bloc has been disposed.
  /// </summary>
  public bool IsDisposed { get; private set; }

  /// <inheritdoc/>
  public override event EventHandler<TEvent> OnEvent {
    add => _eventEventSource.Subscribe(value);
    remove => _eventEventSource.Unsubscribe(value);
  }

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

  /// <inheritdoc/>
  public override event EventHandler<TAction> OnAction {
    add => _actionEventSource.Subscribe(value);
    remove => _actionEventSource.Unsubscribe(value);
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
        _eventEventSource.Raise(this, @event);

        try {
          return _handlers[@event.GetType()](@event);
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

    _actionSubscription = _actionController.Subscribe(
      onNext: (a) => _actionEventSource.Raise(this, a)
    );

    _stateController.OnNext(initialState);
  }

  /// <summary>
  /// Gives bloc implementations a chance to determine if an exception should
  /// be thrown. Throwing an exception breaks further event processing.
  /// </summary>
  /// <param name="e">Exception to be thrown.</param>
  /// <returns>True if the exception should be thrown. False to simply add it
  /// as an error to the bloc error stream.</returns>
  protected abstract bool ShouldThrow(Exception e);

  /// <summary>
  /// Registers an event handler for a specific event type. The event handler
  /// is a function that receives an event of a specific type and emits one or
  /// more states in response to the event.
  /// </summary>
  /// <param name="handler">Event handler.</param>
  /// <typeparam name="TEventType">Type of the event.</typeparam>
  /// <exception cref="InvalidOperationException"></exception>
  protected void On<TEventType>(
    Func<TEventType, IObservable<TState>> handler
  ) where TEventType : TEvent {
    var type = typeof(TEventType);
    if (_handlers.ContainsKey(type)) {
      throw new InvalidOperationException(
        "Another handler was already registered for the event type " +
        type.FullName + "."
      );
    }
    _handlers.Add(typeof(TEventType), (e) => handler((TEventType)e));
  }

  /// <inheritdoc/>
  public override void Add(TEvent @event) {
    var type = @event.GetType();
    if (!_handlers.ContainsKey(type)) {
      throw new InvalidOperationException(
        $"No handler registered for the event type {type.FullName}."
      );
    }
    _eventController.OnNext(@event);
  }

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
  protected override void Trigger(TAction action) =>
    _actionController.OnNext(action);

  /// <inheritdoc/>
  public override void Dispose() {
    Dispose(true);
    // GC doesn't need to call our finalizer since we've already cleaned
    // everything up.
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Cleans up the bloc's subscriptions and event streams.
  /// </summary>
  /// <param name="disposing">True if this method was invoked from the public
  /// <see cref="Dispose()" /> method, or false if it was invoked from the
  /// finalizer.</param>
  protected virtual void Dispose(bool disposing) {
    if (IsDisposed) { return; }

    if (disposing) {
      _eventController.OnCompleted();
      _stateController.OnCompleted();
      _errorController.OnCompleted();

      // Unsubscribe from events.
      _eventSubscription.Cancel();
      _eventSubscription.Dispose();

      // Unsubscribe from state changes.
      _stateSubscription.Dispose();
      _errorSubscription.Dispose();

      // Unsubscribe from action announcements.
      _actionSubscription.Dispose();
    }

    IsDisposed = true;
  }

  /// <summary>
  /// Finalizer for the bloc — cleans up resources before garbage collection.
  /// </summary>
  ~GenericBloc() => Dispose(false);
}
