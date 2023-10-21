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

using XsvLib.StringConversion;

namespace WikiDataLib.Repository;

/// <summary>
/// Wraps one {page} XML element, read from an UNwrapped xml page fragment stream
/// </summary>
public class WikiXmlPage
{
  private XPathNavigator _pageNavigator;

  /// <summary>
  /// Create a new WikiXmlPage
  /// </summary>
  public WikiXmlPage(XPathDocument doc)
  {
    Doc = doc;
    var nav = CreateNewPageNavigator();
    if(nav.Name != "page")
    {
      throw new InvalidDataException(
        $"Expecting '<page>' as root element, but found '{nav.Name}'");
    }
    _pageNavigator = nav;
    Id = Int64.Parse(EvaluateRequiredString("id", nav));
    Title = EvaluateRequiredString("title", nav);
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
  /// Get the revision ID
  /// </summary>
  public long RevisionId { get => Int64.Parse(RequiredString("revision/id")); }

  /// <summary>
  /// The number of bytes in the content
  /// </summary>
  public int ContentSize { get => Int32.Parse(RequiredString("revision/text/@bytes")); }

  /// <summary>
  /// Get the MediaWiki namespace ID. This is 0 for normal articles
  /// </summary>
  public int NamespaceId { get => Int32.Parse(RequiredString("ns")); }

  /// <summary>
  /// The time stamp (as ISO formatted UTC time, including the trailing 'Z')
  /// </summary>
  public string Timestamp { get => RequiredString("revision/timestamp"); }

  /// <summary>
  /// The time stamp of the page revision as an UTC DateTime
  /// </summary>
  public DateTime UtcStamp { get => __stampAdapter.ParseString(Timestamp); }

  /// <summary>
  /// The title this page redirects to, if any
  /// </summary>
  public string? RedirectTitle { get => EvaluateString("redirect/@title"); }

  /// <summary>
  /// Returns a clone of the default page navigator
  /// </summary>
  public XPathNavigator PageNavigator { get => _pageNavigator.Clone(); }

  /// <summary>
  /// Return a new ArticleIndexRow for this page, if this page contains
  /// a plain article.
  /// </summary>
  /// <param name="streamId">
  /// The stream ID to be referenced in the index row
  /// </param>
  /// <returns>
  /// A new ArticleIndexRow if this page represents an article, null otherwise
  /// </returns>
  public ArticleIndexRow? MakeArticleIndexRow(int streamId)
  {
    if(!String.IsNullOrEmpty(RedirectTitle))
    {
      return null;
    }
    if(NamespaceId != 0)
    {
      return null; 
    }
    return new ArticleIndexRow(
      Id,
      streamId,
      Title,
      ContentSize,
      RevisionId,
      UtcStamp);
  }

  /// <summary>
  /// Create a new XPath navigator, positioned on the {page} element
  /// </summary>
  public XPathNavigator CreateNewPageNavigator()
  {
    var nav = Doc.CreateNavigator();
    nav.MoveToRoot();
    nav.MoveToFirstChild();
    return nav;
  }

  /// <summary>
  /// Evaluate the xpath expression wrapped in "string(...)" against
  /// the page root element.
  /// Throw an exception if the result is null or empty
  /// </summary>
  public string RequiredString(string xpath)
  {
    return EvaluateRequiredString(xpath, PageNavigator);
  }

  /// <summary>
  /// Evaluate the xpath expression wrapped in "string(...)" against
  /// the page root element. May return null or an empty string
  /// </summary>
  public string? EvaluateString(string xpath)
  {
    var value = EvaluateString(xpath, PageNavigator);
    return String.IsNullOrEmpty(value) ? null : value;
  }

  /// <summary>
  /// Wraps the experssion <paramref name="xpath"/> in "string(...)" and
  /// evaluates it against the navigator <paramref name="nav"/>.
  /// An exception is thrown if the result is null or empty
  /// </summary>
  public static string EvaluateRequiredString(string xpath, XPathNavigator nav)
  {
    xpath = "string(" + xpath + ")";
    var value = nav.Evaluate(xpath) as string;
    if(String.IsNullOrEmpty(value))
    {
      throw new InvalidDataException(
        $"Failed to evaluate '{xpath}'");
    }
    return value;
  }

  /// <summary>
  /// Wraps the experssion <paramref name="xpath"/> in "string(...)" and
  /// evaluates it against the navigator <paramref name="nav"/>.
  /// The return value may be null or empty.
  /// </summary>
  public static string? EvaluateString(string xpath, XPathNavigator nav)
  {
    xpath = "string(" + xpath + ")";
    var value = nav.Evaluate(xpath) as string;
    return value;
  }

  private static readonly StringAdapterLibrary __adapterLibrary =
    new StringAdapterLibrary().RegisterDateTimeIsoSeconds("wikiTimestampAdapter", true);
  private static readonly IStringAdapter<DateTime> __stampAdapter =
    __adapterLibrary.Get<DateTime>("wikiTimestampAdapter");

}
