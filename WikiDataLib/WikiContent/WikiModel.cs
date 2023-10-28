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
/// Description of WikiModel
/// </summary>
public class WikiModel
{
  /// <summary>
  /// Create a new WikiModel
  /// </summary>
  public WikiModel(
    string wikiSourceText)
  {
    WikiSourceText = wikiSourceText;
    var parser = new WikitextParser();
    var parserOptions = new WikitextParserOptions() {
    };
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
  /// Return the plaintext as an enumeration of lines. This method has a few tweaks
  /// on top of the underlying implementation
  /// </summary>
  /// <param name="stripTables">
  /// When true, drop tables. This is a bug workaround
  /// </param>
  public IEnumerable<string> PlaintextLines(bool stripTables)
  {
    var lines = Model.ToPlainText()
      .Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None) ?? Array.Empty<string>();
    var drop = false;
    var wasEmpty = true;
    foreach(var line in lines)
    {
      if(stripTables && line.StartsWith("{|"))
      {
        drop = true;
      }
      if(!drop)
      {
        var isEmpty = String.IsNullOrEmpty(line);
        if(!isEmpty || !wasEmpty) // avoid multiple empty lines
        {
          yield return line;
        }
        wasEmpty = isEmpty;
      }
      if(line.StartsWith("|}"))
      {
        drop = false;
      }
    }
  }
}