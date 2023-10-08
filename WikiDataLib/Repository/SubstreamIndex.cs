/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WikiDataLib.Utilities;

namespace WikiDataLib.Repository;

/// <summary>
/// An in-memory index of sub-streams available in a wiki dump file
/// (or any file, really), loaded from a *.stream-index.csv file.
/// </summary>
public class SubstreamIndex
{
  private readonly List<long> _offsets;
  private readonly List<long> _lengths;

  /// <summary>
  /// Create a new SubstreamIndex object, loading the content of the
  /// index file.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The index file is expected to be in CSV(-like) format, where
  /// the first column is the offset of a substream (as a decimal number),
  /// and the second is its length. Additional columns are allowed but ignored.
  /// Rows where the first column is not a decimal number are ignored
  /// </para>
  /// </remarks>
  public SubstreamIndex(
    string indexFileName,
    string targetFileName)
  {
    IndexFileName = Path.GetFullPath(indexFileName);
    TargetFileName = Path.GetFullPath(targetFileName);
    _offsets = new List<long>();
    _lengths = new List<long>();
    Offsets = _offsets.AsReadOnly();
    Lengths = _lengths.AsReadOnly();
    FirstOffset = Int64.MaxValue;
    LastOffset = -1L;
    Reload();
    if(Count <= 0)
    {
      throw new InvalidDataException(
        $"No index entries found in {IndexFileName}");
    }
  }

  /// <summary>
  /// The name of the index file loaded in this instance
  /// </summary>
  public string IndexFileName { get; init; }

  /// <summary>
  /// The name of the target file where the index refers to
  /// </summary>
  public string TargetFileName { get; init; }

  /// <summary>
  /// The offset of the first Substream
  /// </summary>
  public long FirstOffset { get; private set; }

  /// <summary>
  /// The offset of the last Substream
  /// </summary>
  public long LastOffset { get; private set; }

  /// <summary>
  /// The length of the target file, as determined by the final
  /// substream range.
  /// </summary>
  public long TargetLength { get; private set; }

  /// <summary>
  /// All substream offset positions, sorted
  /// </summary>
  public IReadOnlyList<long> Offsets { get; init; }

  /// <summary>
  /// All substream lengths (paired to Offsets)
  /// </summary>
  public IReadOnlyList<long> Lengths { get; init; }

  /// <summary>
  /// Get the number of substream descriptions (including the
  /// head and tail substreams)
  /// </summary>
  public int Count { get => Offsets.Count; }

  /// <summary>
  /// Find the index of the range in <see cref="Offsets"/> and <see cref="Lengths"/>
  /// that matches the specified <paramref name="position"/>.
  /// If <paramref name="exact"/> is true, only exact matches of the offset of
  /// a range succeed; otherwise the range in which the position lies is returned.
  /// Returns -1 on failure.
  /// </summary>
  /// <param name="position">
  /// The position in <see cref="TargetFileName"/> to match.
  /// </param>
  /// <param name="exact">
  /// When true, only exact matches of the start offset of a range succeed.
  /// </param>
  /// <param name="inclusive">
  /// When false, the first and last range are hidden (returning -1 if they
  /// would match)
  /// </param>
  /// <returns>
  /// An index in <see cref="Offsets"/> and <see cref="Lengths"/> on success, -1 on failure.
  /// </returns>
  public int FindRangeIndex(long position, bool exact, bool inclusive)
  {
    var index = _offsets.BinarySearch(position);
    if(index >= 0)
    {
      // Exact match
      if(inclusive || (index > 0 && index < _offsets.Count-1))
      {
        return index;
      }
      else
      {
        return -1;
      }
    }
    else if(exact)
    {
      return -1;
    }
    else
    {
      index = (~index) - 1;
      // Index now is the index that is next smaller than position or Count-1 if larger than all
      // Count-1 may also indicate being part of the final substream though
      if(!inclusive)
      {
        if(index > 0 && index <= _offsets.Count-1)
        {
          return index;
        }
        else
        {
          return -1;
        }
      }
      // now (indirectly) special-case "index = _offsets.Count-1" by comparing
      // position against the tail of the final substream
      if(position >= _offsets[^1] + _lengths[^1])
      {
        // Beyond the end of the target file
        return -1;
      }
      // Note that a position before the first substream already resulted in "index" being -1 here.
      return index;
    }
  }

  /// <summary>
  /// Try to find the <see cref="LongRange"/> of the substream that contains
  /// the given <paramref name="position"/>.
  /// </summary>
  /// <param name="position">
  /// The position to search for
  /// </param>
  /// <param name="exact">
  /// If true, only exact matches of the start of a range can be successful.
  /// </param>
  /// <param name="inclusive">
  /// If true all ranges can be matched. If false, the first and last range are excluded.
  /// </param>
  /// <param name="range">
  /// If successful: the offset and length describing the range.
  /// </param>
  /// <returns>
  /// True if successful
  /// </returns>
  public bool TryFindRange(long position, bool exact, bool inclusive, out LongRange range)
  {
    var idx = FindRangeIndex(position, exact, inclusive);
    if(idx < 0)
    {
      range = new LongRange();
      return false;
    }
    else
    {
      range = new LongRange(_offsets[idx], _lengths[idx]);
      return true;
    }
  }

  /// <summary>
  /// Open a Substream of the host if the given offset is a known substream
  /// offset
  /// </summary>
  /// <param name="host">
  /// The host stream
  /// </param>
  /// <param name="offset">
  /// The offset identifying the substream
  /// </param>
  /// <param name="inclusive">
  /// When false, the first and last substream act as if not valid
  /// </param>
  /// <param name="exact">
  /// If true (default), the <paramref name="offset"/> must be the exact start of
  /// a substream. Otherwise any offset in a substream can be used to identify it.
  /// </param>
  /// <returns>
  /// The opened substream, or null if the offset is not a valid substream offset
  /// </returns>
  public SimpleSubstream? Open(Stream host, long offset, bool inclusive, bool exact = true)
  {
    if(TryFindRange(offset, exact, inclusive, out var range))
    {
      return SimpleSubstream.FromHostSlice(host, range.Length, range.Offset);
    }
    else
    {
      return null;
    }
  }

  /// <summary>
  /// Reload the index from the file named by <see cref="IndexFileName"/>.
  /// </summary>
  public void Reload()
  {
    _offsets.Clear();
    _lengths.Clear();
    var ranges = new List<LongRange>();
    foreach(var line in File.ReadLines(IndexFileName))
    {
      var parts = line.Split(',', 3);
      if(parts.Length >= 2 && Int64.TryParse(parts[0], out var offset) && Int64.TryParse(parts[1], out var length))
      {
        ranges.Add(new LongRange(offset, length));
      }
    }
    var longSort = Comparer<long>.Default;
    ranges.Sort((r1, r2) => longSort.Compare(r1.Offset, r2.Offset));
    TargetLength = (ranges.Count > 0) ? ranges[^1].Tail : 0L;
    foreach(var lr in ranges)
    {
      _offsets.Add(lr.Offset);
      _lengths.Add(lr.Length);
    }
    if(Count <= 0)
    {
      throw new InvalidDataException(
        $"No index entries found in {IndexFileName}");
    }
    FirstOffset = Offsets[0];
    LastOffset = Offsets[^1];
  }
}
