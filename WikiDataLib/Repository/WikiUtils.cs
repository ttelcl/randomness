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
/// Miscellaneous static utility methods
/// </summary>
public static class WikiUtils
{
  /// <summary>
  /// Create a file-safe string based on the argument string
  /// </summary>
  public static string MakeSlug(string title)
  {
    var sb = new StringBuilder();
    foreach(var ch in title)
    {
      if(Char.IsDigit(ch) || Char.IsLetter(ch) || ch=='-' || ch=='_')
      {
        sb.Append(ch);
      }
    }
    return sb.ToString();
  }

  /// <summary>
  /// Create a file-safe string based on the title of the row
  /// </summary>
  public static string MakeSlug(this ArticleIndexRow row)
  {
    return MakeSlug(row.Title); 
  }
}
