module AppIndex

open System
open System.IO

open Newtonsoft.Json

open WikiDataLib.Configuration
open WikiDataLib.Repository
open WikiDataLib.Utilities

open ColorPrint
open CommonTools

type private DumpSource =
  | DumpFolder of WikiDumpId
  | DumpFile of string // for debug purposes

type private IndexOptions = {
  Source: DumpSource option
  StreamCount: int
  Offset: int64
}

type private StreamStart = {
  Offset: int64
  Compression: int
}

let private runIndex o =
  let sourceFile, targetFile =
    match o.Source with
    | Some(DumpSource.DumpFolder(wdi)) ->
      let repo = new WikiRepo()
      let dumpFolder = wdi |> repo.GetDumpFolder
      if dumpFolder.HasMainFile |> not then
        failwith $"Data file missing: {dumpFolder.MainDumpFileName}"
      if dumpFolder.HasStreamIndex then
        failwith $"Stream index already exists: {dumpFolder.StreamIndexFileName}"
      dumpFolder.MainDumpFileName, dumpFolder.StreamIndexFileName
    | Some(DumpSource.DumpFile(file)) ->
      let file = file |> Path.GetFullPath
      if file |> File.Exists |> not then
        failwith $"File not found: {file}"
      let wdi = WikiDumpId.TryFromFile(file)
      if wdi = null then
        failwith "Expecting a file name starting with a wiki dump tag"
      let targetShort = $"{wdi}.dbg.index.csv"
      let targetDir = file |> Path.GetDirectoryName
      let target = Path.Combine(targetDir, targetShort)
      file, target
    | None ->
      failwith "No source provided"
  cp $"\fg{sourceFile}\f0 ->"
  cp $"  -> \fc{targetFile}\f0"
  use source = sourceFile |> File.OpenRead
  let total = source.Length
  let mutable reportOffset = 0L
  do
    use w = targetFile |> startFile
    "offset,length,from_hex" |> w.WriteLine
    let statemachine = new Bz2SubstreamStatemachine()
    let startPairs =
      source
      |> statemachine.FindStarts
      |> Seq.map (fun sm -> {Compression = sm.CompressionLevel; Offset = sm.SubstreamStart})
      |> Seq.pairwise
      |> Seq.truncate o.StreamCount
    for (s1, s2) in startPairs do
      $"%d{s1.Offset},%d{s2.Offset-s1.Offset},%010X{s1.Offset}" |> w.WriteLine
      if s1.Offset >= reportOffset then
        let percentage = (s1.Offset * 100L) / total
        cpx $"\r\fb%4d{percentage}%%\f0 \fg%010X{s1.Offset}\f0.   "
        reportOffset <- ((percentage + 1L) * total) / 100L
        ()
    // add the final block
    let s1o = statemachine.SubstreamStart
    let s2o = statemachine.Position
    $"%d{s1o},%d{s2o-s1o},%010X{s1o}" |> w.WriteLine
    cp ""
    let percentage = (s2o * 100L) / total
    cp $"Final: \fb%4d{percentage}%%\f0 \fg%010X{s1o}\f0\f0."
  targetFile |> finishFile
  0

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | [] ->
      if o.Source.IsNone then
        None
      else
        Some(o)
    | "-wiki" :: dumpid :: rest
    | "-dump" :: dumpid :: rest ->
      let wdi = dumpid |> WikiDumpId.Parse
      rest |> parseMore {o with Source = Some(DumpSource.DumpFolder(wdi))}
    | "-file" :: file :: rest ->
      rest |> parseMore {o with Source = Some(DumpSource.DumpFile(file))}
    | "-n" :: count :: rest ->
      rest |> parseMore {o with StreamCount = count |> Int32.Parse}
    | "-offset" :: offset :: rest ->
      rest |> parseMore {o with Offset = offset |> Int64.Parse}
    | x :: _ ->
      cp $"\frUnrecognized argument:\f0 '\fo{x}\f0'"
      None
  let oo = args |> parseMore {
    Source = None
    StreamCount = Int32.MaxValue
    Offset = 0L
  }
  match oo with
  | Some(o) ->
    o |> runIndex
  | None ->
    Usage.usage "index"
    1
  
