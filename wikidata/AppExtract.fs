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
  let sectionIndexToIndex si =
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
      idx
    | ByIndex(idx0) ->
      let idx = if idx0 < 0 then idx0 + subindex.Count else idx0
      if idx < 0 || idx >= subindex.Count then
        failwith $"'{idx0}' is not a valid index. Expecting a value between -{subindex.Count} and {subindex.Count-1}"
      idx
  let lastIndex = subindex.Count-1
  let wrapIndices =
    if o.Wrap then [| 0; lastIndex |] else [| |]
  let indices =
    o.Sections
    |> Seq.map sectionIndexToIndex
    |> Seq.append wrapIndices
    |> Set.ofSeq // remove duplicates
    |> Seq.sort // make sure it is sorted
    |> Seq.toArray
  let wrapped =
    indices.Length >= 2 && 
    indices[0] = 0 &&
    indices[indices.Length-1] = lastIndex
  let indexText o =
    if wrapped && o = 0 then
      "("
    elif wrapped && o = lastIndex then
      ")"
    else
      $"p{o}"
  let indicesTag = 
    String.Join("-", indices |> Array.map indexText)
      .Replace("(-", "(")
      .Replace("-)", ")")
  let outNameShort =
    if o.Raw then
      $"{wikiId}.{indicesTag}.wiki.xml.bz2"
    else
      $"{wikiId}.{indicesTag}.wiki.xml"
  let onm = outNameShort
  cp $"Output name: {onm}"
  use host = dump.OpenMainDump()
  match o.Raw with
  | true ->
    do
      use f = onm |> startFileBinary
      for index in indices do
        use segment = subindex.OpenIndex(host, index)
        if segment = null then
          failwith $"Could not get range for index {index} (internal error)"
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
    | "-nowrap" :: rest ->
      rest |> parseMore {o with Wrap = false}
    | "-info" :: rest ->
      rest |> parseMore {o with InfoMode = true}
    | "-s" :: position :: rest ->
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
    Wrap = true
    WikiId = None
    Sections = []
  }
  match oo with
  | None ->
    Usage.usage "extract"
    0
  | Some(o) ->
    o |> runExtract

