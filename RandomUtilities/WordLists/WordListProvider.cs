/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomUtilities.WordLists;

/// <summary>
/// A facility that can return a list of words (or other strings) identified by a name
/// </summary>
public abstract class WordListProvider
{
  /// <summary>
  /// Create a new WordListProvider
  /// </summary>
  protected WordListProvider()
  {
  }

  /// <summary>
  /// Find a list by its label
  /// </summary>
  /// <param name="label">
  /// The name of the list to return
  /// </param>
  /// <param name="referenceResolver">
  /// If not null: a WordListProvider that can resolve referenced word lists
  /// </param>
  /// <returns>
  /// The list, if found, or null if not
  /// </returns>
  public abstract WordList? FindList(string label, WordListProvider? referenceResolver = null);

  /// <summary>
  /// Enumerate potentially available word list names. Implementations
  /// are free to choose which names to report, if any at all.
  /// </summary>
  public abstract IEnumerable<string> ListNames();
}
