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
/// A list of options to randomly pick from
/// </summary>
public class PickList<T>
{
  /// <summary>
  /// Create a new PickList
  /// </summary>
  public PickList(
    IEnumerable<T> options)
  {
    Options = options.ToList().AsReadOnly();
    if(!Options.Any())
    {
      throw new ArgumentOutOfRangeException(
        nameof(options),
        "Expecting at least one option to pick from");
    }
    PickInformationBits = Math.Log2(Options.Count);
  }

  /// <summary>
  /// The list of options to pick from
  /// </summary>
  public IReadOnlyList<T> Options { get; }

  /// <summary>
  /// The "information" (information theory) in bits added
  /// by picking uniformly from this list, assuming all
  /// options are distinct.
  /// </summary>
  public double PickInformationBits { get; }

  /// <summary>
  /// Randomly pick one of the options, using the given randomness
  /// source.
  /// </summary>
  public T Pick(BitSource randomSource)
  {
    return randomSource.RandomPick(Options);
  }
}
