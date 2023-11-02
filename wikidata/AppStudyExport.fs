module AppStudyExport

open System
open System.IO
open System.Text
open System.Xml

open XsvLib

open WikiDataLib.Repository
open WikiDataLib.WikiContent

open ColorPrint
open CommonTools

type private SearchCommand =
  | ByPage of int64
  | ByTitleText of string

type private ExtractLocation =
  | Local
  | WikiFolder

type ExportLimit =
  | MaxLimit of int
  | CapLimit of int
  | NoLimit

type private StudyExportOptions = {
  WikiId: WikiDumpId option
  Search: SearchCommand option
  WriteXml: bool
  WriteWikiText: bool
  WritePlainText: bool
  WriteWords: bool
  Location: ExtractLocation option
  Limit: ExportLimit
}

type ExportContext = {
  Dump: WikiDump
  SubIndex: SubstreamIndex
  WikiRoot: Wiki
  ExportFolder: string
}

let private searchIndexPage pageId (index: ArticleIndex) =
  pageId |> index.FindRowById |> Option.ofObj

let private searchDbPage pageId (articleDb: ArticleDb) =
  articleDb.Slices
  |> Seq.map(ArticleIndex.FromSlice)
  |> Seq.choose (searchIndexPage pageId)
  |> Seq.tryHead

let private searchIndexTitles text (index: ArticleIndex) =
  index.FindMatchingTitles(text, false)

let private searchDbTitles text (articleDb: ArticleDb) =
  articleDb.Slices
  |> Seq.map(ArticleIndex.FromSlice)
  |> Seq.collect (searchIndexTitles text)

let private searchDb command articleDb =
  match command with
  | SearchCommand.ByPage(pageId) ->
    articleDb |> searchDbPage pageId |> Option.toList
  | SearchCommand.ByTitleText(text) ->
    articleDb |> searchDbTitles text |> Seq.toList

let private exportPagePlain o context prefix (page: WikiXmlPage) =
  let wiki = context.WikiRoot
  let settings = wiki.ParseSettings
  let model = new WikiModel(settings, page.Content)
  if o.WritePlainText then
    let ptxName = prefix + ".plain.txt"
    cp $"Saving \fg{ptxName}\f0 in \fc{context.ExportFolder}\f0."
    File.WriteAllLines(Path.Combine(context.ExportFolder, ptxName), model.PlaintextLines(true))
  if o.WriteWords then
    //let wordsDbgName = prefix + ".words-dbg.txt"
    //cp $"Saving \fo{wordsDbgName}\f0."
    //File.WriteAllLines(wordsDbgName, model.EnumerateWords())
    let wordsName = prefix + ".words.csv"
    let fullName = Path.Combine(context.ExportFolder, wordsName)
    cp $"Saving \fg{wordsName}\f0 in \fc{context.ExportFolder}\f0."
    let wordMap = model.GatherWordCounts()
    use w = File.CreateText(fullName)
    w.WriteLine("word,count")
    let wordCounts =
      wordMap
      |> Seq.map(fun kvp -> (kvp.Key, kvp.Value))
      |> Seq.sortByDescending (fun (k,v) -> v)
      |> Seq.toArray
    for (k,v) in wordCounts do
      w.WriteLine($"{k},{v}")
    cp $"Word count: total \fb{wordCounts |> Seq.sumBy (fun (_,v) -> v)}\f0, distinct: \fy{wordCounts.Length}\f0 "
  ()

let private anyDumpsMissing context o (row: ArticleIndexRow) =
  let dump = context.Dump
  let slug = row.MakeSlug()
  let prefix = $"{dump.Id.WikiTag}.p%08d{row.PageId}.{slug}"
  let missingFileNames =
    [
      if o.WriteXml then Some(".xml") else None
      if o.WritePlainText then Some(".plain.txt") else None
      if o.WriteWords then Some(".words.csv") else None
      if o.WriteWikiText then Some(".wiki.txt") else None
    ]
    |> List.choose id
    |> List.map (fun suffix -> Path.Combine(context.ExportFolder, prefix + suffix))
    |> List.filter (fun fileName -> fileName |> File.Exists |> not)
  if missingFileNames.Length > 0 then
    true
  else
    // cp $"\foSkipping \fg{prefix}\f0: \fyAll files to extract already exist\f0"
    false

