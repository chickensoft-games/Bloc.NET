namespace Bloc.NET.Extensions.Tests;

using Bloc.NET.Extensions;
using Shouldly;
using Xunit;

public class DictionaryExtensionsTest {
  [Fact]
  public void AddIfNotPresentDoesNothingIfPresent() {
    var dictionary = new Dictionary<string, int> {
      ["a"] = 10
    };
    dictionary.AddIfNotPresent("a", 20);
    dictionary["a"].ShouldBe(10);
  }

  [Fact]
  public void AddIfNotPresentAddsIfNotPresent() {
    var dictionary = new Dictionary<string, int>();
    dictionary.AddIfNotPresent("a", 20);
    dictionary["a"].ShouldBe(20);
  }
}
