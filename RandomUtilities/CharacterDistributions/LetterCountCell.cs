/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomUtilities.CharacterDistributions;

/// <summary>
/// A letter and a count, together with information for use in a word generator
/// </summary>
public class LetterCountCell
{
  /// <summary>
  /// Create a new LetterCount
  /// </summary>
  public LetterCountCell(
    char letter,
    int count,
    string prefix,
    int previousTotal)
  {
    Prefix = prefix;
    Letter = letter;
    Count = count;
    Cumulative = previousTotal + count;
    NextPrefix = prefix.Length == 0 ? String.Empty : (prefix[1..] + letter);
  }

  /// <summary>
  /// The letter described by this record (in the context of the prefix)
  /// </summary>
  public char Letter { get; init; }

  /// <summary>
  /// The weight recorded for how often <see cref="Prefix"/> is followed by <see cref="Letter"/>
  /// </summary>
  public int Count { get; init; }

  /// <summary>
  /// The prefix for this record
  /// </summary>
  public string Prefix { get; init; }

  /// <summary>
  /// The cumulative weight for all preceding records plus this one
  /// </summary>
  public int Cumulative {  get; init; }

  /// <summary>
  /// The pre-calculated next prefix in case this letter was picked
  /// </summary>
  public string NextPrefix { get; init; }

}


