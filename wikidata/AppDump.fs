module AppDump

open System
open System.IO
open System.Text
open System.Xml

open XsvLib

open WikiDataLib.Repository

open ColorPrint
open CommonTools

// wikidata dbg -wiki enwiki-20230920 -i 1 -xml -xml+

type private SectionIndex =
  | ByOffset of int64
  | ByIndex of int

type private DumpOption =
  | NoDump
  | Article
  | All

type private DbgOptions = {
  Sections: SectionIndex list
  WikiId: WikiDumpId option  
  DumpXml: DumpOption
  DumpText: DumpOption
  AllNamespaces: bool
  SaveIndex: bool
}

type WikiContext = {
  Dump: WikiDump
  SubIndex: SubstreamIndex
}

let private runDumpSection o context idx =
  let subindex = context.SubIndex
  let dump = context.Dump
  let offset = subindex.Offsets[idx]
  let length = subindex.Lengths[idx]
  cpx $"Processing slice \fb{idx}\f0 of \fg{dump.Id}\f0 from \fc{offset}\f0 (\fC0x%08X{offset}\f0)"
  cp $" to \fc{offset+length-1L}\f0 (\fC0x%08X{offset+length-1L}\f0)."
  // let sliceFolder = $"{dump.Id}-s%06d{idx}-x%08X{offset}"
  let sliceFolder = $"{dump.Id}-i%06d{idx}"
  let fld =
    let fld = Path.GetFullPath(sliceFolder)
    if fld |> Directory.Exists |> not then
      cp $"Slice folder \fc{sliceFolder}\f0  \focreated\f0."
      fld |> Directory.CreateDirectory |> ignore
    else
      cp $"Slice folder \fg{sliceFolder}\f0 (\fkexisting\f0)"
    fld
  let articleIndex = new ArticleIndex()
  let currentDir = Environment.CurrentDirectory
  try
    Environment.CurrentDirectory <- fld
    cp $"Processing slice. Press \foCTRL-C\f0 to abort after current element"
    use host = dump.OpenMainDump()
    use slice = subindex.OpenConcatenation(host, [idx], true)
    let documents =
      slice
      |> WikiXml.ReadPageFragments
      |> Seq.takeWhile (fun _ -> canceled() |> not)
    for xpdoc in documents do
      let wxp = new WikiXmlPage(xpdoc)
      let nsid = wxp.NamespaceId
      let redirect = wxp.RedirectTitle
      let redirectText = if redirect = null then "" else $" -> [\f0{redirect}\f0]"
      let titleColor = if redirect = null then "\fy" else "\fk"
      let shortXmlName = $"{dump.Id}-p%08d{wxp.Id}.xml"
      let xmlName = Path.Combine(fld, shortXmlName)
      let xmlMissing = xmlName |> File.Exists |> not
      let xmlTag = if xmlMissing then "\fk-xml\f0" else "\fb+xml\f0"
      let shortTextName = $"{dump.Id}-p%08d{wxp.Id}.wiki.txt"
      let textName = Path.Combine(fld, shortTextName)
      let textMissing = textName |> File.Exists |> not
      let textTag = if textMissing then "\fk-txt\f0" else "\fb+txt\f0"
      let titleparts = wxp.Title.Split(':', 2)
      let titleFormatted =
        match titleparts.Length with
        | 0 -> "\fr???\f0"
        | 1 -> $"{titleColor}{titleparts[0]}\f0"
        | 2 -> $"\fo{titleparts[0]}\f0:{titleColor}{titleparts[1]}\f0"
        | x -> failwith $"Unexpected split length from '{wxp.Title}' ({x})"
      cp $"\fcp%08d{wxp.Id}\f0 {xmlTag} {textTag} [{titleFormatted}]{redirectText}"
      let doNs = nsid = 0 || o.AllNamespaces
      let doXml =
        match o.DumpXml with
        | DumpOption.NoDump -> false
        | DumpOption.Article -> redirect = null && xmlMissing && doNs
        | DumpOption.All -> xmlMissing && doNs
      if doXml then
        cp $"     \fc{sliceFolder}\f0/\fg{shortXmlName}\f0 : \foSaving XML\f0."
        use xw = new XmlTextWriter(xmlName, Encoding.UTF8)
        xw.Formatting <- Formatting.Indented
        let nav = wxp.Doc.CreateNavigator()
        xw.WriteStartDocument()
        nav.WriteSubtree(xw)
      let doText =
        match o.DumpText with
        | DumpOption.NoDump -> false
        | DumpOption.Article -> redirect = null && textMissing && doNs
        | DumpOption.All -> textMissing && doNs
      if doText then
        cp $"     \fc{sliceFolder}\f0/\fg{shortTextName}\f0 : \foSaving WikiText\f0."
        let text = wxp.RequiredString("revision/text")
        File.WriteAllText(textName, text)
      if o.SaveIndex then
        let row = wxp.MakeArticleIndexRow(idx)
        if row <> null then
          articleIndex.Put(row)
      ()
    if o.SaveIndex then
      let indexname = $"{dump.Id}-i%06d{idx}.articles.csv"
      do
        use indexfile = indexname |> startFile
        let buffer = new ArticleIndexRowBuffer()
        let itrw = Xsv.WriteXsv(indexfile, indexname, buffer.Count)
        buffer.WriteXsv(itrw, articleIndex.Rows, true)
        ()
      indexname |> finishFile
  finally
    Environment.CurrentDirectory <- currentDir
  0

let private runDump o =
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
    let result = idx |> runDumpSection o context
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
    | "-pos" :: position :: rest
    | "-off" :: position :: rest
    | "-offset" :: position :: rest
    | "-s" :: position :: rest ->
      let idx = position |> Int64.Parse |> SectionIndex.ByOffset
      rest |> parseMore {o with Sections = idx :: o.Sections}
    | "-i" :: index :: rest ->
      let idx = index |> Int32.Parse |> SectionIndex.ByIndex
      rest |> parseMore {o with Sections = idx :: o.Sections}
    | "-xml" :: rest 
    | "-xml-" :: rest ->
      rest |> parseMore {o with DumpXml = DumpOption.Article}
    | "-xml+" :: rest ->
      rest |> parseMore {o with DumpXml = DumpOption.All}
    | "-txt" :: rest 
    | "-txt-" :: rest 
    | "-text" :: rest 
    | "-text-" :: rest ->
      rest |> parseMore {o with DumpText = DumpOption.Article}
    | "-txt+" :: rest 
    | "-text+" :: rest ->
      rest |> parseMore {o with DumpText = DumpOption.All}
    | "-allns" :: rest ->
      rest |> parseMore {o with AllNamespaces = true}
    | "-index" :: rest ->
      rest |> parseMore {o with SaveIndex = true}
    | [] ->
      if o.WikiId.IsNone then
        cp "\frNo wikidump specified\f0 (Missing \fo-wiki\f0 argument. Use \fowikidata list\f0 to find valid values)"
        None
      else if o.Sections |> List.isEmpty then
        cp "\frNo offsets (\fo-pos\fr) or indices (\fo-i\fr) specified\f0."
        None
      else
        Some({o with Sections = o.Sections |> List.rev})
    | x :: _ ->
      cp $"\frUnrecognized argument\f0: \fo{x}\f0"
      None
  let oo = args |> parseMore {
    WikiId = None
    Sections = []
    DumpXml = DumpOption.NoDump
    DumpText = DumpOption.NoDump
    AllNamespaces = false
    SaveIndex = false
  }
  match oo with
  | None ->
    Usage.usage "dump"
    0
  | Some(o) ->
    o |> runDump


