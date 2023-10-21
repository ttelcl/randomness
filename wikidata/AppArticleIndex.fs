﻿module AppArticleIndex

open System
open System.IO
open System.Text
open System.Xml

open XsvLib

open WikiDataLib.Repository

open ColorPrint
open CommonTools

type private ArtIdxOptions = {
  WikiId: WikiDumpId option
  StreamCount: int
}

type WikiContext = {
  Dump: WikiDump
  SubIndex: SubstreamIndex
}

let private runArtIdx o =
  let repo = new WikiRepo()
  let wikiId =
    match o.WikiId with
    | Some(wid) -> wid
    | None -> failwith "No WikiDump selected"
  let dump = wikiId |> repo.GetDumpFolder
  if dump.HasStreamIndex |> not then
    failwith $"'{wikiId}' has no stream index yet (run 'wikidata index -wiki {wikiId}' to create it)"
  let subindex = dump.LoadIndex()
  let firstStreamIndex = dump.NextArticleIndexStream();
  let lastStreamIndex = firstStreamIndex + o.StreamCount - 1
  let lastStreamIndex =
    if lastStreamIndex >= subindex.Count-1 then subindex.Count-2 else lastStreamIndex
  if firstStreamIndex > lastStreamIndex then
    cp $"\fgThe article index appears to be complete\f0!"
    0
  else
    cp $"Processing streams \fb{firstStreamIndex}\f0 - \fb{lastStreamIndex}\f0:"
    let articleIndex = new ArticleIndex();
    let ais = new ArticleIndexSlice(dump.ArticleIndexFolderName, wikiId, firstStreamIndex, lastStreamIndex)
    use host = dump.OpenMainDump()
    for si in firstStreamIndex .. lastStreamIndex do
      cpx $"  Processing stream \fb{si}\f0 ...      "
      use slice = subindex.OpenConcatenation(host, [si], true)
      let documents =
        slice
        |> WikiXml.ReadPageFragments
      let mutable sliceAddCount = 0
      for xpdoc in documents do
        let wxp = new WikiXmlPage(xpdoc)
        let row = wxp.MakeArticleIndexRow(si)
        if row <> null then
          articleIndex.Put(row)
          sliceAddCount <- sliceAddCount + 1
      cp $"\r  Processed stream \fb{si}\f0: added \fc{sliceAddCount}\f0 rows."
    cp $"Saving \fc{articleIndex.Rows.Count}\f0 rows to \fg{Path.GetFileName(ais.FileName)}\f0 (in \fy{dump.ArticleIndexFolderName}\f0)"
    articleIndex.Save(ais.FileName)
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
    | "-n" :: count :: rest ->
      let n = count |> Int32.Parse
      if n < 1 then
        failwith "Invalid stream count (minimum is 1)"
      rest |> parseMore {o with StreamCount = n}
    | [] ->
      if o.WikiId.IsNone then
        cp "\frNo wikidump specified\f0 (Missing \fo-wiki\f0 argument. Use \fowikidata list\f0 to find valid values)"
        None
      else
        Some(o)
    | x :: _ ->
      cp $"\frError: unrecognized argument \f0'\fo{x}\f0'"
      None
  let oo = args |> parseMore {
    WikiId = None
    StreamCount = 1000
  }
  match oo with
  | Some(o) ->
    o |> runArtIdx
  | None ->
    Usage.usage "articleindex"
    1
