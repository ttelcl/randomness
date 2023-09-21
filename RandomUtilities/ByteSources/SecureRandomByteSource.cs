/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RandomUtilities.ByteSources;

/// <summary>
/// A byte source directly backed by the secure random generator
/// </summary>
public class SecureRandomByteSource : ByteSource
{

    /// <summary>
    /// Create a new SecureRandomByteSource
    /// </summary>
    public SecureRandomByteSource()
    {
    }

    /// <summary>
    /// Fill the span with cryptographically strong random bytes
    /// </summary>
    public override void ReadBytes(Span<byte> bytes)
    {
        RandomNumberGenerator.Fill(bytes);
    }
}
