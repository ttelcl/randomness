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

using XsvLib;
using XsvLib.Buffers;

namespace WikiDataLib.Repository;

/// <summary>
/// Table with article descriptors. Articles are wiki pages in namespace 0 that
/// are not redirects.
/// </summary>
public class ArticleIndex
{
  private readonly Dictionary<long, ArticleIndexRow> _rowsById;

  /// <summary>
  /// Create a new ArticleIndex
  /// </summary>
  public ArticleIndex()
  {
    _rowsById = new Dictionary<long, ArticleIndexRow>();
  }

  /// <summary>
  /// Insert or replace an article index row
  /// </summary>
  public void Put(ArticleIndexRow row)
  {
    _rowsById[row.PageId] = row;
  }

  /// <summary>
  /// Try to retrieve a row by page ID, returning null if not found
  /// </summary>
  public ArticleIndexRow? FindRowById(int pageId)
  {
    return _rowsById.TryGetValue(pageId, out var row) ? row : null; 
  }

  /// <summary>
  /// The rows in this index
  /// </summary>
  public IReadOnlyCollection<ArticleIndexRow> Rows { get => _rowsById.Values; }

  /// <summary>
  /// Load rows from the (full or partial) article index file and
  /// merge them into this article index
  /// </summary>
  /// <param name="articleIndexFile">
  /// The CSV file to load
  /// </param>
  public void Import(string articleIndexFile)
  {
    using(var xsv = Xsv.ReadXsv(articleIndexFile))
    {
      foreach(var row in ArticleIndexRowBuffer.ReadXsv(xsv))
      {
        Put(row);
      }
    }
  }

  /// <summary>
  /// Save this article index to a CSV file
  /// </summary>
  public void Save(string articleIndexFile)
  {
    using(var writer = File.CreateText(articleIndexFile))
    {
      var buffer = new ArticleIndexRowBuffer();
      var itrw = Xsv.WriteXsv(writer, articleIndexFile, buffer.Count);
      buffer.WriteXsv(itrw, Rows, true);
    }
  }

}
