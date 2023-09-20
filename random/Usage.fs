// (c) 2023  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage detail =
  cp "\foUtility for generating random values in various ways\f0"
  cp ""
  cp "\forandom guid\f0 [\fg-n\f0 <\fccount\f0=\fy1\f0>]"
  cp "  Generate a type 4 (random) GUID"
  cp ""
  cp "\forandom hex\f0 [\fg-bytes\f0|\fg-b\f0] <\fcbytes\f0=\fy16\f0> [\fg-n\f0 <\fccount\f0=\fy1\f0>]"
  cp "\forandom HEX\f0 [\fg-bytes\f0|\fg-b\f0] <\fcbytes\f0=\fy16\f0> [\fg-n\f0 <\fccount\f0=\fy1\f0>]"
  cp "\forandom base32\f0 [\fg-bytes\f0|\fg-b\f0] <\fcbytes\f0=\fy20\f0> [\fg-n\f0 <\fccount\f0=\fy1\f0>]"
  cp "\forandom base64\f0 [\fg-bytes\f0|\fg-b\f0] <\fcbytes\f0=\fy18\f0> [\fg-n\f0 <\fccount\f0=\fy1\f0>]"
  cp "  Generate \fcbytes\f0 random bytes and encode them in hex/base32/base64"
  cp ""
  cp "\fyCommon options:\f0"
  cp "\fg-v\f0\fx\fx             Verbose mode"
  cp "\fg-n\f0 <\fccount\f0>     Repeat count"
  cp "\fg-bits\f0 <\fcbits\f0>   Target entropy"



