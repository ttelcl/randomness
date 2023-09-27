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
/// A distribution of characters from an <see cref="Alphabet"/>
/// </summary>
public class AlphabetCharacterDistribution
{
  private readonly int[] _distribution;

  /// <summary>
  /// Create a new AlphabetCharacterDistribution
  /// </summary>
  public AlphabetCharacterDistribution(
    Alphabet alphabet)
  {
    _distribution = new int[alphabet.CharacterCount];
  }

  /// <summary>
  /// The frequency counts for each character of the associated Alphabet
  /// </summary>
  public IReadOnlyList<int> Distribution { get => _distribution; }

  /// <summary>
  /// The total weight
  /// </summary>
  public int Total { get; private set; }

  /// <summary>
  /// Return the entropy of this distribution (expressed in bits)
  /// </summary>
  public double CalculateEntropy()
  {
    // https://en.wikipedia.org/wiki/Entropy_(information_theory)
    if(Total == 0)
    {
      throw new InvalidOperationException(
        "The distribution is empty (entropy is ill defined)");
    }
    double total = Total;
    var entropy = 0.0;
    for(var i=0; i < _distribution.Length; i++)
    {
      var n = _distribution[i];
      if(n > 0)
      {
        var p = n / total;
        entropy -= p * Math.Log2(p);
      }
    }
    return entropy;
  }

  /// <summary>
  /// Return the surprisal (self-information) in bits for the character at the
  /// given index in the alphabet, returning null instead of Infinite
  /// if no samples were recorded for that character.
  /// </summary>
  public double? Surprisal(int index)
  {
    // https://en.wikipedia.org/wiki/Entropy_(information_theory)
    var n = _distribution[index];
    return
      n == 0 // Note: Total == 0 implies n == 0, no need to check that separately
      ? null
      : Math.Log2(Total / (double)n);
  }

  internal void Increment(int index, int weight)
  {
    _distribution[index] += weight;
    Total += weight;
  }

}
