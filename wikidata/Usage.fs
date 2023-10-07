// (c) 2023  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage detail =
  cp "\foTool for working with wikipedia dump files\f0"
  cp ""
  cp "\fowikidata config\f0 [\fg-init \f0<\fcrepo-folder\f0>]"
  cp "   Check the configuration file and optionally initialize it if it did not exist yet"
  cp ""
  cp "\fowikidata wiki-list\f0"
  cp "   List known wikis"
  cp ""
  cp "\fowikidata import\f0 \fg-pending\f0"
  cp "   Import data files waiting in the repository's import-buffer folder, moving them to"
  cp "   the right subfolder"
  cp "\fowikidata import\f0 {\fg-f\f0 <\fcfile\f0>}"
  cp "   Import the named data file(s), moving them to the right folder in the repository"
  cp ""
  cp "\fyCommon options\f0:"
  cp "\fg-v               \f0Verbose mode"



