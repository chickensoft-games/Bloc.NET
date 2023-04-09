namespace Chickensoft.GoDotNet;

using System;
using System.Collections.Generic;

/// <summary>
/// A bloc is a component that processes events and maintains a state. In
/// game development, blocs can be used in place of traditional state machines,
/// HSM's, or state charts. While blocs are not as rigorously defined as state
/// machines, they are easy to test and offer increased flexibility (and often
/// require less code).
/// </summary>
/// <typeparam name="TEvent">Type of events that the bloc receives.</typeparam>
/// <typeparam name="TState">Type of state that bloc maintains.</typeparam>
public interface IBloc<TEvent, TState> : IDisposable {
  /// <summary>Current state of the bloc.</summary>
  TState State { get; }

  /// <summary>
  /// The asynchronous stream of states emitted by the bloc.
  /// </summary>
  IAsyncEnumerable<TState> Stream { get; }

  /// <summary>
  /// <para>
  /// Synchronous iterator of states emitted by the bloc that starts at the
  /// current state. If there is no next state, trying to access the next state
  /// will result in a hang. Instead, only call MoveNext if you are sure that
  /// the bloc has emitted a new state.
  /// </para>
  /// <para>
  /// In general, this will not be as useful as <see cref="Stream"/>. Prefer
  /// <see cref="Stream"/> unless you have a specific reason to use this.
  /// </para>
  /// </summary>
  IEnumerable<TState> States { get; }

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
public abstract class BlocBase<TEvent, TState> : IBloc<TEvent, TState>
  where TState : IEquatable<TState> {
  /// <inheritdoc/>
  public abstract TState State { get; }

  /// <inheritdoc/>
  public abstract IAsyncEnumerable<TState> Stream { get; }

  /// <inheritdoc/>
  public abstract IEnumerable<TState> States { get; }

  /// <inheritdoc/>
  public abstract event EventHandler<TState> OnNextState;
  /// <inheritdoc/>
  public abstract event EventHandler<TState> OnState;
  /// <inheritdoc/>
  public abstract event EventHandler<Exception> OnNextError;

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
