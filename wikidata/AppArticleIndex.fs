module AppArticleIndex

open System
open System.IO
open System.Text
open System.Xml

open XsvLib

open WikiDataLib.Repository

open ColorPrint
open CommonTools

type private ArtIdxSubcommand =
  | Update of int
  | Chunk of int


type private ArtIdxOptions = {
  WikiId: WikiDumpId option
  Subcommand: ArtIdxSubcommand option
  Repeat: int
}

type WikiContext = {
  Dump: WikiDump
  SubIndex: SubstreamIndex
}

type private SliceGroupState = {
  Accu: ArticleIndexSlice list
  AccuCount: int
  Result: ArticleIndexSlice list list
}

let private groupSlices streamcount slices =
  let slicegroupfolder state (slice: ArticleIndexSlice) =
    let slicelength = slice.EndIndex - slice.StartIndex + 1
    let totalcount = slicelength + state.AccuCount
    if totalcount <= streamcount || state.AccuCount = 0 then
      // accu list is empty or has space to grow with the current slice
      let nextAccu = slice :: state.Accu
      {state with Accu = nextAccu; AccuCount = totalcount}
    else
      // accu list is not empty and would grow too large if further extended
      let group = state.Accu |> List.rev
      let nextResult = group :: state.Result
      {state with Accu = [ slice ]; AccuCount = slicelength; Result = nextResult}
  let folded =
    slices |> List.fold slicegroupfolder {
      Accu = []
      AccuCount = 0
      Result = []
    }
  if folded.Accu |> List.isEmpty then
    folded.Result |> List.rev
  else
    let group = folded.Accu |> List.rev
    group :: folded.Result |> List.rev

let private runArtIdxChunk context streamCount =
  let dump = context.Dump
  let slices = dump.ArticleIndexSlices() |> Seq.sortBy (fun s -> s.StartIndex) |> Seq.toList
  for (slice1, slice2) in slices |> List.pairwise do // validate
    if slice1.EndIndex + 1 <> slice2.StartIndex then
      cpx $"\frError!\f0 Malformed article index. Expecting slices \fc{slice1.StartIndex}\f0-\fc{slice1.EndIndex}\f0"
      cp $" and \fc{slice2.StartIndex}\f0-\fc{slice2.EndIndex}\f0 to be contiguous"
  match slices.Length with
  | 0 ->
    cp "\foThe article index is empty. No chunks to merge ...\f0"
    0
  | 1 ->
    cp "\foThe article index has only one entry. No chunks to merge ...\f0"
    0
  | _ ->
    let groupedSlices = slices |> groupSlices streamCount
    for slicelist in groupedSlices do
      cpx "["
      for slice in slicelist do
        cpx $" \fc{slice.StartIndex}\f0-\fc{slice.EndIndex}\f0"
      cp " ]"
      if slicelist.Length < 2 then
        cp "   \fkNo merge required\f0."
      else
        cpx $"   \fyMerging\f0 ...   "
        let combined = dump.MergeArticleIndexSlices(slicelist)
        cp $"\r   Merged to \fg{combined.FileName |> Path.GetFileName}\f0"
    0

let private runArtIdxUpdate context streamCount =
  let dump = context.Dump
  let subindex = context.SubIndex
  let firstStreamIndex = dump.NextArticleIndexStream();
  let lastStreamIndex = firstStreamIndex + streamCount - 1
  let lastStreamIndex =
    if lastStreamIndex >= subindex.Count-1 then subindex.Count-2 else lastStreamIndex
  if firstStreamIndex > lastStreamIndex then
    cp $"\fgThe article index appears to be complete\f0!"
    0
  else
    cp $"Processing streams \fb{firstStreamIndex}\f0 - \fb{lastStreamIndex}\f0:"
    let articleIndex = new ArticleIndex();
    let ais = new ArticleIndexSlice(dump.ArticleIndexFolderName, dump.Id, firstStreamIndex, lastStreamIndex)
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

let private runArtIdx o =
  let repo = new WikiRepo()
  let wikiId =
    match o.WikiId with
    | Some(wid) -> wid
    | None -> o.WikiId |> WikiUtils.resolveWiki
  let dump = wikiId |> repo.GetDumpFolder
  if dump.HasStreamIndex |> not then
    failwith $"'{wikiId}' has no stream index yet (run 'wikidata index -wiki {wikiId}' to create it)"
  let subindex = dump.LoadIndex()
  let context = {
    SubIndex = subindex
    Dump = dump
  }
  match o.Subcommand with
    | None ->
      cp "\frError: expecting one of\f0: \fo-n\f0, \fo-N\f0, \fo-chunk\f0"
      1
    | Some(Update(streamCount)) ->
      if o.Repeat > 1 then
        cp $"Repeating \fb{o.Repeat}\f0 times."
        cp $"Press \frCTRL-C\f0 to abort after the current repetition."
      let status = 
        seq { 1 .. o.Repeat}
        |> Seq.takeWhile (fun i -> canceled() |> not)
        |> Seq.map (fun i -> runArtIdxUpdate context streamCount)
        |> Seq.last
      if o.Repeat > 1 then
        if canceled() then
          cp $"\foCanceled\f0 before completion (intermediate repetitions saved)."
        else
          cp $"All repetions \fgcompleted\f0 (no CTRL-C detected)."
      status
    | Some(Chunk(streamCount)) ->
      runArtIdxChunk context streamCount

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
      if o.Subcommand.IsSome then
        failwith "'-n', '-N' and '-chunk' are mutually exclusive"
      rest |> parseMore {o with Subcommand = Some(Update(n))}
    | "-N" :: rest ->
      let rest2 = "-n" :: "1000" :: rest
      rest2 |> parseMore o
    | "-chunk" :: count :: rest ->
      if o.Subcommand.IsSome then
        failwith "'-chunk', '-n' and '-N' are mutually exclusive"
      let n = count |> Int32.Parse
      if n < 1 then
        failwith "Invalid stream count (minimum is 1)"
      rest |> parseMore {o with Subcommand = Some(Chunk(n))}
    | "-repeat" :: count :: rest ->
      rest |> parseMore {o with Repeat = count |> Int32.Parse}
    | [] ->
        Some(o)
    | x :: _ ->
      cp $"\frError: unrecognized argument \f0'\fo{x}\f0'"
      None
  let oo = args |> parseMore {
    WikiId = None
    Subcommand = None
    Repeat = 1
  }
  match oo with
  | Some(o) ->
    o |> runArtIdx
  | None ->
    Usage.usage "articleindex"
    1
