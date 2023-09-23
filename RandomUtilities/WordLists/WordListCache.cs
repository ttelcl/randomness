/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RandomUtilities.WordLists;

/// <summary>
/// Cache for word lists and dispatcher for searches
/// </summary>
public class WordListCache: WordListProvider
{
  private readonly List<WordListProvider> _providers;
  private readonly Dictionary<string, WordList?> _wordLists;
  private readonly HashSet<string> _currentlyResolving;

  /// <summary>
  /// Create a new WordListCache
  /// </summary>
  public WordListCache()
  {
    _providers = new List<WordListProvider>();
    _wordLists = new Dictionary<string, WordList?>(StringComparer.InvariantCultureIgnoreCase);
    _currentlyResolving = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
  }

  /// <summary>
  /// Add a child provider. 
  /// Returns this instance itself for fluent configuration.
  /// </summary>
  public WordListCache AddChild(WordListProvider child)
  {
    _providers.Add(child);
    return this;
  }

  /// <summary>
  /// Add a new <see cref="DirectoryWordListProvider"/> as child
  /// Returns this instance itself for fluent configuration.
  /// </summary>
  /// <param name="folder">
  /// The folder to look for word list files
  /// </param>
  public WordListCache AddFolderChild(string folder)
  {
    return AddChild(new DirectoryWordListProvider(folder));
  }

  /// <summary>
  /// Add a new  <see cref="DirectoryWordListProvider"/> as child
  /// that looks in the entrypoint folder (or a path relative to it) for word lists
  /// </summary>
  /// <param name="relativePath">
  /// If provided: the path relative to the entry assembly folder to add
  /// </param>
  public WordListCache AddApplicationFolder(string relativePath = "")
  {
    var asm = Assembly.GetEntryAssembly();
    return asm != null
      ? AddFolderChild(Path.Combine(asm.Location, relativePath ?? String.Empty))
      : throw new InvalidOperationException(
        "Cannot add application folder because it is not available");
  }

  /// <summary>
  /// Add a word list resolver for the current folder
  /// </summary>
  /// <returns>
  /// This instance itself, for fluent configuration
  /// </returns>
  public WordListCache AddCurrentFolder()
  {
    return AddFolderChild(Environment.CurrentDirectory);
  }

  /// <summary>
  /// Add a prebuilt word list
  /// </summary>
  /// <param name="wordList">
  /// The word list to add
  /// </param>
  public void AddWordList(WordList wordList)
  {
    _wordLists[wordList.Label] = wordList;
  }

  /// <summary>
  /// Find a word list in this cache, or try to load it into this cache
  /// from the first child word list provider that can provide it.
  /// </summary>
  /// <param name="label">
  /// The label of the word list to provide
  /// </param>
  /// <param name="referenceResolver">
  /// The resolver passed to child word list providers to resolve references.
  /// To be effective this should be this <see cref="WordListCache"/> itself.
  /// If this is null, this <see cref="WordListCache"/> is passed to children.
  /// </param>
  /// <returns>
  /// The word list if found, or null if not found.
  /// </returns>
  public override WordList? FindList(string label, WordListProvider? referenceResolver = null)
  {
    if(_currentlyResolving.Contains(label))
    {
      throw new InvalidOperationException(
        $"Word list resolution loop detected at word list '{label}'");
    }
    if(_wordLists.TryGetValue(label, out var wordList))
    {
      return wordList; // even if null!
    }
    _currentlyResolving.Add(label);
    try
    {
      foreach(var child in _providers)
      {
        var list = child.FindList(label, referenceResolver ?? this);
        if(list != null)
        {
          _wordLists.Add(list.Label, list);
          return list;
        }
      }
      _wordLists.Add(label, null); // prevent looking in vain again
      return null;
    }
    finally
    {
      _currentlyResolving.Remove(label);
    }
  }

  /// <summary>
  /// Gethers and reports names of lists that are already cached
  /// and names of lists that the children report as available
  /// </summary>
  public override IEnumerable<string> ListNames()
  {
    var names = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
    foreach(var wordList in _wordLists.Values)
    {
      if(wordList != null)
      {
        names.Add(wordList.Label);
      }
    }
    foreach(var child in _providers)
    {
      foreach (var name in child.ListNames())
      {
        names.Add(name);
      }
    }
    return names;
  }
}
