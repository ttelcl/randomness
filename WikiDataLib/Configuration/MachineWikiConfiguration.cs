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

using Newtonsoft.Json;

namespace WikiDataLib.Configuration;

/// <summary>
/// Model for machine-wide configuration (JSON Deserializable)
/// </summary>
public class MachineWikiConfiguration
{
  /// <summary>
  /// Create a new MachineWikiConfiguration
  /// </summary>
  public MachineWikiConfiguration([JsonProperty("repo-folder")] string repofolder)
  {
    RepoFolder = repofolder;
  }

  /// <summary>
  /// The machine's wiki repo folder
  /// </summary>
  [JsonProperty("repo-folder")]
  public string RepoFolder { get; init; }

  /// <summary>
  /// Load the default instance from the configuration file
  /// </summary>
  /// <param name="filepath">
  /// The path to the configuration file or null to use the default configuration file
  /// </param>
  public static MachineWikiConfiguration LoadConfiguration(
    string? filepath = null)
  {
    filepath = filepath ?? DefaultConfigFilePath;
    if(!File.Exists(filepath))
    {
      throw new FileNotFoundException(
        "WikiData Configuration file not found",
        filepath);
    }
    var json = File.ReadAllText(filepath);
    return JsonConvert.DeserializeObject<MachineWikiConfiguration>(json) ??
      throw new InvalidDataException($"Invalid JSON content in {filepath}");
  }

  /// <summary>
  /// The path to the default configuration file
  /// </summary>
  public static string DefaultConfigFilePath { get; } =
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WikiData",
        ".wikidata.json");
}
