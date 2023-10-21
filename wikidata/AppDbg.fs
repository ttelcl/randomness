module AppDbg

open System
open System.IO
open System.Text

open Newtonsoft.Json
open ICSharpCode.SharpZipLib.BZip2

open WikiDataLib.Configuration
open WikiDataLib.Repository
open WikiDataLib.Utilities

open ColorPrint
open CommonTools
open System.Xml

// wikidata dbg -wiki enwiki-20230920 -i 1 -xml -xml+

type private SectionIndex =
  | ByOffset of int64
  | ByIndex of int

type private XmlDumpOption =
  | NoXml
  | Article
  | All

type private DbgOptions = {
  Sections: SectionIndex list
  WikiId: WikiDumpId option  
  DumpXml: XmlDumpOption
}

type WikiContext = {
  Dump: WikiDump
  SubIndex: SubstreamIndex
}

let private runDbgSection o context idx =
  let subindex = context.SubIndex
  let dump = context.Dump
  let offset = subindex.Offsets[idx]
  let length = subindex.Lengths[idx]
  cpx $"Processing slice \fb{idx}\f0 of \fg{dump.Id}\f0 from \fc{offset}\f0 (\fC0x%08X{offset}\f0)"
  cp $" to \fc{offset+length-1L}\f0 (\fC0x%08X{offset+length-1L}\f0)."
  // let sliceFolder = $"{dump.Id}-s%06d{idx}-x%08X{offset}"
  let sliceFolder = $"{dump.Id}-s%06d{idx}"
  let fld =
    let fld = Path.GetFullPath(sliceFolder)
    if fld |> Directory.Exists |> not then
      cp $"Slice folder \fc{sliceFolder}\f0  \focreated\f0."
      fld |> Directory.CreateDirectory |> ignore
    else
      cp $"Slice folder \fg{sliceFolder}\f0 (\fkexisting\f0)"
    fld
  let currentDir = Environment.CurrentDirectory
  try
    Environment.CurrentDirectory <- fld
    let mutable counter = 0
    cp $"Processing slice. Press \foCTRL-C\f0 to abort after current element"
    use host = dump.OpenMainDump()
    use slice = subindex.OpenConcatenation(host, [idx], true)
    let documents =
      slice
      |> WikiXml.ReadPageFragments
      |> Seq.takeWhile (fun _ -> canceled() |> not)
    for xpdoc in documents do
      let wxp = new WikiXmlPage(xpdoc)
      let titleparts = wxp.Title.Split(':', 2)
      let titleFormatted =
        match titleparts.Length with
        | 0 -> "\fr???\f0"
        | 1 -> $"\fy{titleparts[0]}\f0"
        | 2 -> $"\fo{titleparts[0]}\f0:\fy{titleparts[1]}\f0"
        | x -> failwith $"Unexpected split length from '{wxp.Title}' ({x})"
      let revisionId = wxp.RevisionId
      let contentSize = wxp.RequiredString("revision/text/@bytes") |> Int32.Parse
      let redirect = wxp.RedirectTitle
      let redirectText =
        if redirect = null then
          ""
        else
          $" -> [\f0{redirect}\f0]"
      cp $"\fb%02d{counter}\f0 \fcp%08d{wxp.Id}\f0 [{titleFormatted}]{redirectText} (\fk{contentSize}\f0)"
      let doXml =
        match o.DumpXml with
        | XmlDumpOption.NoXml -> false
        | XmlDumpOption.Article -> redirect = null
        | XmlDumpOption.All -> true
      if doXml then
        let shortName = $"{dump.Id}-p%08d{wxp.Id}.xml"
        let onm = Path.Combine(fld, shortName)
        if onm |> File.Exists then
          cp $"     {sliceFolder}/{shortName} : \fkalready exists\f0."
        else
          cp $"     \fc{sliceFolder}\f0/\fg{shortName}\f0 : \foSaving\f0."
          use xw = new XmlTextWriter(onm, Encoding.UTF8)
          xw.Formatting <- Formatting.Indented
          let nav = wxp.Doc.CreateNavigator()
          xw.WriteStartDocument()
          nav.WriteSubtree(xw)
          ()
      counter <- counter + 1
  finally
    Environment.CurrentDirectory <- currentDir
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
  let context = {
    Dump = dump
    SubIndex = subindex
  }
  for si in o.Sections do
    let idx = si |> sectionIndexToIndex
    let result = idx |> runDbgSection o context
    if result <> 0 then
      failwith $"Aborting because of error status '{result}'"
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
    | "-xml" :: rest 
    | "-xml-" :: rest ->
      rest |> parseMore {o with DumpXml = XmlDumpOption.Article}
    | "-xml+" :: rest ->
      rest |> parseMore {o with DumpXml = XmlDumpOption.All}
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
    DumpXml = XmlDumpOption.NoXml
  }
  match oo with
  | None ->
    cp "\fmNo specific documentation for 'dbg' available\f0."
    Usage.usage "all"
    0
  | Some(o) ->
    o |> runDbg


