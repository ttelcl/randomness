module AppGenerate

open System
open System.IO

open RandomUtilities.ByteSources
open RandomUtilities.CharacterDistributions
open RandomUtilities.WordCounts

open ColorPrint
open CommonTools

type private GenerateOptions = {
  RepeatCount: int
  SourceFile: string
  MinLength: int
  MinBits: float
  MaxBits: float
  MinLetterBits: float
  BlackList: WordCountMap
  ShowInfo: bool
}

let private wordFilter o ((s: string),f) =
  let len = s.Length
  let letterBits = f / float(len)
  len >= o.MinLength
  && f >= o.MinBits
  && f <= o.MaxBits
  && letterBits >= o.MinLetterBits
  && o.BlackList[s] = 0

let private runGenerate o =
  let randombits = ByteSource.Random().Buffered().ToBitSource()
  let dist = 
    File.ReadAllText(o.SourceFile)
    |> LetterDistributionDto.FromJson
    |> LetterDistribution.FromDto
  if dist.Order < 1 then
    failwith "The minimum supported distribution order is 1"
  let wordpairs =
    Seq.initInfinite (fun i -> dist.RandomWord(randombits))
    |> Seq.filter (wordFilter o)
    |> Seq.truncate o.RepeatCount
  for (word,surprisal) in wordpairs do
    let surprisalPerLetter = surprisal / float(word.Length)
    cpx $"\fg{word,-20}\f0"
    if o.ShowInfo then
      cp $" (\fb{surprisal:F1}\f0 bits, \fc{surprisalPerLetter:F2}\f0 per letter)"
    else
      cp ""
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
    | "-ml" :: minLength :: rest ->
      rest |> parseMore {o with MinLength = minLength |> Int32.Parse}
    | "-mb" :: minBits :: rest ->
      rest |> parseMore {o with MinBits = minBits |> Double.Parse}
    | "-mbl" :: minBits :: rest ->
      rest |> parseMore {o with MinLetterBits = minBits |> Double.Parse}
    | "-xb" :: maxBits :: rest ->
      rest |> parseMore {o with MaxBits = maxBits |> Double.Parse}
    | "-bl" :: fileName :: rest ->
      if fileName |> File.Exists then
        o.BlackList.AddFile(fileName, false)
        rest |> parseMore o
      else
        cp $"\foFile not found\f0: {fileName}"
        None
    | "-info" :: rest ->
      rest |> parseMore {o with ShowInfo = true}
    | "-f" :: fileName :: rest ->
      if fileName |> File.Exists |> not then
        failwith $"File not found: {fileName}"
      if ".word-fragments.json" |> fileName.EndsWith |> not then
        failwith $"Expecting file name to end with '.word-fragments.json'"
      rest |> parseMore {o with SourceFile = fileName}
    | [] ->
      if o.SourceFile |> String.IsNullOrEmpty then
        failwith "No file name specified"
      if o.MinBits >= o.MaxBits then
        failwith "-mb must be smaller than -xb (they should not be too close)"
      Some(o)
    | x :: _ ->
      cp $"\frUnrecognized argument \fo{x}\f0"
      None
  let oo = args |> parseMore {
    RepeatCount = 1
    SourceFile = null
    MinLength = 4
    MinBits = 10.0
    MinLetterBits = 0.0
    MaxBits = 60.0
    BlackList = new WordCountMap()
    ShowInfo = false
  }
  match oo with
  | Some(o) -> 
    o |> runGenerate
  | None ->
    Usage.usage "generate"
    1
