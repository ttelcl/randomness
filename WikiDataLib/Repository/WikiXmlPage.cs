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
using System.Xml;
using System.Xml.XPath;

namespace WikiDataLib.Repository;

/// <summary>
/// Wraps one {page} XML element, read from an UNwrapped xml page fragment stream
/// </summary>
public class WikiXmlPage
{
  /// <summary>
  /// Create a new WikiXmlPage
  /// </summary>
  public WikiXmlPage(XPathDocument doc)
  {
    Doc = doc;
    var nav = Doc.CreateNavigator();
    nav.MoveToRoot();
    if(nav.Name != "path")
    {
      throw new InvalidDataException(
        $"Expecting '<path>' as root element, but found '{nav.Name}'");
    }
    Id = Int64.Parse(EvaluateRequiredString(nav, "string(id)"));
    Title = EvaluateRequiredString(nav, "string(title)");
  }

  /// <summary>
  /// The underlying XPathDocument
  /// </summary>
  public XPathDocument Doc { get; init; }

  /// <summary>
  /// The page id (pre-extracted by the constructor)
  /// </summary>
  public long Id { get; init; }

  /// <summary>
  /// The page title (pre-extracted by the constructor)
  /// </summary>
  public string Title { get; init; }

  /// <summary>
  /// The namespace for MediaWiki XML dumps (version 0.10)
  /// </summary>
  public const string MediaWikiNamespace010 = "http://www.mediawiki.org/xml/export-0.10/";

  private static string EvaluateRequiredString(XPathNavigator nav, string xpath)
  {
    var value = nav.Evaluate(xpath) as string;
    if(String.IsNullOrEmpty(value))
    {
      throw new InvalidDataException(
        $"Failed to evaluate '{xpath}'");
    }
    return value;
  }
}
