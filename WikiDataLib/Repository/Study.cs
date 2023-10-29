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

using Newtonsoft.Json;

namespace WikiDataLib.Repository;

/// <summary>
/// JSON serializable settings and defaults 
/// </summary>
public class Study
{
  private readonly List<StudyArticle> _articles;
  private bool _modified;

  /// <summary>
  /// Create a new WikiStudy
  /// </summary>
  public Study(
    [JsonProperty("wiki")] string wikiName,
    [JsonProperty("wikidate")] string wikiDate,
    [JsonProperty("articles")] IEnumerable<StudyArticle> articles)
  {
    _articles = new List<StudyArticle>(articles);
    WikiId = new WikiDumpId(wikiName, wikiDate);
    Articles = _articles.AsReadOnly();
  }

  /// <summary>
  /// Load a Study from a file
  /// </summary>
  public static Study FromFile(string fileName)
  {
    if(!File.Exists(fileName))
    {
      throw new FileNotFoundException($"No such file: {fileName}");
    }
    var json = File.ReadAllText(fileName);
    return JsonConvert.DeserializeObject<Study>(json)!;
  }

  /// <summary>
  /// Load a Study from the "study.json" file in the current directory,
  /// returning null if not found
  /// </summary>
  public static Study? FromFile()
  {
    var fileName = "study.json";
    if(!File.Exists(fileName))
    {
      return null;
    }
    return FromFile(fileName);
  }

  /// <summary>
  /// The bare wiki name (without dump tag)
  /// </summary>
  [JsonProperty("wiki")]
  public string WikiName { get => WikiId.WikiTag; }

  /// <summary>
  /// The date tag of the dump used for the wiki (in "yyyyMMdd" form)
  /// </summary>
  [JsonProperty("wikidate")]
  public string WikiDate { get => WikiId.DumpTag; }

  /// <summary>
  /// The list of articles in the wiki that "are of interest" for this study
  /// </summary>
  [JsonProperty("articles")]
  public IReadOnlyList<StudyArticle> Articles { get; }

  /// <summary>
  /// The wiki id used to identify the wiki in other classes in this library.
  /// </summary>
  [JsonIgnore]
  public WikiDumpId WikiId { get; init; }

  /// <summary>
  /// Returns true if articles were added or removed
  /// </summary>
  [JsonIgnore]
  public bool Modified { get => _modified; }

  /// <summary>
  /// Add or replace an article in <see cref="Articles"/>.
  /// </summary>
  public void AddArticle(StudyArticle article)
  {
    var old = FindArticle(article.PageId);
    if(old != null)
    {
      _articles.Remove(old);
    }
    _articles.Add(article);
    _modified = true;
  }

  /// <summary>
  /// Add or replace an article
  /// </summary>
  public StudyArticle AddArticle(ArticleIndexRow row)
  {
    var sa = StudyArticle.FromIndexRow(row);
    AddArticle(sa);
    return sa;
  }

  /// <summary>
  /// Find an article by ID
  /// </summary>
  public StudyArticle? FindArticle(long pageId)
  {
    return _articles.FirstOrDefault(article => article.PageId == pageId);
  }

  /// <summary>
  /// Remove an article, if present
  /// </summary>
  public bool RemoveArticle(long pageId)
  {
    var old = FindArticle(pageId);
    if(old != null)
    { 
      _articles.Remove(old); 
      _modified = true;
      return true;
    }
    return false;
  }

  /// <summary>
  /// Save this instance to the given file
  /// </summary>
  public void SaveToFile(string fileName)
  {
    var json = JsonConvert.SerializeObject(this, Formatting.Indented);
    File.WriteAllText(fileName, json);
    _modified = false;
  }

}
