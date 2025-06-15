module AppGather

open System
open System.IO

open RandomUtilities.CharacterDistributions
open RandomUtilities.WordLists

open ColorPrint
open CommonTools

type WeightedWord = {
  Word: string
  Weight: int
}

type WordSource =
  | ListSource of string
  | WordCountFile of string * bool // filename * useweight

type private GatherOptions = {
  Order: int
  AlphabetSpec: string
  Boundary: char
  Sources: WordSource list
  OutTag: string
  AllowShort: bool
}

let private enumerateWordsInSource wordSource =
  match wordSource with
  | ListSource(wordlistLabel) ->
    let wlc = 
      WordListCache.Create()
        .AddApplicationFolder("WordLists")
    let wl = wlc.FindList(wordlistLabel, wlc)
    if wl = null then
      failwith $"Unknown word list or invalid word list expression: '{wordlistLabel}'"
    wl.Words |> Seq.map (fun s -> {Word = s; Weight = 1})
  | WordCountFile(fileName, useWeight) ->
    let wordWeightPairs =
      fileName
      |> File.ReadLines
      |> Seq.skip 1 // skip header
      |> Seq.map (fun line -> line.Split(','))
    if useWeight then
      wordWeightPairs
      |> Seq.filter (fun a -> a.Length >= 2)
      |> Seq.map (fun pair -> {Word = pair[0]; Weight = pair[1] |> Int32.Parse})
    else
      wordWeightPairs
      |> Seq.filter (fun a -> a.Length >= 1)
      |> Seq.map (fun pair -> {Word = pair[0]; Weight = 1})

let private runGather o =
  let alphabet = new Alphabet(o.AlphabetSpec, o.Boundary)
  let collector = new FragmentDistribution(alphabet, o.Order)
  for source in o.Sources do
    let words =
      source
      |> enumerateWordsInSource
      |> Seq.filter (fun ww -> alphabet.IsRepresentable(ww.Word, false))
    let words =
      if o.AllowShort then
        words
      else
        words |> Seq.filter (fun ww -> ww.Word.Length >= o.Order)
    for ww in words do
      collector.AddWord(ww.Word, ww.Weight)
  if collector.Total < 1 then
    cp "\frNo acceptable words found in the source(s)\f0."
    1
  else
    cp $"Added \fb{collector.Total}\f0 fragments"
    let onm1 = $"{o.OutTag}.wordstats-{o.Order}.info.csv"
    do
      use w1 = onm1 |> startFile
      fprintfn w1 "prefix,letter,count,fraction,entropy,surprisal"
      for kvp in collector.AllDistributions() |> Seq.sortBy (fun kvp -> kvp.Key) do
        let prefix = kvp.Key
        let acd = kvp.Value
        let values = acd.Distribution
        let entropy = acd.CalculateEntropy()
        for idx in 0..(alphabet.CharacterCount-1) do
          let value = values[idx]
          if value > 0 then
            let letter = alphabet.Characters[idx]
            let surprisalValue = acd.Surprisal(idx)
            let surprisalText = if surprisalValue.HasValue then $"%.5f{surprisalValue.Value}" else ""
            let fraction = float(value) / float(acd.Total)
            fprintfn w1 $"{prefix},{letter},{value},%.5f{fraction},%.5f{entropy},{surprisalText}"
    onm1 |> finishFile
    let onm2 = $"{o.OutTag}.fragments-{o.Order}.csv"
    do
      use w2 = onm2 |> startFile
      collector.SaveFragmentCsv(w2)
    onm2 |> finishFile
    let onm3 = $"{o.OutTag}.{o.Order}.word-fragments.json"
    do
      use w3 = onm3 |> startFile
      let dto = collector.ToLetterDistibutionDto()
      let json = dto.ToJson(true)
      w3.WriteLine(json)
    onm3 |> finishFile
    0

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | "-words" :: label :: rest ->
      let lst = WordSource.ListSource(label)
      rest |> parseMore {o with Sources = lst :: o.Sources}
    | "-wc" :: fnm :: rest ->
      if fnm |> File.Exists |> not then
        failwith $"No such file: {fnm}"
      let src = WordSource.WordCountFile(fnm, false)
      rest |> parseMore {o with Sources = src :: o.Sources}
    | "-wcw" :: fnm :: rest ->
      if fnm |> File.Exists |> not then
        failwith $"No such file: {fnm}"
      let src = WordSource.WordCountFile(fnm, true)
      rest |> parseMore {o with Sources = src :: o.Sources}
    | "-a" :: alphabetSpec :: rest ->
      rest |> parseMore {o with AlphabetSpec = alphabetSpec}
    | "-b" :: boundary :: rest ->
      if boundary.Length <> 1 then
        failwith $"Expecting a single character argument after '-b'"
      rest |> parseMore {o with Boundary = boundary[0]}
    | "-n" :: orderText :: rest ->
      let order = orderText |> Int32.Parse
      rest |> parseMore {o with Order = order}
    | "-tag" :: tag :: rest ->
      rest |> parseMore {o with OutTag = tag}
    | "-short" :: rest ->
      rest |> parseMore {o with AllowShort = true}
    | [] ->
      if o.Order < 0 then
        failwith "No order (-n) specified"
      if o.Sources |> List.isEmpty then
        failwith "No word source(s) specified (-words, -wc, -wcw)"
      Some({o with Sources = o.Sources |> List.rev})
    | x :: _ ->
      cp $"\frUnrecognized argument\f0 '\fo{x}\f0'"
      None
  let oo = args |> parseMore {
    Order = -1
    AlphabetSpec = "abcdefghijklmnopqrstuvwxyz"
    Boundary = '_'
    Sources = []
    OutTag = "out"
    AllowShort = false
  }
  match oo with
  | Some(o) ->
    o |> runGather
  | None ->
    Usage.usage "all"
    1

