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
    public FakeBloc() : base(new FakeBlocState.StateA(1, 2)) {
      On<IFakeBlocEvent.EventOne>(One);
      On<IFakeBlocEvent.EventTwo>(Two);
    }

    private IEnumerable<FakeBlocState> One(
      IFakeBlocEvent.EventOne @event
    ) {
      yield return new FakeBlocState.StateA(@event.Value1, @event.Value2);
    }

    private IEnumerable<FakeBlocState> Two(
      IFakeBlocEvent.EventTwo @event
    ) {
      yield return new FakeBlocState.StateB(@event.Value1, @event.Value2);
    }
  }
}
