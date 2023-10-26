module AppStudyExport

open System
open System.IO
open System.Text
open System.Xml

open XsvLib

open WikiDataLib.Repository

open ColorPrint
open CommonTools

type private SearchCommand =
  | ByPage of int64


type private StudyExportOptions = {
  WikiId: WikiDumpId option
  Search: SearchCommand option
  WriteXml: bool
  WriteWikiText: bool
  WritePlainText: bool
}

let private runExport o =
  let command =
    match o.Search with
    | None -> failwith "No page selector specified"
    | Some(cmd) -> cmd
  let repo = new WikiRepo()
  let wikiId = o.WikiId |> WikiUtils.resolveWiki
  let dump = wikiId |> repo.GetDumpFolder
  if dump.HasStreamIndex |> not then
    failwith $"'{wikiId}' has no stream index yet (run 'wikidata index -wiki {wikiId}' to create it)"
  let subindex = dump.LoadIndex()
  let articleDb = dump.LoadArticleDb()
  if articleDb.IndexedStreamCount = 0 then
    cp "\frThe article index is empty\f0. (run \fowikidata articleindex\f0 to start building it)"
    1
  else
    let indexStreamCount = articleDb.IndexedStreamCount
    let percentage = 100.0 * float(indexStreamCount) / float(subindex.Count-2)
    if indexStreamCount < subindex.Count-2 then
      cp $"\foWarning\f0: the article index is incomplete (\fb{indexStreamCount}\f0 of \fb{subindex.Count-2}\f0, \fc%.2f{percentage} %%\f0)"
    else
      cp $"The article index is Complete (\fb{indexStreamCount}\f0 of \fb{subindex.Count-2}\f0, \fg%.2f{percentage} %%\f0)"
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
    | "-page" :: pageid :: rest ->
      let command = pageid |> Int64.Parse |> SearchCommand.ByPage
      rest |> parseMore {o with Search = Some(command)}
    | "-text" :: rest ->
      rest |> parseMore {o with WriteWikiText = true}
    | "-plain" :: rest ->
      rest |> parseMore {o with WritePlainText = true}
    | "-xml" :: rest ->
      rest |> parseMore {o with WriteXml = true}
    | [] ->
      if o.Search.IsNone then
        cp "\frNo page select option specified\f0."
        None
      else
        Some(o)
    | x :: _ ->
      cp $"\frUnrecognized argument\f0: '\fo{x}\f0'"
      None
  let oo = args |> parseMore {
    WikiId = None
    Search = None
    WriteXml = false
    WriteWikiText = false
    WritePlainText = false
  }
  match oo with
  | Some(o) ->
    runExport o
  | None ->
    Usage.usage "study"
    0

