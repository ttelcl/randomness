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
/// Article list database, providing an API on top of the collection
/// of all article index slices for a wiki dump
/// </summary>
public class ArticleDb
{
  private readonly List<ArticleIndexSlice> _slices;

  /// <summary>
  /// Create a new ArticleDb
  /// </summary>
  public ArticleDb(WikiDump owner)
  {
    Owner = owner;
    _slices = Owner.ArticleIndexSlices();
    Slices = _slices.AsReadOnly();
  }

  /// <summary>
  /// The owning wiki dump
  /// </summary>
  public WikiDump Owner { get; init; }

  /// <summary>
  /// The list of article index slice descriptors available
  /// </summary>
  public IReadOnlyList<ArticleIndexSlice> Slices { get; init; }

  /// <summary>
  /// Return the final slice
  /// </summary>
  public ArticleIndexSlice? FinalSlice { get => (Slices.Count == 0) ? null : Slices[^1]; }

  /// <summary>
  /// Return the final stream index (of the final slice)
  /// </summary>
  public int? FinalStreamIndex { get => FinalSlice?.EndIndex; }

  /// <summary>
  /// The number of streams indexed in this db
  /// </summary>
  // Note that _slices[^1].EndIndex really is _slices[^1].EndIndex + 1 - 1 (add 1 for end -> count,
  // sub 1 because the first real stream is 1)
  public int IndexedStreamCount {  get => _slices.Count == 0 ? 0 : _slices[^1].EndIndex; }
}