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

using WikiDataLib.Configuration;

namespace WikiDataLib.Repository;

/// <summary>
/// A repository directory comtaining wikipedia dump files (and maybe analysis results)
/// </summary>
public class WikiRepo
{
  private readonly Dictionary<string, Wiki> _wikis;

  /// <summary>
  /// Create a new WikiRepo instance on the specified folder
  /// </summary>
  public WikiRepo(string folder)
  {
    _wikis = new Dictionary<string, Wiki>(StringComparer.InvariantCultureIgnoreCase);
    if(!Directory.Exists(folder))
    {
      throw new DirectoryNotFoundException(
        $"Directory not found: {folder}");
    }
    Folder = Path.GetFullPath(folder);
    ImportFolder = Path.Combine(Folder, "import-buffer");
    if(!Directory.Exists(ImportFolder))
    {
      Directory.CreateDirectory(ImportFolder);
    }
    SynchronizeWikis(false);
  }

  /// <summary>
  /// Create a new WikiRepo instance on the repository folder given in
  /// the specified <see cref="MachineWikiConfiguration"/>.
  /// </summary>
  public WikiRepo(MachineWikiConfiguration cfg)
    : this(cfg.RepoFolder)
  {
  }

  /// <summary>
  /// Create a new WikiRepo instance on the repository folder given in
  /// the system default <see cref="MachineWikiConfiguration"/>.
  /// </summary>
  public WikiRepo()
    : this(MachineWikiConfiguration.LoadConfiguration())
  {
  }

  /// <summary>
  /// The root folder for the repository
  /// </summary>
  public string Folder { get; init; }

  /// <summary>
  /// The folder to put files to be imported
  /// </summary>
  public string ImportFolder { get; init; }

  /// <summary>
  /// Synchronize the wiki instances tracked in this object to the
  /// state of the folder on disk
  /// </summary>
  /// <param name="recurse">
  /// Also synchronize the content of each pre-existing child wiki
  /// (newly added child wikis are always synchronized even when this is false)
  /// </param>
  public void SynchronizeWikis(bool recurse)
  {
    var oldnames = new HashSet<string>(_wikis.Keys, StringComparer.InvariantCultureIgnoreCase);
    var wikinames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
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
    var missing = oldnames.Except(wikinames);
    foreach(var missingName in missing)
    {
      _wikis.Remove(missingName);
    }
    foreach(var name in wikinames)
    {
      if(!_wikis.TryGetValue(name, out var wiki))
      {
        wiki = new Wiki(this, name);
        _wikis[wiki.WikiTag] = wiki;
      }
      else if(recurse)
      {
        wiki.SynchronizeDumps(recurse);
      }
    }
  }

  /// <summary>
  /// Return a collection of the names of the wikis tracked in this repo.
  /// </summary>
  public IReadOnlyCollection<string> WikiNames { get => _wikis.Keys; }

  /// <summary>
  /// Return a collection of the wikis tracked in this repo.
  /// </summary>
  public IReadOnlyCollection<Wiki> Wikis { get => _wikis.Values; }

  /// <summary>
  /// Find a wiki instance in this repository (returning null if not found)
  /// </summary>
  /// <param name="wikitag">
  /// The name of the wiki to find
  /// </param>
  /// <returns>
  /// The <see cref="Wiki"/> instance if found, or null if not found
  /// </returns>
  public Wiki? FindWiki(string wikitag)
  {
    return _wikis.TryGetValue(wikitag, out var wiki) ? wiki : null;
  }

  /// <summary>
  /// Add a wiki folder for the specified wiki tag. If not already done
  /// so, this will create the tag file, the folder and add the wiki to the
  /// list of tracked wikis
  /// </summary>
  /// <param name="wikitag">
  /// The tag to add, consisting of lower case ascii characters only
  /// </param>
  public Wiki AddWiki(string wikitag)
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
    if(!_wikis.TryGetValue(wikitag, out var wiki))
    {
      wiki = new Wiki(this, wikitag);
      _wikis[wiki.WikiTag] = wiki;
    }
    return wiki;
  }

  /// <summary>
  /// Return a WikiDump instance, creating missing folders if necessary
  /// </summary>
  public WikiDump GetDumpFolder(WikiDumpId wdi)
  {
    var wiki = FindWiki(wdi.WikiTag);
    if(wiki == null)
    {
      wiki = AddWiki(wdi.WikiTag);
    }
    return wiki.GetDump(wdi.DumpTag);
  }

  /// <summary>
  /// Return a WikiDump instance, returning null if the folders are missing
  /// </summary>
  public WikiDump? FindDumpFolder(WikiDumpId wdi)
  {
    var wiki = FindWiki(wdi.WikiTag);
    if(wiki == null)
    {
      return null;
    }
    return wiki.FindDump(wdi.DumpTag);
  }

  /// <summary>
  /// Enumerate the files (of supported types) pending import
  /// </summary>
  public IEnumerable<string> PendingFiles()
  {
    return SupportedSuffixes.SelectMany(
      suffix => Directory.EnumerateFiles(ImportFolder, "*-" + suffix));
  }

  /// <summary>
  /// Returns true for strings that are considered valid for use as wiki tag name.
  /// </summary>
  public static bool IsValidWikiName(string wikiname)
  {
    return Regex.IsMatch(wikiname, @"^[a-z]+$");
  }

  /// <summary>
  /// Returns true for strings that look like valid dump tags (dates in yyyyMMdd form)
  /// </summary>
  public static bool IsValidWikiDumpTag(string dumpTag)
  {
    return Regex.IsMatch(dumpTag, @"^2[0-9]{3}(0[1-9]|1[0-2])(0[1-9]|[12][0-9]|3[01])$");
  }

  /// <summary>
  /// Ends of filenames supported for import. These are the part of the filename
  /// after the second '-' (after the wiki tag and dump tag)
  /// </summary>
  public static IReadOnlySet<string> SupportedSuffixes { get; } =
    new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { 
      "pages-articles-multistream.xml.bz2",
      "pages-articles-multistream-index.txt.bz2"
    };

  /// <summary>
  /// Return the WikiDumpId for the file if it is suitable for import
  /// (null otherwise). This check goes deeper than <see cref="WikiDumpId.TryFromFile(string)"/>
  /// </summary>
  public static WikiDumpId? ImportId(string fileName)
  {
    if(File.Exists(fileName))
    {
      var shortName = Path.GetFileName(fileName);
      var parts = shortName.Split('-', 3);
      if(parts.Length == 3 && IsValidWikiName(parts[0]) && IsValidWikiDumpTag(parts[1]) && SupportedSuffixes.Contains(parts[2]))
      {
        return new WikiDumpId(parts[0], parts[1]);
      }
    }
    return null;
  }
}
