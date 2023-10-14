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
/// A read-only nonseekable stream that concatenates other streams.
/// </summary>
public class ConcatenatedStream: Stream
{
  private IEnumerable<Stream> _streamEnumerable;
  private IEnumerator<Stream> _streamEnumerator;
  private Stream? _currentStream;
  private bool _disposed;

  /// <summary>
  /// Create a new ConcatenatedStream
  /// </summary>
  /// <param name="streamEnumerable">
  /// The enumeration of streams to concatenate. This enumeration will
  /// be enumerated once, postponing each enumeration step until its
  /// value is needed. The iterator providing this enumerable is
  /// responsible for disposing the streams.
  /// </param>
  public ConcatenatedStream(
    IEnumerable<Stream> streamEnumerable)
  {
    _streamEnumerable = streamEnumerable;
    _streamEnumerator = _streamEnumerable.GetEnumerator();
    _currentStream = null; // initialize during first read
    OpenNextStream();
  }

  /// <summary>
  /// Create a <see cref="ConcatenatedStream"/> from a series of streams
  /// that are opened by the given opener functions and optionally closed before
  /// opening the next.
  /// </summary>
  /// <param name="openers">
  /// The functions that return the next child stream to read from
  /// </param>
  /// <param name="close">
  /// When true, each child stream is closed before the next opener function is called.
  /// </param>
  /// <returns>
  /// A new ConcatenatedStream
  /// </returns>
  public static ConcatenatedStream FromOpeners(IEnumerable<Func<Stream>> openers, bool close)
  {
    return new ConcatenatedStream(close ? OpenCloseOpeners(openers) : OpenNoCloseOpeners(openers));
  }

  /// <summary>
  /// Return a new <see cref="ConcatenatedStream"/> that delivers the specified slices of
  /// the sprecified single host stream.
  /// </summary>
  /// <param name="hostStream">
  /// The stream providing all of the slices
  /// </param>
  /// <param name="ranges">
  /// The ranges that define the slices to return
  /// </param>
  /// <returns>
  /// The ConcatenatedStream that concatenates the slices
  /// </returns>
  public static ConcatenatedStream FromSlices(Stream hostStream, IEnumerable<LongRange> ranges)
  {
    return new ConcatenatedStream(OpenSlices(hostStream, ranges));
  }

  private static IEnumerable<Stream> OpenSlices(Stream hostStream, IEnumerable<LongRange> ranges)
  {
    foreach(var range in ranges)
    {
      using(var stream = SimpleSubstream.FromHostSlice(hostStream, range.Length, range.Offset))
      {
        yield return stream;
      }
    }
  }

  private static IEnumerable<Stream> OpenCloseOpeners(IEnumerable<Func<Stream>> openers)
  {
    foreach(var opener in openers)
    {
      using(var stream = opener())
      {
        yield return stream;
      }
    }
  }

  private static IEnumerable<Stream> OpenNoCloseOpeners(IEnumerable<Func<Stream>> openers)
  {
    foreach(var opener in openers)
    {
      var stream = opener();
      yield return stream;
    }
  }

  /// <summary>
  /// Returns true.
  /// </summary>
  public override bool CanRead { get => true; }

  /// <summary>
  /// Returns false. ConcatenatedStream is not seekable.
  /// </summary>
  public override bool CanSeek { get => false; }

  /// <summary>
  /// Returns false. ConcatenatedStream is read only.
  /// </summary>
  public override bool CanWrite { get => false; }

  /// <summary>
  /// Throws a <see cref="NotSupportedException"/>
  /// </summary>
  public override long Length { get => throw new NotSupportedException(); }

  /// <summary>
  /// Throws a <see cref="NotSupportedException"/>
  /// </summary>
  public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

  /// <inheritdoc/>
  public override int Read(byte[] buffer, int offset, int count)
  {
    CheckNotDisposed();
    if(_currentStream == null)
    {
      return 0; // EOF
    }
    var total = 0;
    while(total < count)
    {
      Trace.Assert(_currentStream != null);
      var n = _currentStream!.Read(buffer, offset+total, count-total);
      total += n;
      // It is tempting to write "n<(count-total)" instead of "n==0" but let's support network
      // streams too (where 0 < n < (count-total) does not always imply EOF)
      if(n == 0 && !OpenNextStream())
      {
        return total;
      }
    }
    return total;
  }

  /// <inheritdoc/>
  public override int Read(Span<byte> buffer)
  {
    CheckNotDisposed();
    if(_currentStream == null)
    {
      return 0; // EOF
    }
    var total = 0;
    var count = buffer.Length;
    while(total < count)
    {
      Trace.Assert(_currentStream != null);
      var span = buffer[total..];
      var n = _currentStream!.Read(span);
      total += n;
      if(n == 0 && !OpenNextStream())
      {
        return total;
      }
    }
    return total;
  }

  /// <inheritdoc/>
  public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
  {
    CheckNotDisposed();
    if(_currentStream == null)
    {
      return 0; // EOF
    }
    var total = 0;
    while(total < count)
    {
      Trace.Assert(_currentStream != null);
      var n = await _currentStream!.ReadAsync(buffer.AsMemory(offset+total, count-total), cancellationToken);
      cancellationToken.ThrowIfCancellationRequested();
      total += n;
      if(n == 0 && !OpenNextStream())
      {
        return total;
      }
    }
    return total;
  }

  /// <inheritdoc/>
  public async override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
  {
    CheckNotDisposed();
    if(_currentStream == null)
    {
      return 0; // EOF
    }
    var total = 0;
    var count = buffer.Length;
    while(total < count)
    {
      Trace.Assert(_currentStream != null);
      var mem = buffer[total..];
      var n = await _currentStream!.ReadAsync(mem, cancellationToken);
      cancellationToken.ThrowIfCancellationRequested();
      total += n;
      if(n == 0 && !OpenNextStream())
      {
        return total;
      }
    }
    return total;
  }

  /// <inheritdoc/>
  public override int ReadByte()
  {
    // Implementing this in a straightforward way is more tricky than seems at first
    return base.ReadByte();
  }

  /// <summary>
  /// Open the next stream, if there is any left.
  /// </summary>
  /// <returns>
  /// True if the next stream was opened, false if there were none left
  /// </returns>
  private bool OpenNextStream()
  {
    CheckNotDisposed();
    while(true)
    {
      if(_streamEnumerator.MoveNext())
      {
        _currentStream = _streamEnumerator.Current;
        if(_currentStream != null) // else try next one
        {
          return true;
        }
      }
      else
      {
        // EOF
        return false;
      }
    }
  }

  private void CheckNotDisposed()
  {
    if(_disposed)
    {
      throw new ObjectDisposedException(GetType().FullName);
    }
  }

  /// <summary>
  /// Calls the flush method of the current child stream, if any
  /// </summary>
  public override void Flush()
  {
    if(!_disposed)
    { 
      _currentStream?.Flush();
    }
  }

  /// <inheritdoc/>
  public override long Seek(long offset, SeekOrigin origin)
  {
    throw new NotImplementedException();
  }
  
  /// <summary>
  /// Not supported
  /// </summary>
  /// <exception cref="NotSupportedException"></exception>
  public override void SetLength(long value)
  {
    throw new NotSupportedException();
  }

  /// <summary>
  /// Not supported
  /// </summary>
  /// <exception cref="NotSupportedException"></exception>
  public override void Write(byte[] buffer, int offset, int count)
  {
    throw new NotSupportedException();
  }
}
