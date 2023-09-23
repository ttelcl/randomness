/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RandomUtilities.ByteSources;

/// <summary>
/// Extension methods related to Byte and Bit Sources
/// </summary>
public static class ByteSourceExtensions
{

  /// <summary>
  /// Wrap a <see cref="ByteSource"/> as a <see cref="BitSource"/>
  /// </summary>
  public static BitSource ToBitSource(this ByteSource source)
    => new BitSource(source);

  /// <summary>
  /// Wrap a <see cref="ByteSource"/> in a <see cref="BufferedByteSource"/>
  /// </summary>
  public static ByteSource Buffered(this ByteSource source, int buffersize = 1024)
    => new BufferedByteSource(source, buffersize);

  /// <summary>
  /// Return an infinite sequence of bytes
  /// </summary>
  public static IEnumerable<byte> Bytes(this ByteSource source)
  {
    while(true)
    {
      yield return source.ReadByte();
    }
  }

  /// <summary>
  /// Return the next 4 bytes from <paramref name="source"/> as an
  /// unsigned integer
  /// </summary>
  public static uint NextU32(this ByteSource source)
  {
    Span<byte> span = stackalloc byte[4];
    source.ReadBytes(span);
    return BinaryPrimitives.ReadUInt32LittleEndian(span);
  }

  /// <summary>
  /// Return the next 8 bytes from <paramref name="source"/> as an
  /// unsigned long integer
  /// </summary>
  public static ulong NextU64(this ByteSource source)
  {
    Span<byte> span = stackalloc byte[8];
    source.ReadBytes(span);
    return BinaryPrimitives.ReadUInt64LittleEndian(span);
  }

  /// <summary>
  /// Return the next 4 bytes from <paramref name="source"/> as a
  /// full range signed integer
  /// </summary>
  public static int NextI32(this ByteSource source)
  {
    Span<byte> span = stackalloc byte[4];
    source.ReadBytes(span);
    return BinaryPrimitives.ReadInt32LittleEndian(span);
  }

  /// <summary>
  /// Return the next 8 bytes from <paramref name="source"/> as a
  /// full range signed integer
  /// </summary>
  public static long NextI64(this ByteSource source)
  {
    Span<byte> span = stackalloc byte[8];
    source.ReadBytes(span);
    return BinaryPrimitives.ReadInt64LittleEndian(span);
  }

  /// <summary>
  /// Return the next 4 bytes from <paramref name="source"/> as a
  /// non-negative integer (31 bits)
  /// </summary>
  public static int NextI31(this ByteSource source)
  {
    return source.NextI32() & 0x7FFFFFFF;
  }

  /// <summary>
  /// Return the next 8 bytes from <paramref name="source"/> as a
  /// non-negative integer (63 bits)
  /// </summary>
  public static long NextI63(this ByteSource source)
  {
    return source.NextI64() & 0x7FFFFFFFFFFFFFFFL;
  }

  /// <summary>
  /// Return the next 32 or less bits from <paramref name="source"/> as an
  /// unsigned integer
  /// </summary>
  public static uint NextU32(this BitSource source, int bits = 32)
  {
    return source.ReadBitsU32(bits);
  }

  /// <summary>
  /// Return the next 32 bits from <paramref name="source"/> as a
  /// full range signed integer
  /// </summary>
  public static int NextI32(this BitSource source)
  {
    return (int)source.ReadBitsU32(32);
  }

  /// <summary>
  /// Return the next 31 or less bits from <paramref name="source"/> as a
  /// non-negative integer.
  /// </summary>
  public static int NextI31(this BitSource source, int bits = 31)
  {
    return source.ReadBitsI31(bits);
  }

  /// <summary>
  /// Return the next 64 or less bits from <paramref name="source"/> as an
  /// unsigned long integer
  /// </summary>
  public static ulong NextU64(this BitSource source, int bits = 64)
  {
    return source.ReadBitsU64(bits);
  }

  /// <summary>
  /// Return the next 64 bits from <paramref name="source"/> as a
  /// signed long integer
  /// </summary>
  public static long NextI64(this BitSource source)
  {
    return (int)source.ReadBitsU64(64);
  }

  /// <summary>
  /// Return the next 63 bits or fewer from <paramref name="source"/> as a
  /// non-negative long integer
  /// </summary>
  public static long NextI63(this BitSource source, int bits = 63)
  {
    return source.ReadBitsI63(bits);
  }

  /// <summary>
  /// Return an unsigned integer in the range 0..<paramref name="max"/> by
  /// repeatedly retrieving integers of the minimum required bit count from
  /// the random bit source until a value is found that meets the requirement
  /// </summary>
  /// <param name="randomSource">
  /// The source of random bits. For proper operation this must return uniformly
  /// distributed random numbers (true random or pseudorandom)
  /// </param>
  /// <param name="max">
  /// The maximum value to return.
  /// </param>
  /// <returns></returns>
  public static ulong RandomUnsigned(this BitSource randomSource, ulong max)
  {
    if(max == 0UL)
    {
      return 0UL;
    }
    var bits = 64 - BitOperations.LeadingZeroCount(max);
    ulong ul;
    do
    {
      ul = randomSource.ReadBitsU64(bits);
    } while(ul>max);
    return ul;
  }

