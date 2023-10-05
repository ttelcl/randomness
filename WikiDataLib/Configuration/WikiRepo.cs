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

namespace WikiDataLib.Configuration;

/// <summary>
/// A repository directory comtaining wikipedia dump files (and maybe analysis results)
/// </summary>
public class WikiRepo
{
  /// <summary>
  /// Create a new WikiRepo
  /// </summary>
  public WikiRepo(string folder)
  {
    if(!Directory.Exists(folder))
    {
      throw new DirectoryNotFoundException(
        $"Directory not found: {folder}");
    }
    Folder = Path.GetFullPath(folder);
  }

  /// <summary>
  /// The root folder for the repository
  /// </summary>
  public string Folder { get; init; }

}
