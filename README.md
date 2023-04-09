# 🧊 Bloc.NET

[![Chickensoft Badge][chickensoft-badge]][chickensoft-website] [![Discord][discord-badge]][discord] [![Read the docs][read-the-docs-badge]][docs] ![line coverage][line-coverage] ![branch coverage][branch-coverage]

A predictable state management library for C# that helps implement the [BLoC design pattern][bloc-pattern].

---

<p align="center">
<img alt="Bloc.NET" src="Bloc.NET/icon.png" width="200">
</p>

---

> You may know this library from it's more famous counterpart in the Dart/Flutter universe — but there's no reason we can't have one for .NET, too! Thanks to Felix (the creator of the original [Bloc][bloc-gh] library) for consulting on this C# port.

## About Bloc

In some ways, a bloc is similar to a state machine, but does not require the developer to define the entire table of state transitions ahead of time. Instead of being told to transition to a specific state, blocs listen to a stream of events and yield out states.

The increase in flexibility that comes from using blocs reduces the pain of implementing complex or nested/hierarchical state machines (and state charts, for that matter). The reduction of code allows the developer to focus more clearly on constructing the correct state in response to events, but does lack the rigidity of a state machine. Feel free to use whatever patterns are appropriate for your application.

There are a few things to be aware of regarding this implementations:

- No docs! If you're interested in the pattern, I suggest reading the [documentation for the Dart/Flutter version of Bloc][bloc]. Dart and C# are both object-oriented, managed languages with garbage collection, so the concepts are similar enough.

- Not actually equivalent to modern Dart/Flutter blocs. The bloc implementation in this library cannot support concurrent events — all events are processed sequentially and in-order. If events take a while to process, they simply queue up. This is equivalent to how the original Bloc library worked prior to Bloc v8.

- Both asynchronous and synchronous implementations are provided for C#. The Dart/Flutter implementation does not have a specific synchronous implementation. Since C# is commonly used in game development (and I ported this package to use it with game development), I am providing a synchronous implementation as I believe it will be useful in many situations.

  The synchronous implementation differs slightly from the asynchronous one: if an error occurs in the asynchronous implementation, it does not throw and stop execution (since the event is processed asynchronously, errors are not thrown as they would not even be in the same context of the event that triggered the error). Instead, the error is added to the bloc and the bloc can choose how to handle it.
  
  The synchronous implementation, however, will throw an exception when an event is added that triggers an invalid state. I believe this is desirable as it greatly facilitates debugging in simpler situations where forcing everything to be asynchronous is overly complex or too much of a performance hit.

[chickensoft-badge]: https://raw.githubusercontent.com/chickensoft-games/chickensoft_site/main/static/img/badges/chickensoft_badge.svg
[chickensoft-website]: https://chickensoft.games
[discord-badge]: https://raw.githubusercontent.com/chickensoft-games/chickensoft_site/main/static/img/badges/discord_badge.svg
[discord]: https://discord.gg/gSjaPgMmYW
[read-the-docs-badge]: https://raw.githubusercontent.com/chickensoft-games/chickensoft_site/main/static/img/badges/read_the_docs_badge.svg
[docs]: https://chickensoft.games/docsickensoft%20Discord-%237289DA.svg?style=flat&logo=discord&logoColor=white
[line-coverage]: Bloc.NET.Tests/badges/line_coverage.svg
[branch-coverage]: Bloc.NET.Tests/badges/branch_coverage.svg

[bloc-pattern]: https://www.didierboelens.com/2018/08/reactive-programming-streams-bloc/
[bloc-gh]: https://github.com/felangel/bloc
[bloc]: https://bloclibrary.dev/#/
