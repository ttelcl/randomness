﻿// (c) 2023  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage detail =
  cpx "\foletterstats gather \fg-n\f0 <\fcorder\f0> [\fg-a\f0 <\fcalphabet\f0>] [\fg-b\f0 <\fcboundary\f0>]"
  cp " {\fg-words\f0 <\fcwordlist\f0>|\fg-wc\f0 <\fcfile\f0>|\fg-wcw\f0 <\fcfile\f0>} \fg-tag\f0 <\fcoutput-tag\f0>"
  cp "  Collect letter distribution statistics"
  cp "  \fg-n\f0 <\fcorder\f0>         The order: the number of preceding characters to take into account."
  cp "  \fg-a\f0 <\fcalphabet\f0>      The letters of the alphabet (default a-z)."
  cp "  \fg-b\f0 <\fcboundary\f0>      The character to use as boundary marker (default: underscore)"
  cp "  \fg-words\f0 <\fcwordlist\f0>  The identifier of the word list to use as source (repeatable)"
  cp "  \fg-wc\f0 <\fc*.words.csv\f0>  A *.words.csv file as source. Weights/counts are ignored"
  cp "  \fg-wcw\f0 <\fc*.words.csv\f0> A *.words.csv file as source. Counts are used as weights"
  cp "  \fg-tag\f0 <\fcoutput-tag\f0>  The tag to derive name of the output files"
  cp "  \fg-short\f0\fx\fx             Allow short words to be recorded"
  cp ""
  cpx "\foletterstats generate \f0[\fg-defaults\f0|\fg-f\f0 <\fcfile\f0>] [\fg-n\f0 <\fcrepeat\f0>]"
  cpx " [\fg-ml\f0 <\fccount\f0>] [\fg-mb\f0 <\fcbits\f0>] [\fg-xb\f0 <\fcbits\f0>] [\fg-mbl\f0 <\fcbits\f0>]"
  cp " [\fg-bl\f0 <\fcfile.words.csv\f0>] [\fg-info\f0] [\fg-bits\f0 <\fccount\f0>] [\fg-C\f0]"
  cp "  Generate random words from a distribution."
  cp "  Example: \foletterstats generate -standard -n 20 -bits 80\f0"
  cp "  \fg-f\f0 <\fcfile\f0>          The \fc*.word-fragments.json\f0 file to read the distribution from"
  cp "  \fg-n\f0 <\fcrepeat\f0>        The number of words / phrases to generate."
  cp "  \fg-ml\f0 <\fccount\f0>        Minimum length of generated words (default 4)."
  cp "  \fg-mb\f0 <\fcbits\f0>         Minimum information content of generated words (default 10.0 bits)."
  cp "  \fg-xb\f0 <\fcbits\f0>         Maximum information content of generated words (default 60.0 bits)."
  cp "  \fg-mbl\f0 <\fcbits\f0>        Minimum information content per letter."
  cp "  \fg-bl\f0 <\fc*.words.csv\f0>  Blacklist words in the file."
  cp "  \fg-info\f0\fx\fx              Display information content of generated words"
  cp "  \fg-C\f0\fx\fx                 Capitalize words and phrases"
  cp "  \fg-bits\f0 <\fcbits\f0>       Construct phrases of multiple words aiming at \fcbits\f0 total information"
  cp "  \fg-d\f0 <\fccount\f0>         Include \fccount\f0 digits in the phrase (default 0). Ignored unless \fg-bits\f0 is set."
  cp "  \fg-standard\f0\fx\fx          Changes options to a curated default (including distribution and blacklist files)"
  cp "  \fx         \fx\fx\fx          In addition to \fg-f\f0 and \fg-bl\f0 this sets \fg-mbl \fc2.5 \fg-mb \fc18 \fg-xb \fc25 \fg-ml \fc5 \fg-C \fg-d \fc2 \f0."
  cp ""
  cpx "\foletterstats wordcalc \fg-o\f0 <\fcout.words.csv\f0> {\fg+\f0 <\fcin.words.csv\f0>} " 
  cp "[\fg-1\f0] [\fg-ml\f0 <\fccount\f0>]"
  cp "  Combine and filter word count lists"
  cp "  \fg-o\f0 <\fcout.words.csv\f0> The output file."
  cp "  \fg+\f0 <\fcin.words.csv\f0>   Input file (repeatable). Allows wildcards in file name."
  cp "  \fg-ml\f0 <\fccount\f0>        Minimum length of input words."
  cp "  \fg-1\f0\fx\fx                 Replace all output word counts with '1'"
  cp ""
  cp "\fyCommon options\f0:"
  cp "\fg-v               \f0Verbose mode"



