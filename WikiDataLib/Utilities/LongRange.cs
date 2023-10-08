/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikiDataLib.Utilities;

/// <summary>
/// A range defined by an offset and a length as Int64 values
/// </summary>
public struct LongRange
{
  /// <summary>
  /// Create a new LongRange
  /// </summary>
  public LongRange(long offset, long length)
  {
    Offset = offset;
    Length = length;
  }

  /// <summary>
  /// The start offset of the range
  /// </summary>
  public long Offset { get; }

  /// <summary>
  /// The length of the range
  /// </summary>
  public long Length { get; }

  /// <summary>
  /// The tail of the range (first index after the range)
  /// </summary>
  public long Tail { get => Offset + Length; }
}