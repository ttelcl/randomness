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
/// A set of characters that words are made from. An alphabet defines a mapping
/// between integer character codes and the characters in the set.
/// </summary>
public class Alphabet
{
  private readonly Dictionary<char, int> _characterCodes;

  /// <summary>
  /// Create a new Alphabet
  /// </summary>
  /// <param name="characters">
  /// The set of characters used in words.
  /// </param>
  /// <param name="boundary">
  /// The character used to mark the start and end of words. By default the space character.
  /// This character must not be in the set of characters.
  /// </param>
  public Alphabet(
    string characters,
    char boundary = ' ')
  {
    var carr = characters.ToCharArray();
    Array.Sort(carr);
    Characters = new String(boundary, 1) + new String(carr);
    Boundary = boundary;
    _characterCodes = new Dictionary<char, int>();
    for(int i = 0; i < Characters.Length; i++)
    {
      _characterCodes[Characters[i]] = i;
    }
  }

  /// <summary>
  /// The set of characters, including the boundary marker character as first character.
  /// </summary>
  public string Characters { get; init; }

  /// <summary>
  /// The number of characters in <see cref="Characters"/> (including the boundary character)
  /// </summary>
  public int CharacterCount => Characters.Length;

  /// <summary>
  /// The boundary marker character
  /// </summary>
  public char Boundary { get; init; }

  /// <summary>
  /// Get the character code for <paramref name="ch"/> in this alphabet
  /// (such that <see cref="Characters"/>[code] = <paramref name="ch"/>)
  /// Returns 0 for the boundary character. Returns -1 if not found.
  /// </summary>
  /// <param name="ch">
  /// The character to get the code for
  /// </param>
  /// <returns>
  /// The index of the character in <see cref="Characters"/> (0 being the
  /// boundary character), or -1 if not found.
  /// </returns>
  public int GetCharacterCode(char ch)
  {
    return _characterCodes.TryGetValue(ch, out var code) ? code : -1;
  }

  /// <summary>
  /// Returns true if all characters in the sample string are present in this
  /// alphabet (optionally including or excluding the boundary character)
  /// </summary>
  /// <param name="sample">
  /// The sample word to test
  /// </param>
  /// <param name="includeBoundary">
  /// When true, the boundary character is considered valid.
  /// When false, the boundary character is considered invalid.
  /// </param>
  public bool IsRepresentable(string sample, bool includeBoundary)
  {
    var minIndex = includeBoundary ? 0 : 1;
    return sample.All(ch => GetCharacterCode(ch) >= minIndex);
  }

  /// <summary>
  /// Create a new buffer for recording a distribution of fragments of words
  /// whose letters are all in this alphabet. Use its AddWord and Add methods
  /// to add words or fragments.
  /// </summary>
  /// <param name="order">
  /// The length of the word fragment prefixes (i.e. the length of the fragments minus 1).
  /// Passing 0 would record a letter distribution directly, 1 would record
  /// distributions of the final letter of a letter pair, etc.
  /// </param>
  /// <returns>
  /// A new empty <see cref="FragmentDistribution"/>, ready to record new
  /// words and word fragments.
  /// </returns>
  public FragmentDistribution CreateDistributionRecorder(int order) 
  {
    return new FragmentDistribution(this, order);
  }
}
