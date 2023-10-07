/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WikiDataLib.Utilities;

/// <summary>
/// Exposes a part of another stream
/// </summary>
public class ReadOnlySubstream: Stream
{
  /// <summary>
  /// Create a new ReadOnlySubStream, exposing the slice of the
  /// specified <paramref name="length"/> starting at the specified <paramref name="offset"/>
  /// </summary>
  /// <param name="hostStream">
  /// The host stream from which a part is exposed by this ReadOnlySubstream.
  /// Closing the this ReadOnlySubstream does not close the host stream.
  /// </param>
  /// <param name="length">
  /// The length of the stream to expose (positioned before the offset in the parameter list
  /// to stay in line with the other constructor)
  /// </param>
  /// <param name="offset">
  /// The offset at which the sub streaam starts.
  /// </param>
  public ReadOnlySubstream(Stream hostStream, long length, long offset)
  {
    HostStream = hostStream;
    if(!hostStream.CanRead)
    {
      throw new ArgumentOutOfRangeException(nameof(hostStream), "Expecting a readable host stream");
    }
    HostOffset = offset;
    Size = length;
    if(length < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(length), "length cannot be negative");
    }
    if(offset < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(offset), "offset cannot be negative");
    }
    if(length + offset > hostStream.Length)
    {
      throw new ArgumentException("The end of the substream cannot be beyond the end of the host stream");
    }
    ClampHost();
  }

  /// <summary>
  /// Create a new ReadOnlySubStream, exposing the slice of the
  /// specified <paramref name="length"/> starting at the host stream's current offset
  /// </summary>
  /// <param name="hostStream">
  /// The host stream from which a part is exposed by this ReadOnlySubstream
  /// Closing the this ReadOnlySubstream does not close the host stream.
  /// </param>
  /// <param name="length">
  /// The length of the stream to expose.
  /// </param>
  public ReadOnlySubstream(Stream hostStream, long length)
    : this(hostStream, length, hostStream.Position)
  {
  }

  /// <summary>
  /// The host stream
  /// </summary>
  public Stream HostStream { get; init; }

  /// <summary>
  /// The offset of this ReadOnlySubstream in the host stream
  /// </summary>
  public long HostOffset { get; init; }

  /// <summary>
  /// The immutable length of this ReadOnlySubStream
  /// </summary>
  public long Size { get; init; }

  /// <inheritdoc/>
  public override bool CanRead { get => true; }

  /// <inheritdoc/>
  public override bool CanSeek { get => HostStream.CanSeek; }

  /// <inheritdoc/>
  public override bool CanWrite { get => false; }

  /// <inheritdoc/>
  public override long Length { get => Size; }

  /// <inheritdoc/>
  public override long Position {
    get => HostStream.Position - HostOffset;
    set {
      if(value < 0)
      {
        value = 0;
      }
      if(value > Size)
      {
        value = Size;
      }
      HostStream.Position = value + HostOffset;
    }
  }

  /// <inheritdoc/>
  public override long Seek(long offset, SeekOrigin origin)
  {
    switch(origin)
    {
      case SeekOrigin.Begin:
        Position = offset; 
        break;
      case SeekOrigin.Current:
        Position += offset;
        break;
      case SeekOrigin.End:
        Position = Size + offset;
        break;
    }
    return Position;
  }

  /// <inheritdoc/>
  public override void Flush()
  {
    HostStream.Flush();
  }

  /// <inheritdoc/>
  public override int Read(byte[] buffer, int offset, int count)
  {
    throw new NotImplementedException();
  }

  /// <inheritdoc/>
  public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  /// <inheritdoc/>
  public override int Read(Span<byte> buffer)
  {
    throw new NotImplementedException();
  }

  /// <inheritdoc/>
  public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  /// <inheritdoc/>
  public override int ReadByte()
  {
    throw new NotImplementedException();
  }

  /// <summary>
  /// Throws <see cref="NotSupportedException"/>
  /// </summary>
  public override void SetLength(long value)
  {
    throw new NotSupportedException("This implementation is read-only");
  }

  /// <summary>
  /// Throws <see cref="NotSupportedException"/>
  /// </summary>
  public override void Write(byte[] buffer, int offset, int count)
  {
    throw new NotSupportedException("This implementation is read-only");
  }

  private void ClampHost()
  {
    HostStream.Position = ClampedPosition(HostStream.Position-HostOffset) + HostOffset;
  }

  private long ClampedPosition(long position)
  {
    if(position < 0)
    {
      return 0L;
    }
    if(position > Size)
    {
      return Size;
    }
    return position;
  }
}
