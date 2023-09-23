/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RandomUtilities.WordLists;

/// <summary>
/// A named list of words (or other strings)
/// </summary>
public class WordList
{
  private static Regex __wordlistLabelRegex = new Regex(
    @"^[a-z][a-z0-9]*([-_][a-z0-9]+)*$", RegexOptions.IgnoreCase);

  /// <summary>
  /// Create a new WordList
  /// </summary>
  public WordList(string label, IEnumerable<string> words)
  {
    Label = label;
    var list = words.ToList();
    Words = list.AsReadOnly();
    if(!IsValidLabel(label))
    {
      throw new ArgumentOutOfRangeException(nameof(label), "Not a valid word list name");
    }
  }

  /// <summary>
  /// Test if the label matches the requirements to be a valid
  /// word list labal
  /// </summary>
  public static bool IsValidLabel(string label)
  {
    return __wordlistLabelRegex.IsMatch(label);
  }

  /// <summary>
  /// The label identifying this list
  /// </summary>
  public string Label { get; init; }

  /// <summary>
  /// The words in the list
  /// </summary>
  public IReadOnlyList<string> Words { get; init; }
}
