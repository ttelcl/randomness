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

namespace RandomUtilities.WordLists;

/// <summary>
/// A word list provider that can load word lists from files in a directory
/// </summary>
public class DirectoryWordListProvider: WordListProvider
{
  /// <summary>
  /// Create a new DirectoryWordListProvider
  /// </summary>
  public DirectoryWordListProvider(string folder)
  {
    if(!Directory.Exists(folder))
    {
      throw new DirectoryNotFoundException(folder);
    }
    Folder = Path.GetFullPath(folder);
  }

  /// <summary>
  /// The directory serviced by this loader
  /// </summary>
  public string Folder { get; init; }

  /// <summary>
  /// Find a word list by its label. More precisely: load a word list file from a file
  /// in this loader's folder with a file name determined by the word list label as
  /// {label}.wordlist.txt. See remarks for the file format
  /// </summary>
  /// <remarks>
  /// <para>
  /// The file ({label}.wordlist.txt) is line-oriented. Each line may be one of the
  /// following:
  /// </para>
  /// <list type="bullet">
  /// <item>A comment, starting with '#'</item>
  /// <item>An empty line (ignored)</item>
  /// <item>A reference to another word list, to be inserted fully into this list: "{label}"
  /// (including the curly braces)</item>
  /// <item>A word in the list</item>
  /// </list>
  /// </remarks>
  public override WordList? FindList(string label, WordListProvider? referenceResolver = null)
  {
    if(!WordList.IsValidLabel(label))
    {
      throw new ArgumentOutOfRangeException(nameof(label), $"Not a valid label: '{label}'");
    }
    var shortFileName = $"{label}.wordlist.txt";
    var fileName = Path.Combine(Folder, shortFileName);
    var words = new List<string>();
    if(File.Exists(fileName))
    {
      var lines = File.ReadAllLines(fileName);
      foreach(var line in lines)
      {
        var l = line.Trim();
        if(!String.IsNullOrEmpty(l) && !l.StartsWith("#"))
        {
          if(l.StartsWith("{"))
          {
            if(!l.EndsWith("}"))
            {
              throw new InvalidOperationException(
                $"Invalid list reference '{l}' in word list {fileName}");
            }
            var reference = l[1..^1];
            if(!WordList.IsValidLabel(reference))
            {
              throw new InvalidOperationException(
                $"The reference '{l}' in '{fileName}' is not a valid word list name");
            }
            if(referenceResolver == null)
            {
              throw new InvalidOperationException(
                $"Cannot resolve reference '{l}' because no reference resolver was provided");
            }
            var refList = referenceResolver.FindList(reference, referenceResolver);
            if(refList == null)
            {
              throw new InvalidOperationException(
                $"Reference '{l}' in '{fileName}' was not found");
            }
            foreach(var word in refList.Words)
            {
              words.Add(word);
            }
          }
          else
          {
            words.Add(l);
          }
        }
      }
      if(words.Count == 0)
      {
        throw new InvalidOperationException(
          $"Word list '{label}' is present in '{Folder}', but is empty.");
      }
      return new WordList(label, words);
    }
    return null;
  }

  /// <summary>
  /// Reports the list names implied by *.wordlist.txt files in this folder
  /// </summary>
  public override IEnumerable<string> ListNames()
  {
    var di = new DirectoryInfo(Folder);
    var files = di.GetFiles("*.wordlist.txt");
    foreach( var file in files)
    {
      var parts = file.Name.Split('.');
      if(parts.Length == 3 && WordList.IsValidLabel(parts[0]))
      {
        yield return parts[0];
      }
    }
  }
}
