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
  private readonly Dictionary<long, long> _index;

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
    _index = new Dictionary<long, long>();
    FirstOffset = Int64.MaxValue;
    LastOffset = -1L;
    Reload();
    if(LastOffset < FirstOffset)
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
  /// A mapping of substream offset to substream length for all defined
  /// substreams.
  /// </summary>
  public IReadOnlyDictionary<long, long> Substreams { get => _index; }

  /// <summary>
  /// Find the length of the substream starting at the specified
  /// <paramref name="offset"/>. If <paramref name="inclusive"/>
  /// is true, the first and last substream are valid, otherwise
  /// they are hidden
  /// </summary>
  public long? FindLength(long offset, bool inclusive)
  {
    if(!inclusive && (offset <= FirstOffset || offset >= LastOffset))
    {
      return null; 
    }
    return _index.TryGetValue(offset, out var value) ? value : null;
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
  /// <returns>
  /// The opened substream, or null if the offset is not a valid substream offset
  /// </returns>
  public SimpleSubstream? Open(Stream host, long offset, bool inclusive)
  {
    var length = FindLength(offset, inclusive);
    return length is null ? null : SimpleSubstream.FromHostSlice(host, length.Value, offset);
  }

  /// <summary>
  /// Reload the index from the file named by <see cref="IndexFileName"/>.
  /// </summary>
  public void Reload()
  {
    _index.Clear();
    FirstOffset = Int64.MaxValue;
    LastOffset = -1L;
    foreach(var line in File.ReadLines(IndexFileName))
    {
      var parts = line.Split(',', 3);
      if(parts.Length >= 2 && Int64.TryParse(parts[0], out var offset) && Int64.TryParse(parts[1], out var length))
      {
        if(offset < FirstOffset)
        {
          FirstOffset = offset;
        }
        if(offset > LastOffset)
        {
          LastOffset = offset;
        }
        _index[offset] = length;
      }
    }
  }
}
