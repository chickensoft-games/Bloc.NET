namespace Bloc.NET;

using System;
using System.Collections.Generic;
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
public abstract class SyncBloc<TEvent, TState, TAction> :
  GenericBloc<TEvent, TState, TAction>
  where TEvent : notnull
  where TState : IEquatable<TState>
  where TAction : notnull {
  /// <summary>
  /// Creates a new bloc with the given initial state.
  /// </summary>
  /// <param name="initialState">Initial state of the bloc.</param>
  public SyncBloc(TState initialState) : base(initialState) { }

  /// <summary>
  /// Registers an event handler for a specific event type. The event handler
  /// is a function that receives an event of a specific type and emits one or
  /// more states in response to the event.
  /// </summary>
  /// <param name="handler">Event handler.</param>
  /// <typeparam name="TEventType">Type of the event.</typeparam>
  protected void On<TEventType>(Func<TEventType, IEnumerable<TState>> handler)
    where TEventType : TEvent =>
      On<TEventType>((@event) => handler(@event).ToObservable());

  /// <inheritdoc/>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  protected override bool ShouldThrow(Exception e) => true;
  // Sync blocs should always throw exceptions when adding an event.
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
/// Unlike <see cref="SyncBloc{TEvent, TState, TAction}" />, this bloc cannot
/// trigger actions. The lack of actions replicates the API surface of the
/// original bloc library for Flutter and is useful when you do not need blocs
/// to trigger one-shot actions unrelated to state.
/// </para>
/// </summary>
/// <typeparam name="TEvent">Type of events that the bloc receives.</typeparam>
/// <typeparam name="TState">Type of state that bloc maintains.</typeparam>
public abstract class SyncBlocClassic<TEvent, TState> :
  SyncBloc<TEvent, TState, object>
  where TEvent : notnull
  where TState : IEquatable<TState> {
  /// <summary>
  /// Creates a new bloc with the given initial state.
  /// </summary>
  /// <param name="initialState">Initial state of the bloc.</param>
  public SyncBlocClassic(TState initialState) : base(initialState) { }

  /// <inheritdoc/>
  protected override void Trigger(object action) =>
    throw new InvalidOperationException(
      "This bloc does not support actions. Use an AsyncBloc to trigger actions."
    );
}
