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
      var total = 0;
      var cells = new List<LetterCountCell>();
      foreach(var kvp2 in kvp1.Value)
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
          var cell = new LetterCountCell(key, kvp2.Value, prefix, total);
          total = cell.Cumulative;
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
