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

namespace WikiDataLib.Repository;

/// <summary>
/// Represents one dump set for one wiki (identified by the tag of that wiki
/// plus the date of the dump)
/// </summary>
public class WikiDump
{
  /// <summary>
  /// Create a new WikiDump
  /// </summary>
  public WikiDump(Wiki wiki, string dumpTag)
  {
    Owner = wiki;
    Id = new WikiDumpId(wiki.WikiTag, dumpTag);
    Folder = Path.Combine(Owner.Folder, Id.DumpTag);
    if(!Directory.Exists(Folder))
    {
      Directory.CreateDirectory(Folder);
    }
    MainDumpFileName = Path.Combine(Folder, $"{Id}-pages-articles-multistream.xml.bz2");
    MainRawIndexFileName = Path.Combine(Folder, $"{Id}-pages-articles-multistream-index.txt.bz2");
    MainIndexFileName = Path.Combine(Folder, $"{Id}-pages-articles-multistream-index.txt");
    StreamIndexFileName = Path.Combine(Folder, $"{Id}.stream-index.csv");
    ArticleIndexFolderName = Path.Combine(Folder, "article-index");
    if(!Directory.Exists(ArticleIndexFolderName))
    {
      Directory.CreateDirectory(ArticleIndexFolderName);
    }
    Synchronize();
  }

  /// <summary>
  /// The wiki dump identifier
  /// </summary>
  public WikiDumpId Id { get; init; }

  /// <summary>
  /// The <see cref="Wiki"/> instance this Dump belongs to
  /// </summary>
  public Wiki Owner { get; init; }

  /// <summary>
  /// The dump tag for this particular dump of the wiki
  /// </summary>
  public string DumpTag { get => Id.DumpTag; }

  /// <summary>
  /// The folder containing the files for this dump
  /// </summary>
  public string Folder { get; init; }

  /// <summary>
  /// The full path to the main dump file (which may or may not yet exist).
  /// To be downloaded from a mirror site.
  /// </summary>
  public string MainDumpFileName { get; init; }

  /// <summary>
  /// True if the main dump file is present
  /// </summary>
  public bool HasMainFile { get => File.Exists(MainDumpFileName); }

  /// <summary>
  /// The full path to the main dump index file (which may or may not yet exist)
  /// (in compressed form)
  /// To be downloaded from a mirror site.
  /// </summary>
  public string MainRawIndexFileName { get; init; }

  /// <summary>
  /// True if the main compressed index file is present
  /// </summary>
  public bool HasMainIndexBz2 { get => File.Exists(MainRawIndexFileName); }

  /// <summary>
  /// The full path to the decompressed main dump index file (which may or may not yet exist)
  /// To be decompressed.
  /// </summary>
  public string MainIndexFileName { get; init; }

  /// <summary>
  /// True if the main uncompressed index file is present
  /// </summary>
  public bool HasMainIndex { get => File.Exists(MainIndexFileName); }

  /// <summary>
  /// The name of the stream index file (when complete)
  /// </summary>
  public string StreamIndexFileName { get; init; }

  /// <summary>
  /// True if the stream index file is present (and complete)
  /// </summary>
  public bool HasStreamIndex { get => File.Exists(StreamIndexFileName); }

  /// <summary>
  /// The full path to the folder holding the partial article index files
  /// </summary>
  public string ArticleIndexFolderName { get; init; }

  /// <summary>
  /// Find the index where to continue gathering the article index
  /// </summary>
  public int NextArticleIndexStream()
  {
    var sliceEnds = ArticleIndexSlices().Select(ais => ais.EndIndex).ToList();
    return sliceEnds.Count == 0 ? 1 : (sliceEnds.Max()+1);
  }

  /// <summary>
  /// Enumerate the partial article index slices found in the article index folder
  /// </summary>
  /// <returns></returns>
  public IEnumerable<ArticleIndexSlice> ArticleIndexSlices()
  {
    var di = new DirectoryInfo(ArticleIndexFolderName);
    foreach(var fi in di.EnumerateFiles($"{Id}.i-*.partidx.csv"))
    {
      var parts = fi.Name.Split('.');
      if(parts.Length == 4)
      {
        yield return ArticleIndexSlice.ParseFileName(fi.FullName);
      }
    }
  }

  /// <summary>
  /// Synchronize the state of this instance with the disk state.
  /// Currently this is a no-op
  /// </summary>
  public void Synchronize()
  {
  }

  /// <summary>
  /// Open the main dump file (a BZ2 stream)
  /// </summary>
  /// <returns></returns>
  public FileStream OpenMainDump()
  {
    if(!HasMainFile)
    {
      throw new FileNotFoundException(
        "The data file is missing", MainDumpFileName);
    }
    return File.OpenRead(MainDumpFileName);
  }

  /// <summary>
  /// Load the stream index (throws an exception if the file is missing)
  /// </summary>
  public SubstreamIndex LoadIndex()
  {
    if(!HasMainFile)
    {
      throw new FileNotFoundException(
        "The data file is missing", MainDumpFileName);
    }
    if(!HasStreamIndex)
    {
      throw new FileNotFoundException(
        "The stream index file is missing", StreamIndexFileName);
    }
    return new SubstreamIndex(StreamIndexFileName, MainDumpFileName);
  }

}