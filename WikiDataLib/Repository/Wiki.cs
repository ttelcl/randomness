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
/// Represents a folder containing data from one wiki
/// </summary>
public class Wiki
{
  private readonly Dictionary<string, WikiDump> _dumps;

  /// <summary>
  /// Create a new Wiki instance (wrapping an existing wiki folder in a repository)
  /// </summary>
  public Wiki(WikiRepo repo, string wikitag)
  {
    _dumps = new Dictionary<string, WikiDump>(); // no point in setting the case sensitivity - valid keys only have digits
    Repository = repo;
    WikiTag = wikitag;
    if(!WikiRepo.IsValidWikiName(wikitag))
    {
      throw new ArgumentOutOfRangeException(nameof(wikitag), $"'{wikitag}' is not a valid wiki name");
    }
    Folder = Path.Combine(Repository.Folder, WikiTag);
    if(!Directory.Exists(Folder))
    { 
      Directory.CreateDirectory(Folder);
    }
    SynchronizeDumps(false);
  }

  /// <summary>
  /// The repository owning this WikiFolder
  /// </summary>
  public WikiRepo Repository { get; init; }

  /// <summary>
  /// The tag identifying this wiki in its repository
  /// </summary>
  public string WikiTag { get; init; }

  /// <summary>
  /// The folder containing the data for this wiki
  /// </summary>
  public string Folder {  get; init; }

  /// <summary>
  /// Enumerate the tracked dumps
  /// </summary>
  public IReadOnlyCollection<WikiDump> Dumps { get => _dumps.Values; }

  /// <summary>
  /// Synchronize the tracked dump instances to the state on disk
  /// </summary>
  public void SynchronizeDumps(bool recurse)
  {
    var di = new DirectoryInfo(Folder);
    var dumptags = new List<string>();
    foreach(var folder in di.GetDirectories("2???????"))
    {
      var dumptag = folder.Name;
      if(WikiRepo.IsValidWikiDumpTag(dumptag))
      {
        dumptags.Add(dumptag);
      }
    }
    var olddumps = _dumps.Keys.ToHashSet();
    var missing = olddumps.Except(dumptags);
    foreach(var key in missing)
    {
      _dumps.Remove(key);
    }
    foreach(var key in dumptags)
    {
      if(!_dumps.TryGetValue(key, out var dump))
      {
        dump = new WikiDump(this, key);
        _dumps[dump.DumpTag] = dump;
      }
      else if(recurse)
      {
        dump.Synchronize();
      }
    }
  }
}
