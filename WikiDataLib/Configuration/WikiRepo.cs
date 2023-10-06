/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WikiDataLib.Configuration;

/// <summary>
/// A repository directory comtaining wikipedia dump files (and maybe analysis results)
/// </summary>
public class WikiRepo
{
  private readonly HashSet<string> _wikinames;

  /// <summary>
  /// Create a new WikiRepo
  /// </summary>
  public WikiRepo(string folder)
  {
    _wikinames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
    if(!Directory.Exists(folder))
    {
      throw new DirectoryNotFoundException(
        $"Directory not found: {folder}");
    }
    Folder = Path.GetFullPath(folder);
    WikiNames(true);
  }

  /// <summary>
  /// The root folder for the repository
  /// </summary>
  public string Folder { get; init; }

  /// <summary>
  /// Return a set of the names of the wikis tracked in this repo.
  /// A tracked wiki is defined by the existence of an empty file
  /// named {name}.iswiki in the repo folder and a {name} subfolder
  /// (the subfolder is created by this method if missing).
  /// This list is cached upon first call, unless <paramref name="reload"/>
  /// is true, in which case the cache is reloaded. 
  /// </summary>
  /// <param name="reload">
  /// If true: reload the cache
  /// </param>
  /// <returns>
  /// A set of the names of currently tracked wikis. This set is "hot" in the sense
  /// that it adapts if wikis are added or removed.
  /// </returns>
  public IReadOnlySet<string> WikiNames(bool reload)
  {
    var wikinames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
    if(reload)
    {
      var tagfiles = Directory.GetFiles(Folder, "*.iswiki");
      foreach(var tagfile in tagfiles)
      {
        var tag = Path.GetFileNameWithoutExtension(tagfile);
        if(IsValidWikiName(tag))
        {
          wikinames.Add(tag);
          var wikifolder = Path.Combine(Folder, tag);
          if(!Directory.Exists(wikifolder))
          {
            Directory.CreateDirectory(wikifolder);
          }
        }
      }
      var missing = _wikinames.Except(wikinames);
      _wikinames.UnionWith(wikinames);
      _wikinames.ExceptWith(missing);
    }
    return _wikinames;
  }

  /// <summary>
  /// Add a wiki folder for the specified wiki tag. If not already done
  /// so, this will create the tag file, the folder and add the tag to the
  /// list of tracked wikis (hot-updating the set returned by <see cref="WikiNames(bool)"/>)
  /// </summary>
  /// <param name="wikitag">
  /// The tag to add, consisting of lower case ascii characters only
  /// </param>
  public void AddWiki(string wikitag)
  {
    if(!IsValidWikiName(wikitag))
    {
      throw new ArgumentOutOfRangeException(nameof(wikitag),
        $"{wikitag} is not a valid tag");
    }
    var tagfile = Path.Combine(Folder, wikitag + ".iswiki");
    var folder = Path.Combine(Folder, wikitag);
    if(!Directory.Exists(folder))
    {
      Directory.CreateDirectory(folder);
    }
    if(!File.Exists(tagfile))
    {
      File.WriteAllBytes(tagfile, Array.Empty<byte>());
    }
    _wikinames.Add(wikitag);
  }

  /// <summary>
  /// Returns true for strings that are considered valid for use as wiki tag name.
  /// </summary>
  public static bool IsValidWikiName(string wikiname)
  {
    return Regex.IsMatch(wikiname, @"^[a-z]+$");
  }
}
