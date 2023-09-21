/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomUtilities.ByteSources;

/// <summary>
/// An infinite read-only stream of bits, based on a wrapped ByteStream
/// </summary>
public class BitSource
{
  private ulong _buffer;
  private ulong _mask;
  private ByteSource _source;


  /// <summary>
  /// Create a new BitSource
  /// </summary>
  public BitSource(ByteSource source)
  {
    _source = source;
    _mask = 0UL;
  }

  /// <summary>
  /// Return the next <paramref name="bits"/> from the bit stream,
  /// as an unsigned long integer in the range from 0 to (1&lt;&lt;<paramref name="bits"/>)-1.
  /// The underlying byte stream is treated as Big Endian.
  /// </summary>
  /// <param name="bits">
  /// The number of bits to return (valid range 0-64)
  /// </param>
  public ulong ReadBitsU64(int bits)
  {
    if(bits < 0 || bits > 64)
    {
      throw new ArgumentOutOfRangeException(nameof(bits));
    }
    if(bits == 0)
    {
      return 0UL;
    }
    var ret = 0UL;
    while(bits > 0)
    {
      if(_mask == 0)
      {
        Refill();
      }
      var bit = (_buffer & _mask) == 0 ? 0UL : 1UL;
      ret = (ret << 1) | bit;
      _mask >>= 1;
      bits--;
    }
    return ret;
  }

  /// <summary>
  /// Return the next <paramref name="bits"/> from the bit stream,
  /// as a signed long integer in the range from 0 to (1&lt;&lt;<paramref name="bits"/>)-1.
  /// The underlying byte stream is treated as Big Endian.
  /// </summary>
  /// <param name="bits">
  /// The number of bits to return (valid range 0-63)
  /// </param>
  public long ReadBitsI63(int bits)
  {
    if(bits < 0 || bits > 63)
    {
      throw new ArgumentOutOfRangeException(nameof(bits));
    }
    return (long)ReadBitsU64(bits);
  }

  /// <summary>
  /// Return the next <paramref name="bits"/> from the bit stream,
  /// as an unsigned integer in the range from 0 to (1&lt;&lt;<paramref name="bits"/>)-1.
  /// The underlying byte stream is treated as Big Endian.
  /// </summary>
  /// <param name="bits">
  /// The number of bits to return (valid range 0-32)
  /// </param>
  public uint ReadBitsU32(int bits)
  {
    if(bits < 0 || bits > 32)
    {
      throw new ArgumentOutOfRangeException(nameof(bits));
    }
    return (uint)ReadBitsU64(bits);
  }

  /// <summary>
  /// Return the next <paramref name="bits"/> from the bit stream,
  /// as a signed integer in the range from 0 to (1&lt;&lt;<paramref name="bits"/>)-1.
  /// The underlying byte stream is treated as Big Endian.
  /// </summary>
  /// <param name="bits">
  /// The number of bits to return (valid range 0-31)
  /// </param>
  public int ReadBitsI31(int bits)
  {
    if(bits < 0 || bits > 31)
    {
      throw new ArgumentOutOfRangeException(nameof(bits));
    }
    return (int)ReadBitsU64(bits);
  }

  /// <summary>
  /// Return the next <paramref name="bits"/> from the bit stream,
  /// as a byte in the range from 0 to (1&lt;&lt;<paramref name="bits"/>)-1.
  /// The underlying byte stream is treated as Big Endian.
  /// </summary>
  /// <param name="bits">
  /// The number of bits to return (valid range 0-8)
  /// </param>
  public byte ReadBitsByte(int bits)
  {
    if(bits < 0 || bits > 8)
    {
      throw new ArgumentOutOfRangeException(nameof(bits));
    }
    return (byte)ReadBitsU64(bits);
  }

  private void Refill()
  {
    Span<byte> bytes = stackalloc byte[8];
    _source.ReadBytes(bytes);
    _buffer = BinaryPrimitives.ReadUInt64BigEndian(bytes);
    _mask = 0x8000000000000000UL;
  }

}
