/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace WikiDataLib.Repository;

/// <summary>
/// JSON serializable article reference inside a study.
/// Note that the wiki and wiki date is not included here,
/// they are found in the study.
/// </summary>
public class StudyArticle
{
  /// <summary>
  /// Create a new StudyArticle
  /// </summary>
  public StudyArticle(
    [JsonProperty("page")] long pageId,
    [JsonProperty("title")] string title,
    [JsonProperty("stream")] int streamId)
  {
    PageId = pageId;
    Title = title;
    StreamId = streamId;
  }

  /// <summary>
  /// Create a StudyArticle from an <see cref="ArticleIndexRow"/>
  /// </summary>
  public static StudyArticle FromIndexRow(ArticleIndexRow row)
  {
    return new StudyArticle(
      row.PageId,
      row.Title,
      row.StreamId);
  }

  /// <summary>
  /// The wikipedia page id for the article
  /// </summary>
  [JsonProperty("page")]
  public long PageId { get; init; }

  /// <summary>
  /// The title of the article
  /// </summary>
  [JsonProperty("title")]
  public string Title { get; init; }

  /// <summary>
  /// The stream in which the page was found
  /// </summary>
  [JsonProperty("stream")]
  public int StreamId { get; init; }

}
