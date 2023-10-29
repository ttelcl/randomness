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
/// Utilities for handling MediaWiki XML content
/// </summary>
public static class WikiXml
{

  /// <summary>
  /// Return a new instance of <see cref="XmlReaderSettings"/> with
  /// settings suitable for WikiXml fragment reading.
  /// </summary>
  public static XmlReaderSettings CreateFragmentReaderSettings()
  {
    var settings = new XmlReaderSettings() {
      ConformanceLevel = ConformanceLevel.Fragment,
      CheckCharacters = false,
      CloseInput = false,
    };
    return settings;
  }

  /// <summary>
  /// Wrap a TextReader stream that contains a sequence of {page}
  /// fragments as a fragment compatible XmlReader. Closing the
  /// XmlReader will not close the TextReader.
  /// </summary>
  public static XmlReader WrapFragments(TextReader textReader)
  {
    var reader = XmlReader.Create(textReader, CreateFragmentReaderSettings());
    return reader;
  }

  /// <summary>
  /// Read a series of page fragment "documents" from the XML fragment stream.
  /// The {page} elements are expected to not be in any XML namespace
  /// </summary>
  public static IEnumerable<XPathDocument> ReadPageFragments(XmlReader xr)
  {
    while(xr.Read())
    {
      if(xr.NodeType == XmlNodeType.Element)
      {
        if(xr.LocalName == "page")
        {
          if(!String.IsNullOrEmpty(xr.NamespaceURI))
          {
            throw new InvalidOperationException(
              $"Expecting the input to be just <page> elements without wrapping. Detected namespace '{xr.NamespaceURI}' instead");
          }
          var xr2 = xr.ReadSubtree();
          var xpd = new XPathDocument(xr2);
          yield return xpd;
        }
      }
    }
  }

  /// <summary>
  /// Read a series of page fragment "documents" from the XML fragment stream.
  /// The {page} elements are expected to not be in any XML namespace
  /// </summary>
  public static IEnumerable<XPathDocument> ReadPageFragments(TextReader tr)
  {
    using(var xr = WrapFragments(tr))
    {
      foreach(var xpd in ReadPageFragments(xr))
      {
        yield return xpd;
      }
    }
  }

  /// <summary>
  /// Read a series of page fragment "documents" from the XML fragment stream
  /// (assumed to be UTF8 text).
  /// The {page} elements are expected to not be in any XML namespace
  /// </summary>
  public static IEnumerable<XPathDocument> ReadPageFragments(Stream stream)
  {
    using(var tr = new StreamReader(stream, Encoding.UTF8, false))
    using(var xr = WrapFragments(tr))
    {
      foreach(var xpd in ReadPageFragments(xr))
      {
        yield return xpd;
      }
    }
  }

}
