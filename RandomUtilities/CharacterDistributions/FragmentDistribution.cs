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
/// Stores frequencies of characters given a given prefix string in a library of words.
/// </summary>
public class FragmentDistribution
{
  private readonly Dictionary<string, AlphabetCharacterDistribution> _distributions;

  /// <summary>
  /// Create a new FragmentDistribution
  /// </summary>
  public FragmentDistribution(Alphabet alphabet, int order)
  {
    Alphabet = alphabet;
    Order = order;
    WordBoundary = new String(Alphabet.Boundary, Order);
    _distributions = new Dictionary<string, AlphabetCharacterDistribution>(StringComparer.Ordinal);
  }

  /// <summary>
  /// The alphabet for this distribution
  /// </summary>
  public Alphabet Alphabet { get; init; }

  /// <summary>
  /// The virtual boundary existing before and after each word
  /// (<see cref="Order"/> repetitions of <see cref="Alphabet.Boundary"/>)
  /// </summary>
  public string WordBoundary { get; init; }

  /// <summary>
  /// The order of this distribution (the length of the prefix string)
  /// </summary>
  public int Order { get; init; }

  /// <summary>
  /// The total weight of all added samples
  /// </summary>
  public int Total { get; private set; }

  /// <summary>
  /// Find the distribution for the given prefix (returning null if not found)
  /// </summary>
  public AlphabetCharacterDistribution? FindDistribution(string prefix)
  {
    return _distributions.TryGetValue(prefix, out var distribution) ? distribution : null;
  }

  /// <summary>
  /// Add a new sample to this distribution
  /// </summary>
  /// <param name="prefix">
  /// The prefix string (of length <see cref="Order"/>)
  /// </param>
  /// <param name="suffix">
  /// The sample character to add
  /// </param>
  /// <param name="weight">
  /// The weight to add (default 1)
  /// </param>
  public void Add(string prefix, char suffix, int weight = 1)
  {
    var index = Alphabet.GetCharacterCode(suffix);
    if(index < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(suffix), 
        $"The character '{suffix}' is not in the target alphabet");
    }
    if(!Alphabet.IsRepresentable(prefix, true))
    {
      throw new ArgumentOutOfRangeException(nameof(prefix),
        $"The prefix '{prefix}' is not valid in the target alphabet");
    }
    if(prefix.Length != Order)
    {
      throw new ArgumentOutOfRangeException(nameof(prefix),
        $"The prefix '{prefix}' is not valid: expecting a length of {Order}");
    }
    if(!_distributions.TryGetValue(prefix, out var values))
    {
      values = new AlphabetCharacterDistribution(Alphabet);
      _distributions.Add(prefix, values);
    }
    values.Increment(index, weight);
    Total += weight;
  }

  /// <summary>
  /// Add the sample string fragment, after splitting it in a prefix and suffix
  /// </summary>
  /// <param name="sample">
  /// The sample fragment string, of length <see cref="Order"/> + 1.
  /// </param>
  /// <param name="weight">
  /// The weight to add (default 1)
  /// </param>
  public void Add(string sample, int weight = 1)
  {
    var prefix = sample[0..^1];
    var suffix = sample[^1];
    Add(prefix, suffix, weight);
  }

  /// <summary>
  /// Add all fragments for a word
  /// </summary>
  /// <param name="word">
  /// The word (without boundary padding; this method adds the padding)
  /// </param>
  /// <param name="weight">
  /// The weight to add for each fragment (default 1)
  /// </param>
  public void AddWord(string word, int weight = 1)
  {
    if(!Alphabet.IsRepresentable(word, false))
    {
      throw new ArgumentOutOfRangeException(
        nameof(word), $"Word '{word}' contains characters outside this distribution's alphabet");
    }
    var paddedWord = WordBoundary + word + WordBoundary;
    for(var i = 0; i < paddedWord.Length - Order; i++)
    {
      var prefix = paddedWord.Substring(i, Order);
      var suffix = paddedWord[i+Order];
      Add(prefix, suffix, weight);
    }
  }


}
