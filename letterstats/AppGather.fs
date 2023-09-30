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
  Source: WordSource option
  OutTag: string
}

let private enumerateWords o =
  match o.Source with
  | Some(ListSource(wordlistLabel)) ->
    let wlc = 
      WordListCache.Create()
        .AddApplicationFolder("WordLists")
    let wl = wlc.FindList(wordlistLabel, wlc)
    if wl = null then
      failwith $"Unknown word list or invalid word list expression: '{wordlistLabel}'"
    wl.Words :> seq<string>
  | None ->
    failwith "No word source specified (-list)"

let private runGather o =
  let alphabet = new Alphabet(o.AlphabetSpec, o.Boundary)
  let words =
    o
    |> enumerateWords
    |> Seq.filter (fun word -> alphabet.IsRepresentable(word, false))
  let collector = new FragmentDistribution(alphabet, o.Order)
  for word in words do
    collector.AddWord(word, 1)
  if collector.Total < 1 then
    cp "\frNo acceptable words found in the source\f0."
    1
  else
    cp $"Added \fb{collector.Total}\f0 fragments"
    let onm = $"{o.OutTag}.wordstats-{o.Order}.csv"
    do
      use w = onm |> startFile
      fprintfn w "prefix,letter,count,fraction,entropy,surprisal"
      for kvp in collector.AllDistributions() do
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
            fprintfn w $"{prefix},{letter},{value},%.5f{fraction},%.5f{entropy},{surprisalText}"
    onm |> finishFile
    0

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | "-list" :: label :: rest ->
      let lst = WordSource.ListSource(label)
      rest |> parseMore {o with Source = Some(lst)}
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
    | [] ->
      if o.Order < 0 then
        failwith "No order (-n) specified"
      if o.Source |> Option.isNone then
        failwith "No word source specified (-list)"
      Some(o)
    | x :: _ ->
      cp $"\frUnrecognized argument\f0 '\fo{x}\f0'"
      None
  let oo = args |> parseMore {
    Order = -1
    AlphabetSpec = "abcdefghijklmnopqrstuvwxyz"
    Boundary = '_'
    Source = None
    OutTag = "out"
  }
  match oo with
  | Some(o) ->
    o |> runGather
  | None ->
    Usage.usage "all"
    1

