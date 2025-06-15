module AppGenerate

open System
open System.IO

open RandomUtilities.CharacterDistributions
open RandomUtilities.WordLists
open RandomUtilities.ByteSources

open ColorPrint
open CommonTools

type private GenerateOptions = {
  RepeatCount: int
  SourceFile: string
  MinLength: int
}

let private runGenerate o =
  let randombits = ByteSource.Random().Buffered().ToBitSource()
  let dist = 
    File.ReadAllText(o.SourceFile)
    |> LetterDistributionDto.FromJson
    |> LetterDistribution.FromDto
  if dist.Order < 1 then
    failwith "The minimum supported distribution order is 1"
  let words =
    Seq.initInfinite (fun i -> dist.RandomWord(randombits))
    |> Seq.filter (fun s -> s.Length >= o.MinLength)
    |> Seq.truncate o.RepeatCount
  for word in words do
    cp $"\fg{word}\f0"
  0

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | "-n" :: repeatText :: rest ->
      rest |> parseMore {o with RepeatCount = repeatText |> Int32.Parse}
    | "-m" :: minLength :: rest ->
      rest |> parseMore {o with MinLength = minLength |> Int32.Parse}
    | "-f" :: fileName :: rest ->
      if fileName |> File.Exists |> not then
        failwith $"File not found: {fileName}"
      if ".word-fragments.json" |> fileName.EndsWith |> not then
        failwith $"Expecting file name to end with '.word-fragments.json'"
      rest |> parseMore {o with SourceFile = fileName}
    | [] ->
      if o.SourceFile |> String.IsNullOrEmpty then
        failwith "No file name specified"
      Some(o)
    | x :: _ ->
      cp $"\frUnrecognized argument \fo{x}\f0"
      None
  let oo = args |> parseMore {
    RepeatCount = 1
    SourceFile = null
    MinLength = 4
  }
  match oo with
  | Some(o) -> 
    o |> runGenerate
  | None ->
    Usage.usage "generate"
    1
