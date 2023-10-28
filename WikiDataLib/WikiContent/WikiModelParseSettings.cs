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

using Newtonsoft.Json;

namespace WikiDataLib.WikiContent;

/// <summary>
/// Factory for <see cref="WikitextParserOptions"/>, and support
/// for the parsing process and plaintext postprocessing.
/// This class is JSON serializable
/// </summary>
public class WikiModelParseSettings
{
  /// <summary>
  /// Create a new WikiModelParseSettings
  /// </summary>
  public WikiModelParseSettings(
    [JsonProperty("image-namespaces")] IEnumerable<string>? imageNamespaces = null,
    [JsonProperty("category-aliases")] IEnumerable<string>? categoryAliases = null,
    [JsonProperty("alphabet")] string? alphabet = null,
    [JsonProperty("remove-at-end")] IEnumerable<string>? removeAtEnd = null)
  {
    ImageNamespaces = new HashSet<string>(imageNamespaces ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
    CategoryAliases = new HashSet<string>(categoryAliases ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
    RemoveAtEnd = new HashSet<string>(removeAtEnd ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
    Alphabet = String.IsNullOrEmpty(alphabet) ? "abcdefghijklmnopqrstuvwxyz" : alphabet;
  }

  /// <summary>
  /// Additional image namespaces. The english ones, "File" and "Image" are
  /// always implicitly included already.
  /// </summary>
  [JsonProperty("image-namespaces")]
  public ISet<string> ImageNamespaces { get; }

  /// <summary>
  /// Aliases to be considered equivalent to the english "Category".
  /// </summary>
  [JsonProperty("category-aliases")]
  public ISet<string> CategoryAliases { get; }

  /// <summary>
  /// The set of characters that are valid in words. "words" with characters
  /// outside this set will be skipped for analysis. By default this is the
  /// set of lower case characters from "a" to "z".
  /// </summary>
  [JsonProperty("alphabet")]
  public string Alphabet { get; }

  /// <summary>
  /// Strings that should be removed from the end of words before
  /// analyzing them
  /// </summary>
  [JsonProperty("remove-at-end")]
  public ISet<string> RemoveAtEnd { get; }

  /// <summary>
  /// Create a new <see cref="WikitextParserOptions"/> instance
  /// </summary>
  public WikitextParserOptions CreateParserOptions()
  {
    var wpo = new WikitextParserOptions();
    var insSet = new HashSet<string>(WikitextParserOptions.DefaultImageNamespaceNames, StringComparer.OrdinalIgnoreCase);
    foreach(var ins in ImageNamespaces)
    {
      insSet.Add(ins);
    }
    wpo.ImageNamespaceNames = insSet;
    return wpo;
  }

}
