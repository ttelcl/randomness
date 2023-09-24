using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xunit;
using Xunit.Abstractions;

using RandomUtilities.WordLists;

namespace UnitTest.RandomUtilities;

public class WordListTests
{
  private ITestOutputHelper _sink;

  public WordListTests(ITestOutputHelper sink)
  {
    _sink = sink;
  }

  [Fact]
  public void CanLoadSimpleList()
  {
    var wlc =
      WordListCache.Create()
      .AddApplicationFolder("WordLists")
      .AddCurrentFolder();
    var fruitcolors = wlc.FindList("fruitcolors");
    Assert.NotNull(fruitcolors);
    Assert.True(fruitcolors.Words.Count > 5);
    _sink.WriteLine($"Loaded list with {fruitcolors.Words.Count} entries.");
    _sink.WriteLine(String.Join(" ", fruitcolors.Words.Take(7)) + "...");
  }

  [Fact]
  public void CanLoadCompositeList()
  {
    var wlc =
      WordListCache.Create()
      .AddApplicationFolder("WordLists")
      .AddCurrentFolder();
    var colors = wlc.FindList("colors");
    Assert.NotNull(colors);
    Assert.True(colors.Words.Count > 5);
    _sink.WriteLine($"Loaded list with {colors.Words.Count} entries.");
    var wordset = new HashSet<string>(colors.Words);
    Assert.Contains("green", wordset);
    Assert.Contains("orange", wordset);
  }

  [Fact]
  public void CanListLists()
  {
    var wlc =
      WordListCache.Create()
      .AddApplicationFolder("WordLists")
      .AddCurrentFolder();
    var lists = wlc.ListNames().ToHashSet(StringComparer.InvariantCultureIgnoreCase);
    Assert.Contains("colors", lists);
    Assert.Contains("fruitcolors", lists);
    Assert.Contains("words", lists);
  }


}

