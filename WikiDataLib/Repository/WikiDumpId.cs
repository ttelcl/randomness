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
/// Identifies one wiki dump set
/// </summary>
public class WikiDumpId
{
  /// <summary>
  /// Create a new WikiDumpId
  /// </summary>
  public WikiDumpId(
    string wikiTag, string dumpTag)
  {
    WikiTag = wikiTag;
    DumpTag = dumpTag;
    if(!WikiRepo.IsValidWikiName(wikiTag))
    {
      throw new ArgumentException(
        $"'{wikiTag}' is not a valid wikitag");
    }
    if(!WikiRepo.IsValidWikiDumpTag(dumpTag))
    {
      throw new ArgumentException(
        $"'{dumpTag}' is not a valid wiki dump tag");
    }
  }

  /// <summary>
  /// Try to derive a WikiDumpId from a file name. Only the file part
  /// of the path is considered.
  /// </summary>
  /// <param name="filePath">
  /// The path to the file. Directory parts are allowed but ignored
  /// </param>
  /// <returns>
  /// A new WikiDumpId if the file name started with the format '{wikitag}-{yyyyMMdd}'
  /// </returns>
  public static WikiDumpId? TryFromFile(string filePath)
  {
    var fileName = Path.GetFileName(filePath);
    var parts = fileName.Split('-');
    if(parts.Length >= 2 && WikiRepo.IsValidWikiName(parts[0]) && WikiRepo.IsValidWikiDumpTag(parts[1]))
    {
      return new WikiDumpId(parts[0], parts[1]);
    }
    return null;
  }

  /// <summary>
  /// The tag identifying the wiki (a string containing only ascii letters, typically
  /// a two-letter country code followed by "wiki")
  /// </summary>
  public string WikiTag { get; init; }

  /// <summary>
  /// The tag identifying the dump. This takes the shape of a date in "yyyyMMdd" form.
  /// </summary>
  public string DumpTag { get; init; }

  /// <summary>
  /// Format this WikiDumpId in the form {wikitag}-{dumptag} (e.g. "enwiki-20230920")
  /// </summary>
  public override string ToString()
  {
    return $"{WikiTag}-{DumpTag}";
  }
}
