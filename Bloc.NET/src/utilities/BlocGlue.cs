namespace Bloc.NET;

using System;
using System.Collections.Generic;
using Bloc.NET.Extensions;

/// <summary>
/// Glue extension for Blocs — select data from a bloc state and perform an
/// action whenever it changes.
/// </summary>
public static class BlocGlueExtensions {
  /// <summary>
  /// Create glues for a bloc.
  /// </summary>
  /// <param name="bloc">The bloc to glue.</param>
  /// <typeparam name="TEvent">Type of the bloc's events.</typeparam>
  /// <typeparam name="TState">Type of the bloc's state.</typeparam>
  /// <typeparam name="TEffect">Type of the bloc's effects.</typeparam>
  /// <returns>A new <see cref="BlocGlue{TEvent, TState, TEffect}" /></returns>
  public static BlocGlue<TEvent, TState, TEffect> Glue<TEvent, TState, TEffect>(
    this IBloc<TEvent, TState, TEffect> bloc
  ) where TEvent : notnull
    where TState : IEquatable<TState>
    where TEffect : notnull => new(bloc);
}

/// <summary>
/// Glue for a bloc. Glue allows you to select data from a bloc state and
/// perform an action whenever it changes. Using glue enables you to write
/// more declarative code and prevent unnecessary updates when a state is
/// changed but the relevant data within it has not.
/// </summary>
/// <typeparam name="TEvent">Type of the bloc's events.</typeparam>
/// <typeparam name="TState">Type of the bloc's state.</typeparam>
/// <typeparam name="TEffect">Type of the bloc's effects.</typeparam>
public sealed class BlocGlue<TEvent, TState, TEffect>
  : IDisposable where TEvent : notnull
  where TState : IEquatable<TState>
  where TEffect : notnull {
  /// <summary>
  /// Bloc that is glued.
  /// </summary>
  public IBloc<TEvent, TState, TEffect> Bloc { get; }

  private TState _previousState;

  private readonly Dictionary<Type, List<IBlocStateGlue>> _stateGlues = new();
  private readonly Dictionary<Type, List<Action<TState, dynamic>>> _invokers =
    new();
  private readonly Dictionary<Type, Action<TEffect>> _effectHandlers = new();

  internal BlocGlue(IBloc<TEvent, TState, TEffect> bloc) {
    Bloc = bloc;
    _previousState = bloc.State;
    Bloc.OnNextState += OnNextState;
    Bloc.OnEffect += OnEffect;
  }

  /// <summary>
  /// Register bindings for a specific type of state.
  /// </summary>
  /// <typeparam name="TSubstate">The type of state to glue to.</typeparam>
  /// <returns>A <see cref="BlocStateGlueInvoker{TSubstate}" /> that allows
  /// bindings to be registered for selected data within the state.</returns>
  public BlocStateGlue<TSubstate> When<TSubstate>()
    where TSubstate : TState {
    var type = typeof(TSubstate);
    var stateGlue = new BlocStateGlueInvoker<TSubstate>();

    _stateGlues.AddIfNotPresent(type, new());
    _invokers.AddIfNotPresent(type, new());

    _stateGlues[type].Add(stateGlue);
    _invokers[type].Add(
      (state, prev) => stateGlue.Invoke((TSubstate)state, (TState)prev)
    );

    return new BlocStateGlue<TSubstate>(stateGlue);
  }

  /// <summary>
  /// Registers a side-effect handler for the bloc.
  /// </summary>
  /// <typeparam name="TEffectType">The type of side effect to handle.
  /// </typeparam>
  /// <param name="handler">Action which handles an instance of the effect.
  /// </param>
  public BlocGlue<TEvent, TState, TEffect> Handle<TEffectType>(
    Action<TEffectType> handler
  ) where TEffectType : TEffect {
    var type = typeof(TEffectType);
    _effectHandlers[type] = (TEffect effect) => handler((TEffectType)effect!);
    return this;
  }

  private void Cleanup() {
    foreach (var stateGlueList in _stateGlues.Values) {
      foreach (var stateGlue in stateGlueList) {
        stateGlue.Cleanup();
      }
    }
    _stateGlues.Clear();
    _invokers.Clear();
    _effectHandlers.Clear();
  }

  private void OnNextState(object? _, TState state) {
    var type = state.GetType();
    if (_invokers.TryGetValue(type, out var glues)) {
      for (var i = 0; i < glues.Count; i++) {
        var glue = glues[i];
        _invokers[state.GetType()][i](state, _previousState);
      }
    }

    _previousState = state;
  }

  private void OnEffect(object? _, TEffect effect) {
    var type = effect.GetType();
    if (_effectHandlers.TryGetValue(type, out var handler)) {
      handler(effect);
    }
  }

  /// <summary>
  /// Clean up registered glue bindings for all states and stop listening
  /// for state changes.
  /// </summary>
  public void Dispose() => Dispose(true);

  private void Dispose(bool disposing) {
    if (disposing) {
      Bloc.OnEffect -= OnEffect;
      Bloc.OnNextState -= OnNextState;
      Cleanup();
    }
  }

  /// <summary>Glue finalizer.</summary>
  ~BlocGlue() {
    Dispose(false);
  }

  /// <summary>
  /// Glue for a specific type of state.
  /// </summary>
  internal interface IBlocStateGlue {
    /// <summary>
    /// Invoke all registered glue bindings for a specific type of state. Used
    /// by <see cref="BlocGlue{TEvent, TState, TEffect}" />.
    /// </summary>
    /// <param name="state">Current state of the bloc.</param>
    /// <param name="previous">Previous state of the bloc.</param>
    /// <typeparam name="TOtherSubstate">Specific type of the bloc's current
    /// state.</typeparam>
    void Invoke<TOtherSubstate>(TOtherSubstate state, TState previous)
      where TOtherSubstate : TState;

    /// <summary>
    /// Clean up registered glue bindings for a specific type of state.
    /// </summary>
    void Cleanup();
  }

  internal class BlocStateGlueInvoker<TSubstate> : IBlocStateGlue
    where TSubstate : TState {
    private readonly List<Action<dynamic, TState>> _bindings = new();

    /// <inheritdoc />
    public void Invoke<TOtherSubstate>(TOtherSubstate state, TState previous)
      where TOtherSubstate : TState {
      foreach (var action in _bindings) {
        action(state, previous);
      }
    }

    public BlocStateGlueInvoker<TSubstate> Use<TSelected>(
      Func<TSubstate, TSelected> data, Action<TSelected> to
    ) {
      var handler = (dynamic state, TState previous) => {
        var selectedData = data((TSubstate)state);
        if (previous is TSubstate previousSubstate) {
          var previousData = data(previousSubstate);
          if (
            EqualityComparer<TSelected>.Default.Equals(
              selectedData, previousData
            )
          ) {
            // Selected data hasn't changed. No need to update!
            return;
          }
        }

        to(selectedData);
      };

      _bindings.Add(handler);

      return this;
    }

    /// <inheritdoc />
    public void Cleanup() => _bindings.Clear();
  }

  /// <summary>
  /// Glue for a specific type of state.
  /// </summary>
  /// <typeparam name="TSubstate">The type of state that is glued.</typeparam>
  public class BlocStateGlue<TSubstate> where TSubstate : TState {
    internal BlocStateGlueInvoker<TSubstate> StateGlue { get; }

    internal BlocStateGlue(BlocStateGlueInvoker<TSubstate> stateGlue) {
      StateGlue = stateGlue;
    }

    /// <summary>
    /// Selects data from the state and performs an action whenever the selected
    /// data changes.
    /// </summary>
    /// <param name="data">Data selected from the bloc's state.</param>
    /// <param name="to">Action to perform when selected data changes.</param>
    /// <typeparam name="TSelected">Type of the selected data.</typeparam>
    public BlocStateGlue<TSubstate> Use<TSelected>(
      Func<TSubstate, TSelected> data, Action<TSelected> to
    ) {
      StateGlue.Use(data, to);
      return this;
    }
  }
}
