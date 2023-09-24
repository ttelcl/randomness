using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xunit;
using Xunit.Abstractions;

using RandomUtilities.ByteSources;

namespace UnitTest.RandomUtilities;

public class RandomUtilitiesTests
{
  private ITestOutputHelper _sink;

  public RandomUtilitiesTests(ITestOutputHelper sink)
  {
    _sink = sink;
  }

  [Fact]
  public void CycleByteSourceBehavesAsExpected()
  {
    var source = new CycleByteSource();

    for(var i = 0; i < 300; i++)
    {
      var b = source.ReadByte();
      Assert.Equal((byte)i, b);
    }

    var bufferedSource = new BufferedByteSource(new CycleByteSource(), 31 /* relatively prime to 256 */);
    for(var i = 0; i < 300; i++)
    {
      var b = bufferedSource.ReadByte();
      Assert.Equal((byte)i, b);
    }
  }

  [Fact]
  public void TestBitSource()
  {
    var bitSource = new BitSource(new CycleByteSource());
    for(var i = 0; i < 20; i++)
    {
      var sb = new StringBuilder();
      for(var j = 0; j < 10; j++)
      {
        var n = bitSource.ReadBitsI31(24);
        sb.Append($" {n:X06}");
      }
      _sink.WriteLine(sb.ToString());
    }
  }

  [Fact]
  public void TestBitSource2()
  {
    var bitSource = new BitSource(new CycleByteSource());
    for(var i = 0; i < 20; i++)
    {
      var sb = new StringBuilder();
      for(var j = 0; j < 10; j++)
      {
        var n = bitSource.ReadBitsI31(23);
        sb.Append($" {n:X06}");
      }
      _sink.WriteLine(sb.ToString());
    }
  }

  [Fact]
  public void TestRandomUnsigned()
  {
    var bitSource = ByteSource.Random().Buffered().ToBitSource();
    var cap = 9999999999999999999UL;
    //var cap = 9UL;
    var count = 1000;
    var samples =
      SequenceGenerator.InfiteFrom(() => bitSource.RandomUnsigned(cap))
      .Take(count)
      .ToList();
    // _sink.WriteLine(String.Join(" ", samples.Select(ul => ul.ToString())));
    Assert.All(samples, x => Assert.InRange(x, 0UL, cap));
    var halfCap = cap / 2;
    var aboveHalf = samples.Where(x => x > halfCap).Count();
    var belowHalf = samples.Count - aboveHalf;
    var percentageAbove = (aboveHalf * 100) / samples.Count;
    _sink.WriteLine($"below / above = {belowHalf} / {aboveHalf} ~ {100 - percentageAbove}% / {percentageAbove}% ");

    // This could occasionally fail based on randomness, but the 35%/65% margin seems quite safe:
    Assert.InRange(percentageAbove, 35, 65);
  }

  [Fact]
  public void TestRandomDouble()
  {
    var byteSource = ByteSource.Random().Buffered();
    var count = 1000;
    var samples =
      SequenceGenerator.InfiteFrom(() => byteSource.RandomDouble())
      .Take(count)
      .ToList();
    Assert.All(samples, x => Assert.InRange(x, 0.0, 1.0));
    {
      var aboveHalf = samples.Where(x => x > 0.5).Count();
      var belowHalf = samples.Count - aboveHalf;
      var percentageAbove = (aboveHalf * 100) / samples.Count;
      _sink.WriteLine($"below 0.50 / above 0.50 = {belowHalf} / {aboveHalf} ~ {100 - percentageAbove}% / {percentageAbove}% ");
      // This could occasionally fail based on randomness, but the 35%/65% margin seems quite safe:
      Assert.InRange(percentageAbove, 35, 65);
    }
    {
      var aboveQuart = samples.Where(x => x > 0.25).Count();
      var belowQuart = samples.Count - aboveQuart;
      var percentageAbove = (aboveQuart * 100) / samples.Count;
      _sink.WriteLine($"below 0.25 / above 0.25 = {belowQuart} / {aboveQuart} ~ {100 - percentageAbove}% / {percentageAbove}% ");
      // This could occasionally fail based on randomness, but the 35%/65% margin seems quite safe:
      //Assert.InRange(percentageAbove, 35, 65);
    }
  }

  [Fact]
  public void ShuffleTest()
  {
    var bitSource = ByteSource.Random().Buffered().ToBitSource();
    var trackMap = new Dictionary<string, int>() {
      ["ABC"] = 0,
      ["ACB"] = 0,
      ["BCA"] = 0,
      ["BAC"] = 0,
      ["CAB"] = 0,
      ["CBA"] = 0,
    };

    var count = 600;
    for(var i = 0; i < count; i++)
    {
      var source = "ABC".ToCharArray();
      bitSource.RandomShuffle(source);
      var s = new String(source);
      Assert.True(trackMap.ContainsKey(s));
      trackMap[s]++;
    }

    foreach(var kvp in trackMap)
    {
      _sink.WriteLine($"'{kvp.Key}' = {kvp.Value,4} / {count}");
      Assert.True(kvp.Value > 0);
    }
  }

}
