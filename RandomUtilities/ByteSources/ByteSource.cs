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
/// Abstract read-only infinite byte stream
/// </summary>
public abstract class ByteSource
{
  /// <summary>
  /// Create a new ByteSource
  /// </summary>
  protected ByteSource()
  {
  }

  /// <summary>
  /// Return a new <see cref="SecureRandomByteSource"/> instance
  /// </summary>
  public static ByteSource Random()
  {
    return new SecureRandomByteSource();
  }

  /// <summary>
  /// Return a new <see cref="CycleByteSource"/> instance
  /// (for testing purposes)
  /// </summary>
  public static ByteSource Cycle()
  {
    return new CycleByteSource();
  }

  /// <summary>
  /// Return the next byte. The default implementation is a wrapper around
  /// <see cref="ReadBytes"/>.
  /// </summary>
  /// <returns>
  /// The next byte from the infinite stream
  /// </returns>
  public virtual byte ReadByte()
  {
    Span<byte> oneByte = stackalloc byte[1];
    ReadBytes(oneByte);
    return oneByte[0];
  }

  /// <summary>
  /// Read the next bytes from the infinite stream
  /// </summary>
  /// <param name="bytes">
  /// The span to receive the bytes read. The length of the span determines the number
  /// of bytes to read.
  /// </param>
  public abstract void ReadBytes(Span<byte> bytes);
}
