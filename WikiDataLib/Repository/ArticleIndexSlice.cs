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

namespace WikiDataLib.Repository;

/// <summary>
/// Represents a slice of an article index that is persisted.
/// This is a helper class mostly related to file names, not so
/// much the index content.
/// </summary>
public class ArticleIndexSlice
{
  /// <summary>
  /// Create a new ArticleIndexSlice
  /// </summary>
  public ArticleIndexSlice(
    string indexFolderName,
    WikiDumpId wikiId,
    int startIndex,
    int endIndex)
  {
    if(startIndex > endIndex)
    {
      throw new ArgumentException(
        $"Expecting endindex >= startIndex");
    }
    WikiId = wikiId;
    StartIndex = startIndex;
    EndIndex = endIndex;
    var shortName = $"{wikiId}.i-{startIndex}-{endIndex}.partidx.csv";
    FileName = Path.Combine(indexFolderName, shortName);
  }

  /// <summary>
  /// Parse a partial article index file name to a <see cref="ArticleIndexSlice"/>
  /// </summary>
  /// <param name="fileName">
  /// The full path to the file name to parse
  /// </param>
  public static ArticleIndexSlice ParseFileName(string fileName)
  {
    var fullName = Path.GetFullPath(fileName);
    var folder = Path.GetDirectoryName(fullName);
    fileName = Path.GetFileName(fullName);
    var parts = fileName.Split('.');
    if(parts.Length != 4)
    {
      throw new ArgumentOutOfRangeException(nameof(fileName),
        "Expecting shape <wikid>.i<start>-<end>.partidx.csv (incorrect segments)");
    }
    var dumpId = WikiDumpId.Parse(parts[0]);
    var rangeParts = parts[1].Split("-");
    if(rangeParts.Length != 3)
    {
      throw new ArgumentOutOfRangeException(nameof(fileName),
        "Expecting shape <wikid>.i<start>-<end>.partidx.csv (incorrect start-end segment)");
    }
    if(rangeParts[0] != "i")
    {
      throw new ArgumentOutOfRangeException(nameof(fileName),
        "Expecting shape <wikid>.i<start>-<end>.partidx.csv (missing 'i' at start-end segment)");
    }
    var startIndex = Int32.Parse(rangeParts[1]);
    var endIndex = Int32.Parse(rangeParts[2]);
    if(parts[2] != "partidx" || parts[3] != "csv")
    {
      throw new ArgumentOutOfRangeException(nameof(fileName),
        "Expecting shape <wikid>.i<start>-<end>.partidx.csv (not ending with .partidx.csv)");
    }
    return new ArticleIndexSlice(folder!, dumpId, startIndex, endIndex);
  }

  /// <summary>
  /// The wiki instance this index applies to
  /// </summary>
  public WikiDumpId WikiId { get; init; }

  /// <summary>
  /// The full name of the index file. This may or may not yet exist.
  /// </summary>
  public string FileName { get; init; }
  
  /// <summary>
  /// The index of the first substream in this index file
  /// </summary>
  public int StartIndex { get; init; }

  /// <summary>
  /// The index of the last substream in this index file. This may be
  /// equal to <see cref="StartIndex"/> (or larger)
  /// </summary>
  public int EndIndex { get; init; }

}
