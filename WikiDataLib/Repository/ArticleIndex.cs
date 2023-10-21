/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

}
