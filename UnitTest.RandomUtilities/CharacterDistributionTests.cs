using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xunit;
using Xunit.Abstractions;

using RandomUtilities.ByteSources;
using RandomUtilities.WordLists;
using RandomUtilities.CharacterDistributions;

namespace UnitTest.RandomUtilities;

public class CharacterDistributionTests
{
  private ITestOutputHelper _sink;

  public CharacterDistributionTests(ITestOutputHelper sink)
  {
    _sink = sink;
  }

  [Fact]
  public void CanCreateOrder0Distrigram()
  {
    const string listname = "words";
    var alphabet = new Alphabet("abcdefghijklmnopqrstuvwxyz");
    var dist0 = alphabet.CreateDistributionRecorder(0);
    var wlc = WordListCache.Create().AddApplicationFolder("WordLists");
    var words = wlc.FindList(listname, wlc);
    Assert.NotNull(words);

    foreach(var word in words.Words)
    {
      dist0.AddWord(word);
    }

    var kvp0list = dist0.AllDistributions().ToList();
    Assert.Single(kvp0list);
    Assert.Equal("", kvp0list[0].Key);
    var distribution0 = kvp0list[0].Value;
    var counts = distribution0.Distribution;
    var total = distribution0.Total;
    var entropy = distribution0.CalculateEntropy();
    _sink.WriteLine($"Total Count = {total}. Entropy = {entropy}");
    for(var i = 0; i < counts.Count; i++)
    {
      var character = alphabet.Characters[i].ToString().Replace(alphabet.Boundary, '-');
      var count = counts[i];
      var surprisal = distribution0.Surprisal(i) ?? Double.PositiveInfinity ;
      _sink.WriteLine($"'{character}': {count,4} / {total} / {surprisal:F6}");
    }
  }
}
