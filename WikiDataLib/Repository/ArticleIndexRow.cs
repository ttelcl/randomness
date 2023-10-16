/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XsvLib;

namespace WikiDataLib.Repository;

/// <summary>
/// A row in the article index
/// </summary>
public class ArticleIndexRow
{
  /// <summary>
  /// Create a new ArticleIndexRow
  /// </summary>
  public ArticleIndexRow(
    int pageId,
    int streamId,
    string title,
    int byteCount,
    long revisionId,
    DateTime timestamp)
  {
    PageId = pageId;
    RevisionId = revisionId;
    Title = title;
    StreamId = streamId;
    ByteCount = byteCount;
    TimeStamp = timestamp;
  }

  /// <summary>
  /// The page ID
  /// </summary>
  public int PageId { get; init; }

  /// <summary>
  /// The index of the stream containing the page content
  /// </summary>
  public int StreamId { get; init; }

  /// <summary>
  /// The title
  /// </summary>
  public string Title { get; init; }

  /// <summary>
  /// The ISO format UTC time stamp
  /// </summary>
  public DateTime TimeStamp { get; init; }

  /// <summary>
  /// The current revision ID
  /// </summary>
  public long RevisionId { get; init; }

  /// <summary>
  /// The number of bytes in the content
  /// </summary>
  public int ByteCount { get; init; }

  /// <summary>
  /// Read the article index rows
  /// </summary>
  public static IEnumerable<ArticleIndexRow> ReadXsv(ITextRecordReader itrr)
  {
    var reader = new XsvReader(itrr, true);
    var buffer = new ArticleIndexRowBuffer(reader.Header);
    foreach(var textrow in reader.ReadRecords())
    {
      buffer.Attach(textrow);
      yield return buffer.GetRow();
      buffer.Detach();
    }
  }

  /// <summary>
  /// Write the article index rows
  /// </summary>
  public static void WriteXsv(ITextRecordWriter itrw, IEnumerable<ArticleIndexRow> rows)
  {
    var buffer = new ArticleIndexRowBuffer();
    buffer.WriteHeader(itrw);
    buffer.Attach(new string[buffer.Count]);
    foreach(var row in rows)
    {
      buffer.PutRow(row);
      buffer.WriteRow(itrw);
    }
  }
}
