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
  /// <summary>
  /// Create a new Wiki instance (wrapping an existing wiki folder in a repository)
  /// </summary>
  public Wiki(WikiRepo repo, string wikitag)
  {
    Repository = repo;
    WikiTag = wikitag;
    if(!WikiRepo.IsValidWikiName(wikitag))
    {
      throw new ArgumentOutOfRangeException(nameof(wikitag), $"'{wikitag}' is not a valid wiki name");
    }
    Folder = Path.Combine(Repository.Folder, WikiTag);
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
}
