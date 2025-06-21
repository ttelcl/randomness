/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomUtilities.WordCounts;

/// <summary>
/// Description of WordCountMap
/// </summary>
public class WordCountMap
{
  private readonly Dictionary<string, int> _wordCounts;

  /// <summary>
  /// Create a new WordCountMap
  /// </summary>
  public WordCountMap()
  {
    _wordCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Adds the specified word (at the given weight / count)
  /// </summary>
  public void Add(string word, int count = 1)
  {
    if(_wordCounts.TryGetValue(word, out var oldValue))
    {
      _wordCounts[word] = count + oldValue;
    }
    else
    {
      _wordCounts[word] = count;
    }
  }

  /// <summary>
  /// Add the contents of a *.words.csv file to this map,
  /// optionally ignoring the weights and optionally skipping short words
  /// </summary>
  /// <param name="fileName">
  /// The name of the file to load
  /// </param>
  /// <param name="weighted">
  /// If true (default), include the weights (counts). If false, treat
  /// all entries as if they had count "1".
  /// </param>
  /// <param name="minLength">
  /// The minimum word length (default 1). Any words shorter than this
  /// are skipped.
  /// </param>
  /// <exception cref="FileNotFoundException"></exception>
  public void AddFile(string fileName, bool weighted = true, int minLength = 1)
  {
    if(!File.Exists(fileName))
    {
      throw new FileNotFoundException("File not found", fileName);
    }
    foreach(var line in File.ReadLines(fileName).Skip(1))
    {
      if(String.IsNullOrWhiteSpace(line))
      {
        continue;
      }
      var parts = line.Split(',');
      var word = parts[0];
      if(word.Length < minLength)
      {
        continue;
      }
      var count =
        weighted && parts.Length >= 2
        ? Int32.Parse(parts[1])
        : 1;
      Add(word, count);
    }
  }

  /// <summary>
  /// Reset all word counts to 1
  /// </summary>
  public void ResetCounts()
  {
    foreach(var word in _wordCounts.Keys)
    {
      _wordCounts[word] = 1;
    }
  }

  /// <summary>
  /// Return the word count for the given word (0 if not found)
  /// </summary>
  public int this[string word] => 
    _wordCounts.TryGetValue(word, out var value) ? value : 0;

  /// <summary>
  /// Save this map to a *.words.csv file
  /// </summary>
  /// <param name="fileName">
  /// The name of the file to save to (will be overwritten)
  /// </param>
  public void SaveFile(string fileName)
  {
    fileName = Path.GetFullPath(fileName);
    var entries =
      from kvp in _wordCounts
      orderby kvp.Value descending, kvp.Key
      select kvp;
    using(var w = File.CreateText(fileName))
    {
      w.WriteLine("word,count");
      foreach(var entry in entries)
      {
        w.WriteLine($"{entry.Key},{entry.Value}");
      }
    }
  }

}