let private exportPage context o (row: ArticleIndexRow) =
  let dump = context.Dump
  let slug = row.MakeSlug()
  let prefix = $"{dump.Id.WikiTag}.p%08d{row.PageId}.{slug}"
  //let missingFileNames =
  //  [
  //    if o.WriteXml then Some(".xml") else None
  //    if o.WritePlainText then Some(".plain.txt") else None
  //    if o.WriteWords then Some(".words.csv") else None
  //    if o.WriteWikiText then Some(".wiki.txt") else None
  //  ]
  //  |> List.choose id
  //  |> List.map (fun suffix -> Path.Combine(context.ExportFolder, prefix + suffix))
  //  |> List.filter (fun fileName -> fileName |> File.Exists |> not)
  //if missingFileNames.Length = 0 then
  //  cp $"\foSkipping \fg{prefix}\f0: \fyAll files to extract already exist\f0"
  //  0
  //else
  let subindex = context.SubIndex
  use main = dump.OpenMainDump()
  let substream = subindex.OpenConcatenation(main, [row.StreamId], true)
  let pages =
    substream
    |> WikiXml.ReadPageFragments
    |> Seq.map (fun xpd -> new WikiXmlPage(xpd))
    |> Seq.filter (fun wxp -> wxp.Id = row.PageId)
    |> Seq.toArray
  if pages.Length = 0 then
    cp $"\frIndex Error\f0: \fopage {row.PageId}\fy not found in stream \fo{row.StreamId}\f0."
    1
  elif pages.Length > 1 then
    cp $"\frIndex Error\f0: \fopage {row.PageId}\fy found multiple times in stream \fo{row.StreamId}\f0???"
    1
  else
    let page = pages[0]
    if o.WriteXml then
      let xmlName = prefix + ".xml"
      cp $"Saving \fg{xmlName}\f0 in \fc{context.ExportFolder}\f0."
      let xws = new XmlWriterSettings()
      xws.Indent <- true
      use xw = XmlWriter.Create(Path.Combine(context.ExportFolder, xmlName), xws)
      xw.WriteStartDocument()
      let nav = page.Doc.CreateNavigator()
      nav.WriteSubtree(xw)
    if o.WriteWikiText then
      let wtxName = prefix + ".wiki.txt"
      let wtx = page.Content
      cp $"Saving \fg{wtxName}\f0 in \fc{context.ExportFolder}\f0."
      File.WriteAllText(Path.Combine(context.ExportFolder, wtxName), wtx)
    if o.WritePlainText || o.WriteWords then
      page |> exportPagePlain o context prefix
    0

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
    let results = articleDb |> searchDb command
    if results.Length = 0 then
      cp "\foNo matching records found\f0."
      0
    else
      cp $"Found \fb{results.Length}\f0 matching index records."
      let wiki = repo.FindWiki(dump.Id.WikiTag)
      let exportFolder =
        match o.Location with
        | Some(ExtractLocation.Local) ->
          Path.Combine(Environment.CurrentDirectory, $"{wiki.WikiTag}-exports")
        | Some(ExtractLocation.WikiFolder) ->
          wiki.ExportFolder
        | None ->
          failwith "Missing export location"
      if exportFolder |> Directory.Exists |> not then
        cp $"\fyCreating folder \fc{exportFolder}\f0."
        exportFolder |> Directory.CreateDirectory |> ignore
      let context = {
        Dump = dump
        SubIndex = subindex
        WikiRoot = wiki
        ExportFolder = exportFolder
      }
      let missingResults =
        results
        |> List.choose (fun result ->
          let missing = result |> anyDumpsMissing context o
          if missing then
            cpx $"  Page \fg%-8d{result.PageId}\f0 in stream \fb%-8d{result.StreamId}\f0" 
            cp $" \fy%-36s{result.Title}\f0 (\fb{result.ByteCount}\f0 bytes)."
            Some(result)
          else
            cpx $"  \fkPage \fG%-8d{result.PageId}\fk in stream \fB%-8d{result.StreamId}\fk" 
            cp $" \fo%-36s{result.Title}\fk (\fB{result.ByteCount}\fk bytes)\f0 (done)."
            None)
      if o.WritePlainText || o.WriteWikiText || o.WriteXml || o.WriteWords then
        if missingResults.Length = 0 then
          cp "\fyAll search results were exported already\f0."
          0
        else
          let targetResults =
            match o.Limit with
            | ExportLimit.MaxLimit(n) ->
              if missingResults.Length > n then
                cp $"Number if missing matches (\fb{missingResults.Length}\f0) exceeds maximum (\fc{n}\f0). \foSkipping downloads\f0!"
                []
              else
                missingResults
            | ExportLimit.CapLimit(n) ->
              if missingResults.Length > n then
                cp $"Number if missing matches (\fb{missingResults.Length}\f0) exceeds cap (\fc{n}\f0). \foCapping downloads\f0!"
                missingResults |> List.take n
              else
                missingResults
            | ExportLimit.NoLimit ->
              missingResults
          if targetResults.Length > 0 then
            if targetResults.Length > 1 then
              cp "\foNot implemented\f0: multiple downloads."
              0
            else
              targetResults[0] |> exportPage context o
          else
            1
      else
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
    | "-search" :: text :: rest ->
      let command = text |> SearchCommand.ByTitleText
      rest |> parseMore {o with Search = Some(command)}
    | "-text" :: rest ->
      rest |> parseMore {o with WriteWikiText = true}
    | "-plain" :: rest ->
      rest |> parseMore {o with WritePlainText = true}
    | "-words" :: rest ->
      rest |> parseMore {o with WriteWords = true}
    | "-xml" :: rest ->
      rest |> parseMore {o with WriteXml = true}
    | "-w" :: rest ->
      rest |> parseMore {o with Location = Some(ExtractLocation.WikiFolder)}
    | "-l" :: rest ->
      rest |> parseMore {o with Location = Some(ExtractLocation.Local)}
    | "-max" :: n :: rest ->
      rest |> parseMore {o with Limit = n |> Int32.Parse |> ExportLimit.MaxLimit}
    | "-cap" :: n :: rest ->
      rest |> parseMore {o with Limit = n |> Int32.Parse |> ExportLimit.CapLimit}
    | "-all" :: rest ->
      rest |> parseMore {o with Limit = ExportLimit.NoLimit}
    | [] ->
      if o.Search.IsNone then
        cp "\frNo page select option specified\f0."
        None
      elif o.Location.IsNone then
        cp "\frNo write location specified\f0 (\fo-l\f0 or \fo-w\f0)"
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
    WriteWords = false
    Location = None
    Limit = ExportLimit.MaxLimit(1)
  }
  match oo with
  | Some(o) ->
    runExport o
  | None ->
    Usage.usage "study"
    0

