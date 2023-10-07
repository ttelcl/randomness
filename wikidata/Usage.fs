// (c) 2023  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage detail =
  let showSynopsis key = detail = "all" || detail = key
  let showDescription key = detail = "all" || detail = key
  if showSynopsis "config" then
    cp "\fowikidata config\f0 [\fg-init \f0<\fcrepo-folder\f0>]"
  if showDescription "config" then
    cp "   Check the configuration file and optionally initialize it if it did not exist yet"
    cp ""
  if showSynopsis "wiki-list" then
    cp "\fowikidata wiki-list\f0"
  if showDescription "wiki-list" then
    cp "   List known wikis"
    cp ""
  if showSynopsis "import" then
    cp "\fowikidata import\f0 \fg-pending\f0"
  if showDescription "import" then
    cp "   Import data files waiting in the repository's import-buffer folder, moving them to"
    cp "   the right subfolder"
  if showSynopsis "import" then
    cp "\fowikidata import\f0 {\fg-f\f0 <\fcfile\f0>}"
  if showDescription "import" then
    cp "   Import the named data file(s), moving them to the right folder in the repository"
    cp ""
  if showSynopsis "index" then
    cp "\fowikidata index\f0 \fg-dump\f0 <\fcwiki\fy-\fcdate\f0> [\fg-n\f0 <\fccount\f0>] [\fg-offset\f0 <\fcoffset\f0>]"
  if showDescription "index" then
    cp "   Initialize or update the stream index for a dump, if missing."
    cp "   \fg-file\f0 <\fcfilename\f0>      (For debug purposes)"
    cp ""
  cp "\fyCommon options\f0:"
  cp "\fg-v               \f0Verbose mode"



