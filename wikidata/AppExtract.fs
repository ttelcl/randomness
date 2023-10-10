module AppExtract

open System
open System.IO

open Newtonsoft.Json

open WikiDataLib.Configuration
open WikiDataLib.Repository
open WikiDataLib.Utilities

open ColorPrint
open CommonTools

type private SectionIndex =
  | ByOffset of int64
  | ByIndex of int

type private ExtractOptions = {
  InfoMode: bool
  Raw: bool
  Wrap: bool
  WikiId: WikiDumpId option
  Sections: SectionIndex list
}

let private runExtract o =
  let repo = new WikiRepo()
  let wikiId =
    match o.WikiId with
    | Some(wid) -> wid
    | None -> failwith "No WikiDump selected"
  let dump = wikiId |> repo.GetDumpFolder
  if dump.HasStreamIndex |> not then
    failwith $"'{wikiId}' has no stream index yet (run 'wikidata index -wiki {wikiId}' to create it)"
  let subindex = dump.LoadIndex()
  let sectionIndexToOffset si =
    match si with
    | ByOffset(offset) ->
      if offset < 0L || offset > subindex.LastOffset then
        failwith $"offset out of range: {offset}. Expected range is 0 - {subindex.LastOffset}"
      let idx = subindex.FindRangeIndex(offset, false, true)
      if idx < 0 then
        // unexpected when exact = false
        failwith "Invalid offset (internal error)"
      let start = subindex.Offsets[idx]
      if start <> offset then
        failwith $"Offset {offset} is not valid. The next lower valid value is {start}"
      start
    | ByIndex(idx0) ->
      let idx = if idx0 < 0 then idx0 + subindex.Count else idx0
      if idx < 0 || idx >= subindex.Count then
        failwith $"'{idx0}' is not a valid index. Expecting a value between -{subindex.Count} and {subindex.Count-1}"
      subindex.Offsets[idx]
  let wrapOffsets =
    if o.Wrap then [| subindex.FirstOffset; subindex.LastOffset |] else [| |]
  let offsets =
    o.Sections
    |> Seq.map sectionIndexToOffset
    |> Seq.append wrapOffsets
    |> Set.ofSeq // remove duplicates
    |> Seq.sort // make sure it is sorted
    |> Seq.toList
  let offsetText o =
    if o = subindex.FirstOffset then
      "A"
    elif o = subindex.LastOffset then
      "Z"
    else
      $"p{o}"
  let offsetsTag = String.Join("-", offsets |> List.map offsetText)
  let outNameShort =
    if o.Raw then
      $"{wikiId}.{offsetsTag}.wiki.xml.bz2"
    else
      $"{wikiId}.{offsetsTag}.wiki.xml"
  let onm = outNameShort
  cp $"Output name: {onm}"
  use host = dump.OpenMainDump()
  match o.Raw with
  | true ->
    do
      use f = onm |> startFileBinary
      for offset in offsets do
        use segment = subindex.Open(host, offset, true, true)
        if segment = null then
          failwith $"Could not get range for ofset {offset} (internal error)"
        segment.CopyTo(f)
    onm |> finishFile
    ()
  | false ->
    cp "\frNot Yet Implemented\f0. \fg-raw\f0 is currently REQUIRED"
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
    | "-raw" :: rest ->
      rest |> parseMore {o with Raw = true}
    | "-wrap" :: rest ->
      rest |> parseMore {o with Wrap = true}
    | "-info" :: rest ->
      rest |> parseMore {o with InfoMode = true}
    | "-p" :: position :: rest ->
      let idx = position |> Int64.Parse |> SectionIndex.ByOffset
      rest |> parseMore {o with Sections = idx :: o.Sections}
    | "-i" :: index :: rest ->
      let idx = index |> Int32.Parse |> SectionIndex.ByIndex
      rest |> parseMore {o with Sections = idx :: o.Sections}
    | [] ->
      if o.WikiId.IsNone then
        cp "\frNo wikidump specified\f0 (Missing \fo-wiki\f0 argument. Use \fowikidata list\f0 to find valid values)"
        None
      else if o.Sections |> List.isEmpty && o.Wrap |> not then
        cp "\frNo offsets (\fo-p\fr), indices (\fo-i\fr) or wrap flag (\fo-wrap\fr) specified\f0."
        None
      else
        Some({o with Sections = o.Sections |> List.rev})
    | x :: _ ->
      cp $"\frUnrecognized argument\f0: \fo{x}\f0"
      None
  let oo = args |> parseMore {
    InfoMode = false
    Raw = false
    Wrap = false
    WikiId = None
    Sections = []
  }
  match oo with
  | None ->
    Usage.usage "extract"
    0
  | Some(o) ->
    o |> runExtract

