module AppGenerate

open System
open System.IO

open RandomUtilities.ByteSources
open RandomUtilities.CharacterDistributions
open RandomUtilities.WordCounts

open ColorPrint
open CommonTools

type private PhraseMode =
  | Word
  | Phrase of MinTotalBits: float

type private GenerateOptions = {
  RepeatCount: int
  SourceFile: string
  MinLength: int
  MinBits: float
  MaxBits: float
  MinLetterBits: float
  BlackList: WordCountMap
  ShowInfo: bool
  Phrasing: PhraseMode
  Capitalize: bool
}

type private PhraseBuildState = {
  PhraseList: string list
  TotalBits: float
}

let private phraseBuildStartState = {
  PhraseList=[]
  TotalBits=0.0
}

let capitalize (word: string) =
  let firstLetter = word[0] |> Char.ToUpperInvariant
  firstLetter.ToString() + word.Substring(1)

let private phraseBuildStep minBits builder (word, bits) =
  let newState = {
    builder with
      PhraseList = word :: builder.PhraseList
      TotalBits = builder.TotalBits + bits}
  if newState.TotalBits >= minBits then
    phraseBuildStartState, Some(newState)
  else
    newState, None

let private phraseGenerator minBits source =
  source
  |> Seq.scan
    (fun (builder, _) t -> phraseBuildStep minBits builder t)
    (phraseBuildStartState, None)
  |> Seq.choose (fun (_, output) -> output)

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
  let wordpairStream = // infinite stream of words matching the settings
    Seq.initInfinite (fun i -> dist.RandomWord(randombits))
    |> Seq.filter (wordFilter o)
  match o.Phrasing with
  | PhraseMode.Word ->
    let wordpairs =
      wordpairStream
      |> Seq.truncate o.RepeatCount
    for (word,surprisal) in wordpairs do
      let surprisalPerLetter = surprisal / float(word.Length)
      let word =
        if o.Capitalize then
          word |> capitalize
        else
          word
      if o.ShowInfo then
        cpx $"\fg{word,-20}\f0"
        cp $" (\fb{surprisal:F1}\f0 bits, \fc{surprisalPerLetter:F2}\f0 per letter)"
      else
        cp $"\fg{word}\f0"
    0
  | PhraseMode.Phrase(phraseBits) ->
    let phrases =
      wordpairStream
      |> phraseGenerator phraseBits
      |> Seq.truncate o.RepeatCount
    for phrase in phrases do
      let words =
        if o.Capitalize then
          match phrase.PhraseList with
          | head :: tail ->
            (head |> capitalize) :: tail
          | [] -> []
        else
          phrase.PhraseList
      let phraseLine = String.Join("\fy-\fg", words)
      if o.ShowInfo then
        cp $"(\fb{phrase.TotalBits:F1}\f0 bits) \t\fg{phraseLine}\f0"
      else
        cp $"\fg{phraseLine}\f0"
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
    | "-C" :: rest ->
      rest |> parseMore {o with Capitalize = true}
    | "-bits" :: totalBitsText :: rest ->
      let totalBits = totalBitsText |> Double.Parse
      rest |> parseMore {o with Phrasing = PhraseMode.Phrase(totalBits)}
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
    Phrasing = PhraseMode.Word
    Capitalize = false
  }
  match oo with
  | Some(o) -> 
    o |> runGenerate
  | None ->
    Usage.usage "generate"
    1
