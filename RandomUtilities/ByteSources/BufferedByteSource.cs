/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomUtilities.ByteSources;

/// <summary>
/// Wraps a ByteSource with a buffer, reducing the number of calls
/// to the wrapped <see cref="ByteSource"/>
/// </summary>
public class BufferedByteSource: ByteSource
{
  private readonly ByteSource _source;
  private readonly byte[] _buffer;
  private int _position;

  /// <summary>
  /// Create a new BufferedByteSource
  /// </summary>
  public BufferedByteSource(ByteSource source, int bufferSize = 1024)
  {
    _source = source;
    _buffer = new byte[bufferSize];
    _position = bufferSize;
  }

  /// <summary>
  /// Fill <paramref name="bytes"/> with bytes from this buffer, refilling this
  /// buffer as needed.
  /// </summary>
  public override void ReadBytes(Span<byte> bytes)
  {
    var outPosition = 0;
    while(outPosition < bytes.Length)
    {
      var toFill = bytes.Length - outPosition;
      if(_position >= _buffer.Length)
      {
        _source.ReadBytes(_buffer);
        _position = 0;
      }
      var available = _buffer.Length - _position;
      var n = available < toFill ? available : toFill;
      _buffer.AsSpan(_position, n).CopyTo(bytes.Slice(outPosition, n));
      outPosition += n;
      _position += n;
    }
  }
}
