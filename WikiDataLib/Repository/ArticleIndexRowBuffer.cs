﻿/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XsvLib;
using XsvLib.Buffers;

namespace WikiDataLib.Repository;

/// <summary>
/// Adapter for converting CSV lines to / from <see cref="ArticleIndexRow"/>
/// </summary>
public class ArticleIndexRowBuffer: XsvBuffer
{
  private readonly XsvTypedAccessor<long> _pageId;
  private readonly XsvTypedAccessor<int> _streamId;
  private readonly XsvTypedAccessor<string> _title;
  private readonly XsvTypedAccessor<int> _bytecount;
  private readonly XsvTypedAccessor<long> _revision;
  private readonly XsvTypedAccessor<DateTime> _timestamp;

  /// <summary>
  /// Create a new ArticleIndexRowBuffer
  /// </summary>
  /// <param name="columnOrder">
  /// The order of the columns, determined by the header of the CSV file
  /// to be read. Only used for reading scenarios; pass null for writing
  /// scenarios.
  /// </param>
  public ArticleIndexRowBuffer(
    IEnumerable<string>? columnOrder = null)
    : base(true, false)
  {
    AdapterLibrary
      .RegisterDateTimeIsoSeconds("datetime", true);
    _pageId = Declare<long>("pageId");
    _streamId = Declare<int>("streamId");
    _title = Declare<string>("title");
    _bytecount = Declare<int>("bytecount");
    _revision = Declare<long>("revision");
    _timestamp = Declare<DateTime>("timestamp", "datetime");
    if(columnOrder != null)
    {
      Lock(columnOrder);
    }
    else
    {
      Lock();
    }
  }

  /// <summary>
  /// Put the content of an <see cref="ArticleIndexRow"/> into this buffer
  /// (filling the attached string buffer)
  /// </summary>
  /// <param name="row">
  /// The row to convert
  /// </param>
  public void PutRow(ArticleIndexRow row)
  {
    _pageId.Set(row.PageId);
    _streamId.Set(row.StreamId);
    _title.Set(row.Title);
    _bytecount.Set(row.ByteCount);
    _revision.Set(row.RevisionId);
    _timestamp.Set(row.TimeStamp);
  }

  /// <summary>
  /// Read the content of the attached string buffer as a new <see cref="ArticleIndexRow"/>
  /// </summary>
  /// <returns></returns>
  public ArticleIndexRow GetRow()
  {
    return new ArticleIndexRow(
      _pageId.Get(),
      _streamId.Get(),
      _title.Get(),
      _bytecount.Get(),
      _revision.Get(),
      _timestamp.Get());
  }

  /// <summary>
  /// Read the article index rows
  /// </summary>
  public static IEnumerable<ArticleIndexRow> ReadXsv(ITextRecordReader itrr)
  {
    var reader = new XsvReader(itrr, true);
    var buffer = new ArticleIndexRowBuffer(reader.Header);
    foreach(var textrow in reader.ReadRecords())
    {
      // [[A bit of a design blooper, the intended behaviour of buffer.Attach is not implemented]]
      // buffer.Attach(textrow);
      // Instead: copy fields one by one
      for(var i = 0; i < textrow.Count; i++)
      {
        buffer[i] = textrow[i];
      }
      yield return buffer.GetRow();
      // buffer.Detach();
    }
  }

  /// <summary>
  /// Write the article index rows using this buffer
  /// </summary>
  public void WriteXsv(ITextRecordWriter itrw, IEnumerable<ArticleIndexRow> rows, bool writeHeader = true)
  {
    if(writeHeader)
    {
      WriteHeader(itrw);
    }
    Attach(new string[Count]);
    foreach(var row in rows)
    {
      PutRow(row);
      WriteRow(itrw);
    }
  }

}
