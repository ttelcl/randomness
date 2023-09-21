/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomUtilities.ByteSources
{
  /// <summary>
  /// A byte source for testing, returning the values 0x00-0xFF ever repeating
  /// </summary>
  public class CycleByteSource: ByteSource
  {
    private byte _next;

    /// <summary>
    /// Create a new CycleByteSource
    /// </summary>
    public CycleByteSource()
    {
      _next = 0;
    }

    /// <inheritdoc/>
    public override byte ReadByte()
    {
      return _next++;
    }

    /// <inheritdoc/>
    public override void ReadBytes(Span<byte> bytes)
    {
      for(var i=0; i<bytes.Length; i++)
      {
        bytes[i] = _next++;
      }
    }
  }
}
