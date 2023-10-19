module AppDbg

open System
open System.IO

open Newtonsoft.Json
open ICSharpCode.SharpZipLib.BZip2

open WikiDataLib.Configuration
open WikiDataLib.Repository
open WikiDataLib.Utilities

open ColorPrint
open CommonTools

// wikidata dbg -wiki enwiki-20230920 -i 1


type private SectionIndex =
  | ByOffset of int64
  | ByIndex of int

type private DbgOptions = {
  Sections: SectionIndex list
  WikiId: WikiDumpId option  
}

let private runDbgSection idx o =
  0

let private runDbg o =
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
      let idx = subindex.FindRangeIndex(offset, false, false)
      if idx < 0 then
        // unexpected when exact = false
        failwith "Invalid offset (internal error)"
      let start = subindex.Offsets[idx]
      if start <> offset then
        failwith $"Offset {offset} is not valid. The next lower valid value is {start}"
      idx
    | ByIndex(idx0) ->
      let idx = if idx0 < 0 then idx0 + subindex.Count else idx0
      if idx <= 0 || idx >= subindex.Count-1 then
        failwith $"'{idx0}' is not a valid index. Expecting a value between -{subindex.Count-1} and {subindex.Count-2} (excluding 0 and -1)"
      idx
  for si in o.Sections do
    let idx = si |> sectionIndexToIndex
    let offset = subindex.Offsets[idx]
    let length = subindex.Lengths[idx]
    cpx $"Processing section \fb{idx}\f0 of \fg{wikiId}\f0 from \fc{offset}\f0 (\fC0x%08X{offset}\f0)"
    cp $" to \fc{offset+length-1L}\f0 (\fC0x%08X{offset+length-1L}\f0)."
    ()
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
      else if o.Sections |> List.isEmpty then
        cp "\frNo offsets (\fo-s\fr) or indices (\fo-i\fr) specified\f0."
        None
      else
        Some({o with Sections = o.Sections |> List.rev})
    | x :: _ ->
      cp $"\frUnrecognized argument\f0: \fo{x}\f0"
      None
  let oo = args |> parseMore {
    WikiId = None
    Sections = []
  }
  match oo with
  | None ->
    cp "\fmNo specific documentation for 'dbg' available\f0."
    Usage.usage "all"
    0
  | Some(o) ->
    o |> runDbg


