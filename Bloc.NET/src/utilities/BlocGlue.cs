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
  /// <returns>A new <see cref="BlocGlue{TEvent, TState}" /></returns>
  public static BlocGlue<TEvent, TState> Glue<TEvent, TState>(
    this IBloc<TEvent, TState> bloc
  ) where TState : notnull => new(bloc);
}

/// <summary>
/// Glue for a bloc. Glue allows you to select data from a bloc state and
/// perform an action whenever it changes. Using glue enables you to write
/// more declarative code and prevent unnecessary actions when a state is
/// changed but the relevant data within it has not.
/// </summary>
/// <typeparam name="TEvent">Type of the bloc's events.</typeparam>
/// <typeparam name="TState">Type of the bloc's state.</typeparam>
public class BlocGlue<TEvent, TState> : IDisposable where TState : notnull {
  /// <summary>
  /// Bloc that is glued.
  /// </summary>
  public IBloc<TEvent, TState> Bloc { get; }

  private TState _previousState;

  private readonly Dictionary<Type, List<IBlocStateGlue>> _stateGlues = new();
  private readonly Dictionary<Type, List<Action<TState, dynamic>>> _invokers =
    new();

  internal BlocGlue(IBloc<TEvent, TState> bloc) {
    Bloc = bloc;
    _previousState = bloc.State;
    Bloc.OnNextState += OnNextState;
  }

  /// <summary>
  /// Register glue actions for a specific type of state.
  /// </summary>
  /// <typeparam name="TSubstate">The type of state to glue.</typeparam>
  /// <returns>A <see cref="BlocStateGlueInvoker{TSubstate}" /> that allows
  /// actions to be registered for selected data within the state.</returns>
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

  private void Cleanup() {
    foreach (var stateGlueList in _stateGlues.Values) {
      foreach (var stateGlue in stateGlueList) {
        stateGlue.Cleanup();
      }
    }
    _stateGlues.Clear();
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

  /// <summary>
  /// Clean up registered glue actions for all states and stop listening
  /// for state changes.
  /// </summary>
  public void Dispose() {
    Bloc.OnNextState -= OnNextState;
    Cleanup();
  }

  /// <summary>
  /// Glue for a specific type of state.
  /// </summary>
  internal interface IBlocStateGlue {
    /// <summary>
    /// Invoke all registered glue actions for a specific type of state. Used
    /// by <see cref="BlocGlue{TEvent, TState}" />.
    /// </summary>
    /// <param name="state">Current state of the bloc.</param>
    /// <param name="previous">Previous state of the bloc.</param>
    /// <typeparam name="TOtherSubstate">Specific type of the bloc's current
    /// state.</typeparam>
    void Invoke<TOtherSubstate>(TOtherSubstate state, TState previous)
      where TOtherSubstate : TState;

    /// <summary>
    /// Clean up registered glue actions for a specific type of state.
    /// </summary>
    void Cleanup();
  }

  internal class BlocStateGlueInvoker<TSubstate> : IBlocStateGlue
    where TSubstate : TState {
    private readonly List<Action<dynamic, TState>> _actions = new();

    /// <inheritdoc />
    public void Invoke<TOtherSubstate>(TOtherSubstate state, TState previous)
      where TOtherSubstate : TState {
      foreach (var action in _actions) {
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

      _actions.Add(handler);

      return this;
    }

    /// <inheritdoc />
    public void Cleanup() => _actions.Clear();
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
