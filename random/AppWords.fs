module AppWords

open System
open System.Text

open RandomUtilities.ByteSources
open RandomUtilities.WordLists

open ColorPrint
open CommonTools
open System.Collections.Generic

type private PreparedList = {
  Name: string
  Words: string []
  Entropy: float
}

type private WordsOptions = {
  Repeat: int
  ListLists: bool
  WordLists: PreparedList list
  TargetBits: int
  Separator: string
}

let private listLists showDetails (wlc:WordListCache) =
  cp "\foAvailable word lists\f0:"
  let listNames = 
    wlc.ListNames()
    |> Seq.sort
    |> Seq.toArray
  for listName in listNames do
    if showDetails then
      let wl = wlc.FindList(listName, wlc)
      let count = wl.Words.Count
      let entropyBits = log(float(count)) / log(2.0)
      cp $"\fg%-14s{listName}\f0 \fb%4d{count}\f0 words, \fc%8.3f{entropyBits}\f0 bits per word."
    else
      cp $"\fg%-14s{listName}\f0"
    
let private listLoader (wlc:WordListCache) expression =
  if WordList.IsValidLabel(expression) |> not then
    failwith $"'{expression}' is not a valid word list label"
  let getBaseList label =
    if WordList.IsValidLabel(label) |> not then
      failwith $"'{label}' is not a valid word list label"
    let words = wlc.FindList(label, wlc)
    if words = null then
      cp $"\frList not found\f0: \fo{label}\f0"
      wlc |> listLists false
      failwith "list not found"
    words
  // expressions are NYI, treat expression as plain list ID, as placeholder
  let wordList = expression |> getBaseList
  let words = wordList.Words |> Seq.toArray
  if words.Length = 0 then
    failwith $"List '{expression}' exists but is empty"
  if words.Length = 1 then
    failwith $"List '{expression}' exists but contains only one word (and therefore provides no entropy)"
  let entropyBits = log(float(words.Length)) / log(2.0)
  {
    Name = expression
    Words = words
    Entropy = entropyBits
  }

let run args =
  let wlc =
    WordListCache.Create()
      .AddApplicationFolder("WordLists")
      .AddCurrentFolder()
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | "-listlists" :: _ ->
      Some({o with ListLists = true})
    | "-list" :: expression :: rest ->
      let wl = expression |> listLoader wlc
      rest |> parseMore {o with WordLists = wl :: o.WordLists}
    | "-n" :: countText :: rest ->
      let n = countText |> Int32.Parse
      rest |> parseMore {o with Repeat = n}
    | "-bits" :: bitText :: rest ->
      let bits = bitText |> Int32.Parse
      rest |> parseMore {o with TargetBits = bits}
    | [] ->
      if o.WordLists |> List.isEmpty then
        cp "\frNo word list(s) specified\f0."
        {o with ListLists = true} |> Some
      else
        {o with WordLists = o.WordLists |> List.rev} |> Some
    | x :: _ ->
      cp $"\frUnrecognized argument \f0'\fo{x}\f0'"
      None
  let oo = args |> parseMore {
    Repeat = 1
    ListLists = false
    WordLists = []
    TargetBits = 128
    Separator = "-"
  }  
  match oo with
  | None ->
    Usage.usage "words"
    1
  | Some(o) ->
    if o.ListLists then
      wlc |> listLists true
      1
    else
      if verbose then
        cp $"\foUsing lists\f0:"
        for pl in o.WordLists do
          cp $"\fg%32s{pl.Name}\f0 \fb%4d{pl.Words.Length}\f0 words \fc%8.3f{pl.Entropy}\f0 bits per word"
      let lists = o.WordLists |> Seq.map (fun pl -> pl.Words :> IReadOnlyList<string>) |> Seq.toArray
      let bitSource = ByteSource.Random().Buffered().ToBitSource()
      for _ in 1 .. o.Repeat do
        let values = bitSource.RandomPickSequence(float(o.TargetBits), lists)
        let joined = String.Join(o.Separator, values)
        cp $"\fg{joined}\f0"
      0

