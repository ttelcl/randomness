// (c) 2023  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage detail =
  let showSynopsis key = detail = "all" || detail = "short" || detail = key
  let showDescription key = detail = "all" || detail = key
  if showSynopsis "config" then
    cp "\fowikidata config\f0 [\fg-init \f0<\fcrepo-folder\f0>]"
  if showDescription "config" then
    cp "   Check the configuration file and optionally initialize it if it did not exist yet"
    cp ""
  if showSynopsis "list" then
    cp "\fowikidata list\f0"
  if showDescription "list" then
    cp "   List known wikis (aliases: \fowiki-list\f0, \fowikilist\f0)"
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
    cp "\fowikidata index\f0 \fg-wiki\f0 <\fcwiki\fy-\fcdate\f0> [\fg-n\f0 <\fccount\f0>] [\fg-offset\f0 <\fcoffset\f0>]"
  if showDescription "index" then
    cp "   Initialize or update the stream index for a dump, if missing."
    cp "   \fg-file\f0 <\fcfilename\f0>      (For debug purposes)"
    cp ""
  if showSynopsis "extract" then
    cp "\fowikidata extract\f0 \fg-wiki\f0 <\fcwiki\fy-\fcdate\f0> [\fg-raw\f0] [\fg-nowrap\f0] {\fg-s\f0 <\fcstart\f0>|\fg-i\f0 <\fcindex\f0>}"
  if showDescription "extract" then
    cp "   Extract an entry from a wikidump to the current directory"
    cp "   \fg-wiki\f0 <\fcwiki\fy-\fcdate\f0>   The wiki dump to extract from"
    cp "   \fg-raw\f0\fx\fx                      Extract the raw compressed segment (do not decompress)"
    cp "   \fg-nowrap\f0\fx\fx                   Do not prepend the header and append the trailer (saving a sequence"
    cp "   \fx\fx\fx\fx                          of XML fragments instead of a document)"
    cp "   \fg-s\f0 <\fcstart\f0>                Extract the substream at the specified position (see *.stream-index.csv)"
    cp "   \fg-i\f0 <\fcindex\f0>                Extract the i-th substream. Pass a negative value to count from the end."
    cp "   \fx\fx\fx\fx                          \fc0\f0 is the header and \fc-1\f0 the trailer"
    cp "   \fg-info\f0\fx\fx                     Do not extract anything but print info"
    cp ""
  cp "\fyCommon options\f0:"
  cp "\fg-v               \f0Verbose mode"



