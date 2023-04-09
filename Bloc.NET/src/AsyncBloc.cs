namespace Chickensoft.GoDotNet;

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
public abstract class AsyncBloc<TEvent, TState> : GenericBloc<TEvent, TState>
  where TState : IEquatable<TState> {
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
