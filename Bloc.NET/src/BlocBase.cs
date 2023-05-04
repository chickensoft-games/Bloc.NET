namespace Bloc.NET;

using System;

/// <summary>
/// A bloc is a component that processes events and maintains a state. In
/// game development, blocs can be used in place of traditional state machines,
/// HSM's, or state charts. While blocs are not as rigorously defined as state
/// machines, they are easy to test and offer increased flexibility (and often
/// require less code).
/// </summary>
/// <typeparam name="TEvent">Type of events that the bloc receives.</typeparam>
/// <typeparam name="TState">Type of state that bloc maintains.</typeparam>
/// <typeparam name="TEffect">Type of effects the bloc can trigger.</typeparam>
public interface IBloc<TEvent, TState, TEffect> : IDisposable
  where TEvent : notnull
  where TState : IEquatable<TState>
  where TEffect : notnull {
  /// <summary>Current state of the bloc.</summary>
  TState State { get; }

  /// <summary>Observable stream of events added to the bloc.</summary>
  IObservable<TEvent> Events { get; }

  /// <summary>
  /// Observable stream of states emitted by the bloc that starts at the
  /// current state. To convert this to a pull-based object, call
  /// ToAsyncEnumerable or ToEnumerable on it.
  /// </summary>
  IObservable<TState> States { get; }

  /// <summary>Observable stream of effects triggered by the bloc.</summary>
  IObservable<TEffect> Effects { get; }

  /// <summary>Observable stream of errors occurring in the bloc.</summary>
  IObservable<Exception> Errors { get; }

  /// <summary>
  /// Event invoked whenever an instance of a bloc event is added to the bloc.
  /// </summary>
  event EventHandler<TEvent> OnEvent;

  /// <summary>
  /// Event invoked when the bloc's state changes. Note that this event is
  /// only invoked the next time the bloc's state changes. If you want to
  /// receive the current state immediately, use <see cref="OnState"/>.
  /// </summary>
  event EventHandler<TState> OnNextState;

  /// <summary>
  /// Event invoked when the bloc's state changes. This event is invoked
  /// immediately upon subscription with the bloc's current state and for all
  /// subsequent state changes. If you do not need the current state, use
  /// <see cref="OnNextState"/>.
  /// </summary>
  event EventHandler<TState> OnState;

  /// <summary>
  /// Event invoked when an effect is triggered from the bloc.
  /// </summary>
  event EventHandler<TEffect> OnEffect;

  /// <summary>
  /// Event invoked when the bloc adds an error.
  /// </summary>
  event EventHandler<Exception> OnNextError;

  /// <summary>Adds an event to the bloc.</summary>
  /// <param name="event">Event to add.</param>
  void Add(TEvent @event);

  /// <summary>Subscribes to all of the bloc's events and returns the
  /// subscription. The subscription runs until the subscription is disposed.
  /// </summary>
  /// <param name="onNext">Callback invoked when the bloc's state changes.
  /// </param>
  /// <param name="onError">Callback invoked when the bloc adds an error.
  /// </param>
  /// <param name="onCompleted">Callback invoked when the bloc is disposed.
  /// </param>
  IDisposable Listen(
    Action<TState> onNext,
    Action<Exception>? onError = null,
    Action? onCompleted = null
  );
}

/// <summary>
/// Base class for blocs.
/// </summary>
/// <typeparam name="TEvent">Type of events that the bloc receives.</typeparam>
/// <typeparam name="TState">Type of state that bloc maintains.</typeparam>
/// <typeparam name="TEffect">Type of effects the bloc can trigger.</typeparam>
public abstract class BlocBase<TEvent, TState, TEffect> :
  IBloc<TEvent, TState, TEffect>
  where TEvent : notnull
  where TState : IEquatable<TState>
  where TEffect : notnull {
  /// <inheritdoc/>
  public abstract TState State { get; }

  /// <inheritdoc/>
  public abstract IObservable<TEvent> Events { get; }

  /// <inheritdoc/>
  public abstract IObservable<TState> States { get; }

  /// <inheritdoc/>
  public abstract IObservable<TEffect> Effects { get; }

  /// <inheritdoc/>
  public abstract IObservable<Exception> Errors { get; }

  /// <inheritdoc/>
  public abstract event EventHandler<TEvent> OnEvent;

  /// <inheritdoc/>
  public abstract event EventHandler<TState> OnNextState;
  /// <inheritdoc/>
  public abstract event EventHandler<TState> OnState;
  /// <inheritdoc/>
  public abstract event EventHandler<Exception> OnNextError;
  /// <inheritdoc/>
  public abstract event EventHandler<TEffect> OnEffect;

  /// <inheritdoc/>
  public abstract void Add(TEvent @event);

  /// <summary>Adds an error to the bloc's error stream.</summary>
  /// <param name="e">Exception to add.</param>
  protected abstract void AddError(Exception e);

  /// <summary>
  /// Method invoked when the bloc encounters an error.
  /// </summary>
  /// <param name="e">The exception encountered.</param>
  protected abstract void OnError(Exception e);

  /// <summary>
  /// Triggers an effect. Blocs can call this method while handling events to
  /// trigger a one-shot side-effect. An effect can be any type of object that
  /// extends <typeparamref name="TEffect"/>.
  /// </summary>
  /// <param name="effect">Effect to trigger.</param>
  protected abstract void Trigger(TEffect effect);

  /// <inheritdoc/>
  public abstract IDisposable Listen(
    Action<TState> onNext,
    Action<Exception>? onError = null,
    Action? onCompleted = null
  );

  /// <summary>
  /// Closes the bloc and releases all resources.
  /// </summary>
  public abstract void Dispose();
}