  /// <summary>
  /// Return a random integer in the range <paramref name="min"/> (default 0) to
  /// <paramref name="max"/>.
  /// </summary>
  /// <param name="randomSource">
  /// The source of random bits. For proper operation this must return uniformly
  /// distributed random numbers (true random or pseudorandom)
  /// </param>
  /// <param name="max">
  /// The maximum value for the result
  /// </param>
  /// <param name="min">
  /// The minimum value for the result. Default 0. Must be no larger than <paramref name="max"/>
  /// </param>
  public static int RandomInteger(this BitSource randomSource, int max, int min = 0)
  {
    if(max < min)
    {
      throw new ArgumentException("Expecting 'max' to be larger or equal than 'min'");
    }
    var range = (ulong)(max - min);
    var rnd = (int)RandomUnsigned(randomSource, range);
    return min + rnd;
  }

  /// <summary>
  /// Equivalent to <see cref="RandomInteger(BitSource, int, int)"/>, except that
  /// <paramref name="min"/> defaults to 1 instead of 0, and <paramref name="max"/>
  /// defaults to 6 instead of not having a default.
  /// </summary>
  public static int DiceRoll(this BitSource randomSource, int max = 6, int min = 1)
  {
    return RandomInteger(randomSource, max, min);
  }

  /// <summary>
  /// Return a random double in the range 0.0 up to but not including 1.0
  /// </summary>
  /// <param name="randomSource">
  /// The source of random bytes. For proper operation this must return uniformly
  /// distributed random numbers (true random or pseudorandom)
  /// </param>
  public static double RandomDouble(this ByteSource randomSource)
  {
    Span<byte> span = stackalloc byte[8];
    randomSource.ReadBytes(span);
    // Mask it so it represents a Little Endian double in the range [1.0, 2.0)
    span[7] = 63;
    span[6] |= 0xF0;
    return BinaryPrimitives.ReadDoubleLittleEndian(span) - 1.0;
  }

  /// <summary>
  /// Return a random double, uniformly distributed in the specified range (if the
  /// random source is uniformly distributed)
  /// </summary>
  /// <param name="randomSource">
  /// The source of random bytes. For proper operation this must return uniformly
  /// distributed random numbers (true random or pseudorandom)
  /// </param>
  /// <param name="min">
  /// The minimum value to return (inclusive).
  /// This can be above <paramref name="max"/> to swap the inclusive / exclusive end of the range.
  /// </param>
  /// <param name="max">
  /// The maximum value to return (exclusive). 
  /// This can be below <paramref name="max"/> to swap the inclusive / exclusive end of the range.
  /// </param>
  public static double RandomDouble(this ByteSource randomSource, double min, double max)
  {
    var range = max - min;
    return min + range * RandomDouble(randomSource);
  }

  /// <summary>
  /// Randomly shuffle the elements of the <paramref name="target"/> Span. All permutations
  /// are equally likely, including the identity permutation (i.e. no change at all)
  /// </summary>
  public static void RandomShuffle<T>(this BitSource randomSource, Span<T> target)
  {
    for(var i = target.Length - 1; i > 0; i--)
    {
      var j = randomSource.RandomInteger(i);
      // It may be tempting to use randomSource.RandomInteger(i-1), but that would produce
      // a lopsided distribution of permutations: "i" must be inclusive to allow the (unlikely)
      // possibility of randomly generating the identity permutation.
      if(j != i)
      {
        (target[i], target[j]) = (target[j], target[i]);
      }
    }
  }

  /// <summary>
  /// Randomly shuffle the elements of the <paramref name="target"/> list (in-place).
  /// All permutations are equally likely, including the identity permutation (i.e. no change at all)
  /// </summary>
  public static void RandomShuffle<T>(this BitSource randomSource, IList<T> target)
  {
    for(var i = target.Count - 1; i > 0; i--)
    {
      var j = randomSource.RandomInteger(i);
      // It may be tempting to use randomSource.RandomInteger(i-1), but that would produce
      // a lopsided distribution of permutations: "i" must be inclusive to allow the (unlikely)
      // possibility of randomly generating the identity permutation.
      if(j != i)
      {
        (target[i], target[j]) = (target[j], target[i]);
      }
    }
  }

  /// <summary>
  /// Return a random permutation of the source as a new IReadOnlyList.
  /// </summary>
  public static IReadOnlyList<T> RandomPermute<T>(this BitSource randomSource, IReadOnlyList<T> source)
  {
    var target = source.ToArray();
    RandomShuffle(randomSource, target);
    return target;
  }

  /// <summary>
  /// Return an infinite sequence of randomly picked elements from <paramref name="values"/>.
  /// </summary>
  public static IEnumerable<T> RandomProjectedSequence<T>(this BitSource randomSource, IReadOnlyList<T> values)
  {
    while(true)
    {
      var index = randomSource.RandomInteger(values.Count-1);
      yield return values[index];
    }
  }

  /// <summary>
  /// Return a string formed by indexing the <paramref name="alphabet"/> randomly 
  /// <paramref name="characterCount"/> times.
  /// </summary>
  public static string RandomAlphabetProjection(this BitSource randomSource, string alphabet, int characterCount)
  {
    var sb = new StringBuilder();
    for(var i = 0; i < characterCount; i++)
    {
      var index = randomSource.RandomInteger(alphabet.Length-1);
      sb.Append(alphabet[index]);
    }
    return sb.ToString();
  }

}
