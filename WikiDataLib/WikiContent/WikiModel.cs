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
using MwParserFromScratch.Nodes;

namespace WikiDataLib.WikiContent;

/// <summary>
/// Wraps a wiki content AST
/// </summary>
public class WikiModel
{
  /// <summary>
  /// Create a new WikiModel
  /// </summary>
  public WikiModel(
    WikiModelParseSettings settings,
    string wikiSourceText)
  {
    WikiSourceText = wikiSourceText;
    Settings = settings;
    var parser = new WikitextParser();
    var parserOptions = settings.CreateParserOptions();
    parser.Options = parserOptions;
    Model = parser.Parse(wikiSourceText);
  }

  /// <summary>
  /// The original content
  /// </summary>
  public string WikiSourceText { get; }

  /// <summary>
  /// The parsed model AST
  /// </summary>
  public Wikitext Model { get; }

  /// <summary>
  /// Settings used in parsing and postprocessing
  /// </summary>
  public WikiModelParseSettings Settings { get; }

  /// <summary>
  /// Return the plaintext as an enumeration of lines. This method has a few tweaks
  /// on top of the underlying implementation
  /// </summary>
  /// <param name="stripTables">
  /// When true, drop tables. This is a bug workaround
  /// </param>
  public IEnumerable<string> PlaintextLines(bool stripTables)
  {
    // TODO: Category links
    var lines = Model.ToPlainText()
      .Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None) ?? Array.Empty<string>();
    var tableLevel = 0;
    var wasEmpty = true;
    foreach(var line in lines)
    {
      if(stripTables && line.StartsWith("{|"))
      {
        tableLevel++;
      }
      if(tableLevel == 0)
      {
        var line2 = line;
        var parts = line2.Split(':', 2);
        if(parts.Length > 1 
          && parts[1].Length > 0
          && !Char.IsWhiteSpace(parts[1][0])
          && Settings.CategoryAliases.Contains(parts[0]))
        {
          line2 = String.Empty;
        }
        var isEmpty = String.IsNullOrEmpty(line2);
        if(!isEmpty || !wasEmpty) // avoid multiple empty lines
        {
          yield return line2;
        }
        wasEmpty = isEmpty;
      }
      if(line.StartsWith("|}"))
      {
        tableLevel--;
      }
    }
  }
}