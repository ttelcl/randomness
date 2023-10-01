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

using Newtonsoft.Json;

namespace RandomUtilities.CharacterDistributions;

/// <summary>
/// DTO for JSON serialization of <see cref="LetterDistribution"/>
/// </summary>
public class LetterDistributionDto
{
  private readonly Dictionary<string, Dictionary<string, int>> _prefixCountsMap;
  private readonly Alphabet _alphabet;

  /// <summary>
  /// Create a new LetterDistributionDto
  /// </summary>
  [JsonConstructor]
  public LetterDistributionDto(
    string alphabet,
    int order,
    IDictionary<string, Dictionary<string, int>> prefixCountsMap,
    char boundary = '_')
  {
    _alphabet = new Alphabet(alphabet, boundary);
    Order = order;
    _prefixCountsMap = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
    var prefixCounts = prefixCountsMap.OrderBy(kvp => kvp.Key).ToList();
    foreach(var kvpOuter in prefixCounts)
    {
      if(!_alphabet.IsRepresentable(kvpOuter.Key, true))
      {
        throw new InvalidDataException(
          $"Invalid prefix '{kvpOuter.Key}' in data");
      }
      var inner = new Dictionary<string, int>(StringComparer.Ordinal);
      _prefixCountsMap[kvpOuter.Key] = inner;
      foreach(var kvpInner in kvpOuter.Value)
      {
        if(kvpInner.Value > 0)
        {
          if(kvpInner.Key.Length != 1)
          {
            throw new InvalidDataException(
              $"Invalid key '{kvpInner.Key}' in data; expecting one single letter (at prefix '{kvpOuter.Key}')");
          }
          if(!_alphabet.IsRepresentable(kvpInner.Key, true))
          {
            throw new InvalidDataException(
              $"Invalid letter '{kvpInner.Key}' in data (at prefix '{kvpOuter.Key}')");
          }
          inner[kvpInner.Key] = kvpInner.Value;
        }
      }
    }
  }

  /// <summary>
  /// Deserialize a <see cref="LetterDistributionDto"/> from JSON
  /// </summary>
  public static LetterDistributionDto FromJson(string json)
  {
    return JsonConvert.DeserializeObject<LetterDistributionDto>(json)!;
  }

  /// <summary>
  /// The set of valid letters
  /// </summary>
  [JsonProperty("alphabet")]
  public string Alphabet { get => _alphabet.Characters[1..]; }

  /// <summary>
  /// The boundary marker character
  /// </summary>
  [JsonProperty("boundary")]
  public char Boundary { get => _alphabet.Boundary; }

  /// <summary>
  /// The order (length of the context / prefix; length of the
  /// keys in the top level dictionary)
  /// </summary>
  [JsonProperty("order")]
  public int Order { get; init; }

  /// <summary>
  /// Double mapping: a map from prefix strings to a map of letters to counts
  /// for that prefix.
  /// </summary>
  [JsonProperty("prefixCountsMap")]
  public Dictionary<string, Dictionary<string, int>> PrefixCountsMap { get => _prefixCountsMap; }

  /// <summary>
  /// The alphabet object used to back the <see cref="Alphabet"/> and <see cref="Boundary"/> properties
  /// </summary>
  [JsonIgnore]
  public Alphabet AlphabetObject { get => _alphabet; }

  /// <summary>
  /// Serialize this DTO to JSON
  /// </summary>
  public string ToJson(bool indent)
  {
    return JsonConvert.SerializeObject(this, indent ? Formatting.Indented : Formatting.None);
  }
}
