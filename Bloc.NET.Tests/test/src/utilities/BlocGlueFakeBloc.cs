namespace Bloc.NET.Tests;

using System.Collections.Generic;

public partial class BlocGlueTests {
  public interface IFakeBlocEvent {
    public record struct EventOne(int Value1, int Value2) : IFakeBlocEvent;
    public record struct EventTwo(string Value1, string Value2)
      : IFakeBlocEvent;
  }

  public abstract record FakeBlocState {
    public record StateA(int Value1, int Value2) : FakeBlocState;
    public record StateB(string Value1, string Value2) : FakeBlocState;
  }

  public class FakeBloc : SyncBlocClassic<IFakeBlocEvent, FakeBlocState> {
    public FakeBloc() : base(new FakeBlocState.StateA(1, 2)) { }

    public override IEnumerable<FakeBlocState> MapEventToState(
      IFakeBlocEvent @event
    ) {
      if (@event is IFakeBlocEvent.EventOne one) {
        yield return new FakeBlocState.StateA(one.Value1, one.Value2);
      }
      else if (@event is IFakeBlocEvent.EventTwo two) {
        yield return new FakeBlocState.StateB(two.Value1, two.Value2);
      }
    }
  }
}
