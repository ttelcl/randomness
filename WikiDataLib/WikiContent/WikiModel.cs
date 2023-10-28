/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MwParserFromScratch;
using MwParserFromScratch.Nodes;

namespace WikiDataLib.WikiContent;

/// <summary>
/// Wraps a wiki content AST
/// </summary>
public class WikiModel
{
  /// <summary>
  /// Create a new WikiModel
  /// </summary>
  public WikiModel(
    WikiModelParseSettings settings,
    string wikiSourceText)
  {
    WikiSourceText = wikiSourceText;
    Settings = settings;
    var parser = new WikitextParser();
    var parserOptions = settings.CreateParserOptions();
    parser.Options = parserOptions;
    Model = parser.Parse(wikiSourceText);
  }

  /// <summary>
  /// The original content
  /// </summary>
  public string WikiSourceText { get; }

  /// <summary>
  /// The parsed model AST
  /// </summary>
  public Wikitext Model { get; }

  /// <summary>
  /// Settings used in parsing and postprocessing
  /// </summary>
  public WikiModelParseSettings Settings { get; }

  /// <summary>
  /// Return the plaintext as an enumeration of lines. This method has a few tweaks
  /// on top of the underlying implementation
  /// </summary>
  /// <param name="stripTables">
  /// When true, drop tables. This is a bug workaround
  /// </param>
  public IEnumerable<string> PlaintextLines(bool stripTables)
  {
    // TODO: Category links
    var lines = Model.ToPlainText()
      .Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None) ?? Array.Empty<string>();
    var tableLevel = 0;
    var wasEmpty = true;
    foreach(var line in lines)
    {
      if(stripTables && line.StartsWith("{|"))
      {
        tableLevel++;
      }
      if(tableLevel == 0)
      {
        var line2 = line;
        var parts = line2.Split(':', 2);
        if(parts.Length > 1
          && parts[1].Length > 0
          && !Char.IsWhiteSpace(parts[1][0])
          && Settings.CategoryAliases.Contains(parts[0]))
        {
          line2 = String.Empty;
        }
        var isEmpty = String.IsNullOrEmpty(line2);
        if(!isEmpty || !wasEmpty) // avoid multiple empty lines
        {
          yield return line2;
        }
        wasEmpty = isEmpty;
      }
      if(line.StartsWith("|}"))
      {
        tableLevel--;
      }
    }
  }

  private readonly static string[] __dropFromStartOfWords = new string[] {
    "\"",
    "'",
    "(",
  };

  private readonly static string[] __dropFromEndOfWords = new string[] {
    "\"",
    "'",
    ")",
    ",",
    ".",
    ";",
    "!",
    "?",
  };

  /// <summary>
  /// Enumerate words in <paramref name="line"/> that meet the requirements set forth
  /// in <see cref="Settings"/>
  /// </summary>
  public IEnumerable<string> WordsFromLine(string line)
  {
    var words = line.Split();
    var alphabetSet = Settings.AlphabetSet;

    foreach(var word0 in words)
    {
      // This is not foolproof, but gets the job in most cases.
      var word = word0;
      foreach(var dropPrefix in __dropFromStartOfWords)
      {
        if(word.StartsWith(dropPrefix))
        {
          word = word[dropPrefix.Length..];
        }
      }
      foreach(var dropSuffix in __dropFromEndOfWords)
      {
        if(word.EndsWith(dropSuffix))
        {
          word = word[..^dropSuffix.Length];
        }
      }
      foreach(var dropSuffix in Settings.RemoveAtEnd)
      {
        if(word.EndsWith(dropSuffix))
        {
          word = word[..^dropSuffix.Length];
        }
      }
      if(word.Length > 0 && word.All(ch => alphabetSet.Contains(ch)))
      {
        yield return word;
      }
    }
  }

  /// <summary>
  /// Enumerate words in the full plaintext that meet the requirements set forth
  /// in <see cref="Settings"/>
  /// </summary>
  public IEnumerable<string> EnumerateWords()
  {
    return PlaintextLines(true).SelectMany(line => WordsFromLine(line));
  }

  /// <summary>
  /// Count all words in the plaintext that meet the requirements set forth
  /// in <see cref="Settings"/> and return the word count for each distinct word.
  /// </summary>
  public Dictionary<string, int> GatherWordCounts()
  {
    var map = new Dictionary<string, int>();
    foreach(var word in EnumerateWords())
    {
      if(!map.TryGetValue(word, out var count))
      {
        map[word] = 1;
      }
      else
      {
        map[word] = count + 1;
      }
    }
    return map;
  }
}
