module AppGather

open System

open RandomUtilities.CharacterDistributions
open RandomUtilities.WordLists

open ColorPrint
open CommonTools

type WordSource =
  | ListSource of string

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
    wl.Words :> seq<string>

let private runGather o =
  let alphabet = new Alphabet(o.AlphabetSpec, o.Boundary)
  let collector = new FragmentDistribution(alphabet, o.Order)
  let weight = 1
  for source in o.Sources do
    let words =
      source
      |> enumerateWordsInSource
      |> Seq.filter (fun word -> alphabet.IsRepresentable(word, false))
    let words =
      if o.AllowShort then
        words
      else
        words |> Seq.filter (fun word -> word.Length >= o.Order)
    for word in words do
      collector.AddWord(word, weight)
  if collector.Total < 1 then
    cp "\frNo acceptable words found in the source(s)\f0."
    1
  else
    cp $"Added \fb{collector.Total}\f0 fragments"
    let onm1 = $"{o.OutTag}.wordstats-{o.Order}.info.csv"
    let onm2 = $"{o.OutTag}.{o.Order}.fragment-counts.csv"
    do
      use w1 = onm1 |> startFile
      fprintfn w1 "prefix,letter,count,fraction,entropy,surprisal"
      use w2 = onm2 |> startFile
      fprintfn w2 "fragment,count"
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
            fprintfn w2 $"{prefix}{letter},{value}"
    onm1 |> finishFile
    onm2 |> finishFile
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
        failwith "No word source(s) specified (-words)"
      Some(o)
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

