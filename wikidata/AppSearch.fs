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

  // temporary shortcut - only evaluate first slice
  if articleDb.Slices.Count = 0 then
    failwith "No index slices found"
  let slice = articleDb.Slices[0]
  let index = slice |> ArticleIndex.FromSlice
  let rows = index.Rows // |> Seq.truncate o.MaxMatches
  let matchingRows =
    rows
    |> Seq.filter (isMatch o)
    |> Seq.truncate o.MaxMatches
    |> Seq.toList
  cp $"\fr DBG \f0 Got \fb{matchingRows.Length}\f0 rows."
  for row in matchingRows do
    cp $"\fb{row.ByteCount,7} \fc{row.PageId,12}  \fg{row.Title}\f0"
  
  1

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
    Tag = "search"
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

