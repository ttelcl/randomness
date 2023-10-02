// (c) 2023  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage detail =
  cpx "\foletterstats gather \fg-n\f0 <\fcorder\f0> [\fg-a\f0 <\fcalphabet\f0>] [\fg-b\f0 <\fcboundary\f0>]"
  cp " {\fg-words\f0 <\fcwordlist\f0>|\fy???\f0} \fg-tag\f0 <\fcoutput-tag\f0>"
  cp "  Collect letter distribution statistics"
  cp "  \fg-n\f0 <\fcorder\f0>         The order: the number of preceding characters to take into account."
  cp "  \fg-a\f0 <\fcalphabet\f0>      The letters of the alphabet (default a-z)."
  cp "  \fg-b\f0 <\fcboundary\f0>      The character to use as boundary marker (default: underscore)"
  cp "  \fg-words\f0 <\fcwordlist\f0>  The identifier of the word list to use as source (repeatable)"
  cp "  \fx     \fx  \fx        \fx    (Other source types will be added in the future)"
  cp "  \fg-tag\f0 <\fcoutput-tag\f0>  The tag to derive name of the output files"
  cp "  \fg-short\f0\fx\fx             Allow short words to be recorded"
  cp ""
  cp "\foletterstats generate \fg-f\f0 <\fcfile.n.word-fragments.json\f0> [\fg-n\f0 <\fcrepeat\f0>]"
  cp ""
  cp "\fyCommon options\f0:"
  cp "\fg-v               \f0Verbose mode"



