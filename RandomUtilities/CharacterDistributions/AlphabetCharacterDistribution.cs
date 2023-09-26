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
  /// The total weight added
  /// </summary>
  public int Total { get; private set; }

  internal void Increment(int index, int weight)
  {
    _distribution[index] += weight;
    Total += weight;
  }

}
