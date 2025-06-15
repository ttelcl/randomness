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
  cp "\forandom hex\f0 \fg-bits\f0 <\fcbits\f0=\fy64\f0> [\fg-n\f0 <\fccount\f0=\fy1\f0>]"
  cp "\forandom HEX\f0 \fg-bits\f0 <\fcbits\f0=\fy64\f0> [\fg-n\f0 <\fccount\f0=\fy1\f0>]"
  cp "\forandom base32\f0 \fg-bits\f0 <\fcbits\f0=\fy100\f0> [\fg-n\f0 <\fccount\f0=\fy1\f0>]"
  cp "\forandom base64\f0 \fg-bits\f0 <\fcbits\f0=\fy96\f0> [\fg-n\f0 <\fccount\f0=\fy1\f0>]"
  cp "  Generate \fcbits\f0 random bits and encode them in hex/base32/base64"
  cp "  <\fcbits\f0> must be a multiple of the encoding unit (4 for hex, 5 for base32, 6 for base64)"
  cp ""
  cp "\forandom characters\f0 [\fg-alphabet\f0|\fg-a\f0] <\fcalphabet\f0> [\fg-bits\f0 <\fcentropy\f0=\fy128\f0>] [\fg-n\f0 <\fccount\f0=\fy1\f0>]" 
  cp "  Generate a sequence of characters randomly picked from the given alphabet."
  cp "  \fg-bits\f0 <\fcentropy\f0>   Target minimum entropy (in bits)."
  cp "  \fg-a\f0 <\fccharacters\f0>   The characters to choose from. You can include ranges using '-'"
  cp "  \fx  \fx  \fx          \fx    To include '-' in the alphabet it must be the last character."
  cp ""
  cp "\forandom words\f0 \fg-list\f0 <\fclistname\f0> [\fg-bits\f0 <\fcentropy\f0=\fy128\f0>] [\fg-n\f0 <\fccount\f0=\fy1\f0>]"
  cp "\forandom words\f0 \fg-listlists\f0"
  cp "  Generate a sequence of words randomly picked from the specified word lists."
  cp "  \fg-listlists\f0\fx\fx        List the names of available word lists and abort."
  cp "  \fg-list\f0 <\fclistname\f0>  Add a word list to use. Repeatable. Words are picked from the lists cyclically"
  cp "  \fg-bits\f0 <\fcentropy\f0>   Target minimum entropy (in bits)."
  cp ""
  cp "\forandom wordlets\f0 [\fg-bits\f0 <\fcentropy\f0=\fy128\f0>] [\fg-n\f0 <\fccount\f0=\fy1\f0>]"
  cp "  Generate a sequence of wordlets randomly generated picked using the selected wordlet picker."
  cp "  \fg-bits\f0 <\fcentropy\f0>   Target minimum entropy (in bits)."
  cp ""
  cp "\fyCommon options:\f0"
  cp "\fg-v\f0\fx\fx             Verbose mode"
  cp "\fg-n\f0 <\fccount\f0>     Repeat count"
  cp "\fg-bits\f0 <\fcbits\f0>   Target bitcount"



