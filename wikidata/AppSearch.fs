module AppSearch

open System
open System.IO
open System.Text
open System.Xml

open XsvLib

open WikiDataLib.Repository

open ColorPrint
open CommonTools

type private Options = {
  WikiId: WikiDumpId option
  MaxMatches: int
  Tag: string
  MinBytes: int
  MaxBytes: int
  MatchText: string list
  RejectText: string list
}

let private isMatch o (r: ArticleIndexRow) =
  //cp $"TESTING \fb{r.ByteCount,7} \fc{r.PageId,12}  \fg{r.Title}\f0"
  r.ByteCount >= o.MinBytes
  && r.ByteCount <= o.MaxBytes
  && (o.MatchText
      |> Seq.tryFind (fun txt -> r.Title.Contains(txt, StringComparison.OrdinalIgnoreCase) |> not)
      |> Option.isNone)
  && (o.RejectText
      |> Seq.tryFind (fun txt -> r.Title.Contains(txt, StringComparison.OrdinalIgnoreCase))
      |> Option.isNone)

let private runSearch o =
  let repo = new WikiRepo()
  let wikiId =
    match o.WikiId with
    | Some(wid) -> wid
    | None -> o.WikiId |> WikiUtils.resolveWiki
  let dump = wikiId |> repo.GetDumpFolder
  if dump.HasStreamIndex |> not then
    failwith $"'{wikiId}' has no stream index yet (run 'wikidata index -wiki {wikiId}' to create it)"
  let articleDb = dump.LoadArticleDb()
  let matches = new ArticleIndex()

  if articleDb.Slices.Count = 0 then
    failwith "No index slices found"

  let echoSlice (slice: ArticleIndexSlice) =
    cp $"Scanning \fy{slice.FileName}\f0."
    slice

  let matchingRows =
    articleDb.Slices
    |> Seq.map echoSlice
    |> Seq.map ArticleIndex.FromSlice
    |> Seq.collect (fun aidx -> aidx.Rows)
    |> Seq.filter (isMatch o)
    |> Seq.truncate o.MaxMatches
  for row in matchingRows do
    cp $"\fb{row.ByteCount,7} \fc{row.PageId,12}  \fg{row.Title}\f0"
    row |> matches.Put

  cp $"Found \fb{matches.Rows.Count}\f0 matching rows."

  if o.Tag |> String.IsNullOrEmpty then
    cp "\foNo output tag specified.\f0 Not saving results to a file."
  else
    let outputName = $"{o.Tag}.articles.csv"
    cp $"Saving \fg{outputName}\f0."
    matches.Save(outputName + ".tmp")
    outputName |> finishFile
  
  0

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | "-wiki" :: key :: rest ->
      let wdi = key |> WikiDumpId.Parse
      rest |> parseMore {o with WikiId = Some(wdi)}
    | "-tag" :: tag :: rest ->
      rest |> parseMore {o with Tag = tag}
    | "-max" :: maxCount :: rest ->
      let mc = maxCount |> Int32.Parse
      rest |> parseMore {o with MaxMatches = mc}
    | "-minbytes" :: txt :: rest ->
      let mb = txt |> Int32.Parse
      rest |> parseMore {o with MinBytes = mb}
    | "-maxbytes" :: txt :: rest ->
      let mb = txt |> Int32.Parse
      rest |> parseMore {o with MaxBytes = mb}
    | "-has" :: txt :: rest ->
      rest |> parseMore {o with MatchText = txt :: o.MatchText}
    | "-hasnot" :: txt :: rest ->
      rest |> parseMore {o with RejectText = txt :: o.RejectText}
    | [] ->
      Some(o)
    | x :: _ ->
      cp $"\frError: unrecognized argument \f0'\fo{x}\f0'"
      None
  let oo = args |> parseMore {
    WikiId = None
    MaxMatches = 1000
    Tag = null
    MinBytes = 0
    MaxBytes = Int32.MaxValue
    MatchText = []
    RejectText = []
  }
  match oo with
  | None ->
    Usage.usage "search"
    1
  | Some(o) ->
    o |> runSearch

