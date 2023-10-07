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

namespace WikiDataLib.Utilities;

/*
 * State machine description:
 * 
 * This class implements a state machine that is specialized to detect the
 * following byte sequence (in hexadecimal): 42.5A.68.3*.31.41.59.26.53.59
 * (where the fourth byte can be anything from 0x31 to 0x39, indicating
 * different BZ2 compression levels). This sequence indicates the start
 * of a substream in a BZ2 file.
 * 
 * States:
 *  0 - in-between streams, awaiting the next start
 *  1 - got 42
 *  2 - got 42.5A
 *  3 - got 42.5A.68
 *  4 - got 42.5A.68.3* (last byte being in range 0x31 - 0x39
 *  5 - got 42.5A.68.3*.31
 *  6 - got 42.5A.68.3*.31.41
 *  7 - got 42.5A.68.3*.31.41.59
 *  8 - got 42.5A.68.3*.31.41.59.26
 *  9 - got 42.5A.68.3*.31.41.59.26.53
 * 10 - got 42.5A.68.3*.31.41.59.26.53.59 - transitional equivalent to state 0, but marking output as valid
 * 
 */

/// <summary>
/// Utility to locate the start of substreams in BZ2 files
/// </summary>
public class Bz2SubstreamStatemachine
{
  private int _state;
  private long _position;
  private long _start; // candidate start, nit necessarily true

  /// <summary>
  /// Create a new Bz2StreamFinder
  /// </summary>
  /// <param name="offset">
  /// An optional start position offset (affecting
  /// <see cref="Position"/> and <see cref="SubstreamStart"/>)
  /// </param>
  public Bz2SubstreamStatemachine(long offset = 0L)
  {
    Reset(offset);
  }

  /// <summary>
  /// Values in <see cref="SubstreamStart"/> and <see cref="CompressionLevel"/>
  /// are valid if this is true.
  /// </summary>
  public bool Valid { get => _state==10; }

  /// <summary>
  /// The most recently discovered sub-stream starting offset
  /// </summary>
  public long SubstreamStart { get; private set; }

  /// <summary>
  /// The next byte position
  /// </summary>
  public long Position { get => _position; }

  /// <summary>
  /// The most recently discovered compression level
  /// </summary>
  public int CompressionLevel { get; private set; }

  /// <summary>
  /// Reset to the initial state, optionally offsetting the start position
  /// </summary>
  public void Reset(long position = 0L)
  {
    _state = 0;
    _position = position;
    _start = 0L;
    SubstreamStart = 0L;
    CompressionLevel = 0;
  }

  /// <summary>
  /// Push the next byte into the state machine (and advance the
  /// tracked position). Returns true if the last byte of the
  /// pattern was recognized; in that case <see cref="SubstreamStart"/>
  /// and <see cref="CompressionLevel"/> are valid.
  /// </summary>
  public bool PushByte(byte value)
  {
    if(value == 0x42) 
    {
      // Independent of state, since it only appears once
      // This is the only way to break out of state 0 and
      // an alternative way of breaking out of state 10.
      _state = 1;
      _start = _position;
    }
    else if(_state != 0)
    {
      if(_state == 3 && value>=0x31 && value <= 0x39)
      {
        _state = 4;
        CompressionLevel = value - 0x30;
      }
      else
      {
        var statePlusValue = (_state<<8) | value;
        // Pattern: 42.5A.68.3*.31.41.59.26.53.59
        _state = statePlusValue switch {
          0x015A => 2,
          0x0268 => 3,
          // >= 0x0331 and <= 0x339 => 4, // already handled
          0x0431 => 5,
          0x0541 => 6,
          0x0659 => 7,
          0x0726 => 8,
          0x0853 => 9,
          0x0959 => 10,
          _ => 0, // reset to idling. This includes also any input other than 0x42 in state 10
        };
        if(_state == 10)
        {
          SubstreamStart = _start;
        }
      }
    }
    _position++;
    return _state == 10;
  }

  /// <summary>
  /// Enumerate substream start positions in the stream: each time a
  /// start position is found, the current state of this object is returned.
  /// Make sure to copy the state, since it is volatile.
  /// </summary>
  /// <param name="stream">
  /// The BZ2 stream to analyze. This state machine is reset to the current
  /// position of that stream.
  /// </param>
  /// <returns>
  /// A sequence containing this state machine repeatedly, once after
  /// each substream start detection
  /// </returns>
  public IEnumerable<Bz2SubstreamStatemachine> FindStarts(Stream stream)
  {
    Reset(stream.Position);
    var buffer = new byte[1024];
    while(true)
    {
      var n = stream.Read(buffer, 0, buffer.Length);
      if(n == 0)
      {
        break; 
      }
      for(var i = 0; i < n; i++)
      {
        if(PushByte(buffer[i]))
        {
          yield return this;
        }
      }
    }
  }

}
