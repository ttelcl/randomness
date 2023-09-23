/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomUtilities.ByteSources;

/// <summary>
/// Static class for creating sequences using a generator function
/// </summary>
public static class SequenceGenerator
{

  /// <summary>
  /// Create an infinite sequence by calling the generator function over and over again
  /// </summary>
  public static IEnumerable<T> InfiteFrom<T>(Func<T> generator)
  {
    while(true)
    {
      yield return generator();
    }
  }

}
