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

using RandomUtilities.ByteSources;

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
  /// Save the content of this object in *.fragments-?.csv format.
  /// Boundary characters are translated into '_' characters.
  /// </summary>
  public void SaveFragmentCsv(TextWriter w)
  {
    w.WriteLine("fragment,count");
    var buckets = _distributions.ToList();
    buckets.Sort((kvp1, kvp2) => StringComparer.Ordinal.Compare(kvp1.Key, kvp2.Key));
    foreach(var kvp in buckets)
    {
      var distribution = kvp.Value.Distribution;
      for(var i = 0; i<distribution.Count; i++)
      {
        var value = distribution[i];
        if(value > 0)
        {
          var letter = i==0 ? '_' : Alphabet.Characters[i];
          w.WriteLine($"{kvp.Key.Replace(Alphabet.Boundary, '_')}{letter},{value}");
        }
      }
    }
  }

  /// <summary>
  /// Read the content of a CSV file in *.fragments-?.csv style.
  /// The '_' in the file are changed to <see cref="Alphabet"/>.<see cref="Alphabet.Boundary"/>
  /// </summary>
  public void ReadFragmentCsv(TextReader r)
  {
    var header = r.ReadLine();
    if(header != "fragment,count")
    {
      throw new InvalidDataException(
        "Expecting the header line to be 'fragment,count'");
    }
    string? line;
    while((line = r.ReadLine()) != null)
    {
      if(!String.IsNullOrEmpty(line))
      {
        var parts = line.Split(',');
        if(parts.Length != 2)
        {
          throw new InvalidDataException(
            $"Unexpected data format in line '{line}'");
        }
        var fragment = parts[0].Replace('_', Alphabet.Boundary);
        if(fragment.Length != Order+1)
        {
          throw new InvalidOperationException(
            $"The fragment file is not compatible. File has order {fragment.Length-1} instead of {Order}");
        }
        var count = Int32.Parse(parts[1]);
        Add(fragment, count);
      }
    }
  }

  /// <summary>
  /// Enumerate all non-empty distributions
  /// </summary>
  /// <returns>
  /// The list of all non-empty distributions plus their fragment key
  /// </returns>
  public IEnumerable<KeyValuePair<string, AlphabetCharacterDistribution>> AllDistributions()
  {
    foreach(var kvp in _distributions)
    {
      if(kvp.Value != null && kvp.Value.Total > 0)
      {
        yield return kvp;
      }
    }
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

  /// <summary>
  /// Convert this object to the JSON serializable <see cref="LetterDistributionDto"/>
  /// </summary>
  public LetterDistributionDto ToLetterDistibutionDto()
  {
    var map = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
    foreach(var kvp in _distributions)
    {
      var prefix = kvp.Key;
      var acd = kvp.Value;
      var distribution = acd.Distribution;
      var inner = new Dictionary<string, int>(StringComparer.Ordinal);
      map[prefix] = inner;
      for(var i = 0; i<distribution.Count; i++)
      {
        var count = distribution[i];
        if(count > 0)
        {
          var letter = Alphabet.Characters[i];
          inner[letter.ToString()] = count;
        }
      }
    }
    return new LetterDistributionDto(Alphabet.Characters[1..], Order, map, Alphabet.Boundary);
  }
}
