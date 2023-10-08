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
/// Exposes a view on a slice of a host stream as a separate stream.
/// This stream is read-only, does not support seeking, and assumes
/// that the host stream is not used except via this SimpleSubstream
/// while this stream is "alive"
/// </summary>
public class SimpleSubstream: Stream
{
  private readonly long _length;
  private long _position;
  private bool _disposed;

  /// <summary>
  /// Create a new ReadOnlySubStream, exposing the next <paramref name="length"/>
  /// bytes of the host stream as a read-only nonseekable stream
  /// </summary>
  /// <param name="hostStream">
  /// The host stream from which a part is exposed by this ReadOnlySubstream
  /// Closing this ReadOnlySubstream does not close the host stream.
  /// </param>
  /// <param name="length">
  /// The length of the stream slice to expose.
  /// </param>
  public SimpleSubstream(Stream hostStream, long length)
  {
    HostStream = hostStream;
    _length = length;
    _position = 0;
    if(!hostStream.CanRead)
    {
      throw new ArgumentOutOfRangeException(nameof(hostStream), "Expecting a readable host stream");
    }
    if(length < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(length), "length cannot be negative");
    }
  }

  /// <summary>
  /// Return a new SimpleSubstream containing the next <paramref name="length"/>
  /// bytes of the host stream. This overload does not require the host stream
  /// to be seekable.
  /// </summary>
  public static SimpleSubstream FromHostSlice(Stream hostStream, long length)
  {
    return new SimpleSubstream(hostStream, length);
  }

  /// <summary>
  /// Reposition the host stream to <paramref name="offset"/> and return a new
  /// SimpleSubstream containing the next <paramref name="length"/> bytes of the
  /// host stream. This overload requires the host stream to be seekable.
  /// </summary>
  public static SimpleSubstream FromHostSlice(Stream hostStream, long length, long offset)
  {
    hostStream.Position = offset;
    return new SimpleSubstream(hostStream, length);
  }

  /// <summary>
  /// The host stream
  /// </summary>
  public Stream HostStream { get; init; }

  /// <inheritdoc/>
  public override bool CanRead { get => true; }

  /// <inheritdoc/>
  public override bool CanSeek { get => false; }

  /// <inheritdoc/>
  public override bool CanWrite { get => false; }

  /// <inheritdoc/>
  public override long Length { get => _length; }

  /// <summary>
  /// The number of bytes remaining to be read
  /// </summary>
  public long Remaining { get => _length - _position; }

  /// <summary>
  /// Get the virtual position (as derived from tracking reads).
  /// This class does not support seeking, so an attempt to set this
  /// value will throw <see cref="NotSupportedException"/>
  /// </summary>
  public override long Position {
    get => !_disposed ? _position : throw new ObjectDisposedException("SimpleSubstream");
    set => throw new NotSupportedException("SimpleSubstream does not support seeking");
  }

  /// <summary>
  /// Throws <see cref="NotSupportedException"/>
  /// </summary>
  /// <exception cref="NotSupportedException">Always thrown</exception>
  public override long Seek(long offset, SeekOrigin origin)
  {
    throw new NotSupportedException("SimpleSubstream does not support seeking");
  }

  /// <inheritdoc/>
  public override void Flush()
  {
    // allow calls even if disposed
    HostStream.Flush();
  }

  /// <inheritdoc/>
  public override int Read(byte[] buffer, int offset, int count)
  {
    ThrowIfDisposed();
    if(count > Remaining)
    {
      count = (int)Remaining;
    }
    if(count <= 0)
    {
      return 0;
    }
    var n = HostStream.Read(buffer, offset, count);
    if(n == 0)
    {
      throw new EndOfStreamException("Unexpected end of host stream");
    }
    _position += n;
    return n;
  }

  /// <inheritdoc/>
  public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
  {
    ThrowIfDisposed();
    if(count > Remaining)
    {
      count = (int)Remaining;
    }
    if(count <= 0)
    {
      return 0;
    }
    var n = await HostStream.ReadAsync(buffer, offset, count, cancellationToken);
    if(n == 0)
    {
      throw new EndOfStreamException("Unexpected end of host stream");
    }
    _position += n;
    return n;
  }

  /// <inheritdoc/>
  public override int Read(Span<byte> buffer)
  {
    ThrowIfDisposed();
    var slice = buffer.Length > Remaining ? buffer[..(int)Remaining] : buffer;
    if(slice.Length == 0)
    {
      return 0;
    }
    var n = HostStream.Read(slice);
    if(n == 0)
    {
      throw new EndOfStreamException("Unexpected end of host stream");
    }
    _position += n;
    return n;
  }

  /// <inheritdoc/>
  public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
  {
    ThrowIfDisposed();
    var slice = buffer.Length > Remaining ? buffer[..(int)Remaining] : buffer;
    var n = await HostStream.ReadAsync(slice, cancellationToken);
    if(n == 0)
    {
      throw new EndOfStreamException("Unexpected end of host stream");
    }
    _position += n;
    return n;
  }

  /// <inheritdoc/>
  public override int ReadByte()
  {
    ThrowIfDisposed();
    if(Remaining <= 0)
    {
      return -1;
    }
    var value = HostStream.ReadByte();
    if(value < 0)
    {
      throw new EndOfStreamException("Unexpected end of host stream");
    }
    _position++;
    return value;
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

  /// <summary>
  /// Clean up. In this case this just sets an internal flag that the stream is disposed
  /// </summary>
  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);
    _disposed = true;
  }

  private void ThrowIfDisposed()
  {
    if(_disposed)
    {
      throw new ObjectDisposedException("SimpleSubstream");
    }
  }
}
