/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RandomUtilities.ByteSources;

namespace RandomUtilities.Picking;

/// <summary>
/// A specialized <see cref="PickSequence{T}"/> that
/// returns wordlets concatenated from tiny text fragments
/// (possibly single-character strings)
/// </summary>
public class WordletPicker: PickSequence<string>
{
  /// <summary>
  /// Create a new WordletPicker
  /// </summary>
  public WordletPicker(
    IEnumerable<IEnumerable<string>> fragmentSequence)
    : base(fragmentSequence)
  {
  }

  /// <summary>
  /// Pick a random wordlet constructed by concatenating
  /// random picks for each fragment
  /// </summary>
  public string PickWordlet(
    bool capitalize,
    BitSource randomSource)
  {
    var fragments = Pick(randomSource);
    var wordlet = String.Concat(fragments);
    if(capitalize)
    {
      wordlet = Char.ToUpper(wordlet[0]).ToString() + wordlet[1..];
    }
    return wordlet;
  }

  /// <summary>
  /// Example WordletPicker 1.
  /// </summary>
  public static WordletPicker Instance1 { get; } =
    new WordletPicker([
      ["b", "d", "k", "p", "t", "v", ""],
      ["l", "r", "s", "w"],
      ["a", "ai", "e", "ee", "i", "o", "oo", "u"],
      ["l", "r", "s", "w", "f", "ch", "j", "m", "n"],
      ["", "k", "p", "t"],
    ]);

  /// <summary>
  /// Example WordletPicker 2.
  /// </summary>
  public static WordletPicker Instance2 { get; } =
    new WordletPicker([
      ["b", "d", "k", "p", "t", "v", ""],
      ["l", "r", "s", "w"],
      ["a", "ai", "e", "ee", "i", "o", "oo", "u"],
      ["l", "r", "s", "w", "f", "ch", "j", "m", "n"],
      ["a", "ai", "e", "ee", "i", "o", "oo", "u", ""],
      ["", "k", "p", "t"],
      ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"]
    ]);
}
