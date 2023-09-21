using Xunit;

using RandomUtilities.ByteSources;
using System.Text;
using Xunit.Abstractions;

namespace UnitTest.RandomUtilities;

public class UnitTest1
{
  private ITestOutputHelper _sink;

  public UnitTest1(ITestOutputHelper sink)
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
      _sink.WriteLine( sb.ToString());
    }
  }

}
