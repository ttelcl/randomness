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
/// Maps prefixes to letter distributions
/// </summary>
public class LetterDistribution
{
  private readonly Dictionary<string, LetterCountCell[]> _distributionMap;

  /// <summary>
  /// Create a new LetterDistribution
  /// </summary>
  public LetterDistribution(
    Alphabet alphabet,
    int order,
    Dictionary<string, Dictionary<string, int>> countsMap)
  {
    _distributionMap = new Dictionary<string, LetterCountCell[]>(StringComparer.Ordinal);
    Alphabet = alphabet;
    Order = order;
    foreach(var kvp1 in countsMap)
    {
      var prefix = kvp1.Key;
      if(prefix.Length != order)
      {
        throw new InvalidDataException(
          $"Inconsistent prefix length. Got '{prefix}' but expected a length {order} string");
      }
      if(!Alphabet.IsRepresentable(kvp1.Key, true))
      {
        throw new InvalidOperationException(
          $"The prefix '{prefix}' is not valid for use with this alphabet");
      }
      var cumulative = 0;
      var cells = new List<LetterCountCell>();
      var letterCountPairs = kvp1.Value.ToList();
      var total = letterCountPairs.Sum(kvp => kvp.Value);
      foreach(var kvp2 in letterCountPairs)
      {
        if(kvp2.Value > 0)
        {
          var key = kvp2.Key.Length == 1 ? kvp2.Key[0] : throw new InvalidDataException(
            $"Expecting keys to be single characters but got '{kvp2.Key}'");
          if(!Alphabet.IsRepresentable(kvp2.Key, true))
          {
            throw new InvalidOperationException(
              $"The letter '{key}' is not valid for use with this alphabet");
          }
          var cell = new LetterCountCell(key, kvp2.Value, prefix, cumulative, total);
          cumulative = cell.Cumulative;
          cells.Add(cell);
        }
      }
      _distributionMap[prefix] = cells.ToArray();
    }
  }

  /// <summary>
  /// Create a <see cref="LetterDistribution"/> from its serializable form
  /// </summary>
  public static LetterDistribution FromDto(LetterDistributionDto dto)
  {
    return new LetterDistribution(dto.AlphabetObject, dto.Order, dto.PrefixCountsMap);
  }

  /// <summary>
  /// The set of valid letters for this distribution plus a boundary marker
  /// </summary>
  public Alphabet Alphabet { get; init; }

  /// <summary>
  /// The prefix (context) length used
  /// </summary>
  public int Order { get; init; }

  /// <summary>
  /// Get the seed string (<see cref="Order"/> repetitions of the boundary character)
  /// </summary>
  public string Seed { get => new(Alphabet.Boundary, Order); }

  /// <summary>
  /// Generate a randon word using this distribution and the
  /// random bit source
  /// </summary>
  public string RandomWord(BitSource bitSource, out double surprisal)
  {
    var context = Seed;
    var sb = new StringBuilder();
    LetterCountCell cell;
    surprisal = 0.0;
    do
    {
      cell = RandomCell(bitSource, context);
      context = cell.NextPrefix;
      surprisal += cell.Surprisal;
      if(cell.Letter == Alphabet.Boundary)
      {
        return sb.ToString();
      }
      sb.Append(cell.Letter);
    } while(true);
  }

  /// <summary>
  /// Randomly pick the next cell given the specified context
  /// </summary>
  /// <param name="bitSource">
  /// The source of randomness
  /// </param>
  /// <param name="context">
  /// The context string
  /// </param>
  /// <returns>
  /// A randomly selected cell amongst the list of cells for the context
  /// </returns>
  public LetterCountCell RandomCell(BitSource bitSource, string context)
  {
    if(!_distributionMap.TryGetValue(context, out var cells))
    {
      throw new InvalidOperationException(
        $"'{context}' is not a valid context string for this distribution");
    }
    if(cells.Length == 1)
    {
      return cells[0];
    }
    var max = cells[^1].Cumulative;
    var pick = bitSource.RandomInteger(max-1);
    // Console.WriteLine($"  Pick: {pick} / {max}");
    foreach(var cell in cells)
    {
      if(cell.Cumulative > pick)
      {
        return cell;
      }
    }
    throw new InvalidOperationException(
      $"Internal error: max {max} <= pick {pick} ???");
  }

  /// <summary>
  /// Convert this object into a serializable form
  /// </summary>
  public LetterDistributionDto ToDto()
  {
    var map = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
    foreach(var kvp in _distributionMap)
    {
      var inner = new Dictionary<string, int>(StringComparer.Ordinal);
      map[kvp.Key] = inner;
      foreach(var lcc in kvp.Value)
      {
        inner[lcc.Letter.ToString()] = lcc.Count;
      }
    }
    return new LetterDistributionDto(Alphabet.Characters, Order, map, Alphabet.Boundary);
  }
}
