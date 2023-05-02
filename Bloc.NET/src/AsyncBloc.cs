namespace Bloc.NET;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

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
public abstract class AsyncBloc<TEvent, TState, TAction>
  : GenericBloc<TEvent, TState, TAction> where TState : IEquatable<TState> {
  /// <summary>
  /// Creates a new bloc with the given initial state.
  /// </summary>
  /// <param name="initialState">Initial state of the bloc.</param>
  public AsyncBloc(TState initialState) : base(initialState) { }

  /// <summary>
  /// Whenever a bloc processes an event, this method is called with the event
  /// and expected to return the next state of the bloc. This method is used
  /// to map an event to a new state based on the bloc's current state.
  /// </summary>
  /// <param name="event">The event to process.</param>
  public abstract IAsyncEnumerable<TState> MapEventToState(TEvent @event);

  /// <inheritdoc/>
  protected sealed override IObservable<TState> ConvertEvent(TEvent @event) =>
    MapEventToState(@event).ToObservable();

  /// <inheritdoc/>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  protected sealed override bool ShouldThrow(Exception e) => false;
  // Async blocs shouldn't throw exceptions when adding an event. Adding the
  // error to the error stream is sufficient.
}

/// <summary>
/// <para>
/// A bloc is a component that processes events and maintains a state. In
/// game development, blocs can be used in place of traditional state machines,
/// HSM's, or state charts. While blocs are not as rigorously defined as state
/// machines, they are easy to test and offer increased flexibility (and often
/// require less code).
/// </para>
/// <para>
/// Unlike <see cref="AsyncBloc{TEvent, TState, TAction}"/>, this bloc cannot
/// trigger actions. The lack of actions replicates the API surface of the
/// original bloc library for Flutter and is useful when you do not need blocs
/// to trigger one-shot actions unrelated to state.
/// </para>
/// </summary>
/// <typeparam name="TEvent">Type of events that the bloc receives.</typeparam>
/// <typeparam name="TState">Type of state that bloc maintains.</typeparam>
public abstract class Bloc<TEvent, TState>
  : AsyncBloc<TEvent, TState, object> where TState : IEquatable<TState> {
  /// <summary>
  /// Creates a new bloc with the given initial state.
  /// </summary>
  /// <param name="initialState">Initial state of the bloc.</param>
  public Bloc(TState initialState) : base(initialState) { }

  /// <inheritdoc/>
  protected override void Trigger(object action) =>
    throw new InvalidOperationException(
      "This bloc does not support actions. Use an AsyncBloc to trigger actions."
    );
}
