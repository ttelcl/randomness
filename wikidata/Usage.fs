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
  if showSynopsis "streamindex" then
    cp "\fowikidata streamindex\f0 \fg-wiki\f0 <\fcwiki\fy-\fcdate\f0>"
  if showDescription "streamindex" then
    cp "   Initialize or update the stream index for a dump, if missing."
    cp ""
  if showSynopsis "dump" then
    cpx "\fowikidata dump\f0 \fg-wiki\f0 <\fcwiki\fy-\fcdate\f0> [\fg-i\f0 <\fcindex\f0>|\fg-s\f0 <\fcposition\f0>]"
    cp " [\fg-xml\f0|\fg-xml+\f0] [\fg-text\f0|\fg-text+\f0] [\fg-allns\f0]" 
  if showDescription "dump" then
    cp "   List pages available in a dump slice and optionally dump page documents in XML or wikitext form"
    cp "   \fg-wiki\f0 <\fcwiki\fy-\fcdate\f0>   The wiki dump to take the slice from"
    cp "   \fg-xml\f0\fx\fx\fx\fx                Dump XML for articles. \fg-xml+\f0: also redirects"
    cp "   \fg-text\f0\fx\fx\fx\fx               Dump wikitext for articles. \fg-text+\f0: also redirects"
    cp "   \fg-allns\f0\fx\fx\fx\fx              Modifies \fg-xml\f0 and \fg-txt\f0 to process all namespaces, not just articles"
    cp ""
  if showSynopsis "articleindex" then
    cp "\fowikidata articleindex\f0 \fg-wiki\f0 <\fcwiki\fy-\fcdate\f0> [\fg-n\f0 <streamcount\f0>] [\fg-split\f0]"
  if showDescription "articleindex" then
    cp "   Initialize or extend the article index, processing the next \fcn\f0 streams. 'articles' are pages that are not redirects"
    cp "   \fg-wiki\f0 <\fcwiki\fy-\fcdate\f0>   The wiki dump to extract from"
    cp "   \fg-split\f0\fx\fx\fx\fx              Create per-stream index files"
    cp "   \fg-n\f0 <streamcount\f0>             The number of streams to process"
    cp ""
  if showSynopsis "extract" then
    cp "\fowikidata extract\f0 \fg-wiki\f0 <\fcwiki\fy-\fcdate\f0> [\fg-raw\f0] [\fg-nowrap\f0] {\fg-s\f0 <\fcstart\f0>|\fg-i\f0 <\fcindex\f0>}"
  if showDescription "extract" then
    cp "   Extract an entry from a wikidump to the current directory"
    cp "   \fg-wiki\f0 <\fcwiki\fy-\fcdate\f0>   The wiki dump to extract from"
    cp "   \fg-raw\f0\fx\fx\fx\fx                Extract the raw compressed segment (do not decompress)"
    cp "   \fg-nowrap\f0\fx\fx\fx\fx             Do not prepend the header and append the trailer (saving a sequence"
    cp "   \fx\fx\fx\fx\fx\fx                    of XML fragments instead of a document)"
    cp "   \fg-s\f0 <\fcstart\f0>\fx\fx          Extract the substream at the specified position (see *.stream-index.csv)"
    cp "   \fg-i\f0 <\fcindex\f0>\fx\fx          Extract the i-th substream. Pass a negative value to count from the end."
    cp "   \fx\fx\fx\fx\fx\fx                    \fc0\f0 is the header and \fc-1\f0 the trailer"
    cp ""
  cp "\fyCommon options\f0:"
  cp "\fg-v               \f0Verbose mode"



