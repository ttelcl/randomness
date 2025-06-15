/*
 * (c) 2025  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RandomUtilities.ByteSources;

namespace RandomUtilities.Picking;

/// <summary>
/// A sequence of one or more <see cref="PickList{T}"/> that
/// will be picked as a group all at once
/// </summary>
public class PickSequence<T>
{
  /// <summary>
  /// Create a new PickSequence from existing pick lists
  /// </summary>
  public PickSequence(
    IEnumerable<PickList<T>> pickLists)
  {
    PickLists = pickLists.ToList().AsReadOnly();
    if(!PickLists.Any())
    {
      throw new ArgumentOutOfRangeException(
        nameof(pickLists),
        "Expecting at least one pick list as argument");
    }
    TotalInformationBits = PickLists.Sum(pl => pl.PickInformationBits);
  }

  /// <summary>
  /// Create a new PickSequence by creating new pick lists
  /// </summary>
  public PickSequence(
    IEnumerable<IEnumerable<T>> pickLists)
    : this(pickLists.Select(pl => new PickList<T>(pl)))
  {
  }

  /// <summary>
  /// The lists to pick from in sequence
  /// </summary>
  public IReadOnlyList<PickList<T>> PickLists { get; }

  /// <summary>
  /// The total information theoretical "information" of all the
  /// pick lists in this sequence (expressed in bits).
  /// </summary>
  public double TotalInformationBits { get; }
  
  /// <summary>
  /// Return a list of random picks from all the pick lists
  /// in this sequence
  /// </summary>
  public IReadOnlyList<T> Pick(BitSource randomSource)
  {
    var result = new T[PickLists.Count];
    for(var i = 0; i < result.Length; i++)
    {
      result[i] = PickLists[i].Pick(randomSource);
    }
    return result;
  }

}
