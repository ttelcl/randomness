module AppCharacters

open System
open System.Text

open RandomUtilities.ByteSources

open ColorPrint
open CommonTools

type private CharactersOptions = {
  Repeat: int
  Alphabet: string
  TargetBits: int
}

let entropyBits (n:int) =
  log(float(n)) / log(2.0)

let parseAlphabet (spec: string) =
  let rec prependCharacterRange (nextChar:char) lastChar list =
    let list = nextChar :: list
    let nextChar = char(int(nextChar) + 1)
    if nextChar > lastChar then
      list
    else
      list |> prependCharacterRange nextChar lastChar
  let rec processNextCharacter outList inList =
    match inList with
    | c1 :: '-' :: c2 :: rest ->
      let ic1 = int(c1)
      let ic2 = int(c2)
      if ic1 > ic2 then
        failwith $"Invalid character range '{c1}-{c2}'"
      let outList = outList |> prependCharacterRange c1 c2
      rest |> processNextCharacter outList
    | ch :: rest ->
      rest |> processNextCharacter (ch :: outList)
    | [] ->
      let alphabet = outList |> List.rev |> List.toArray |> String
      alphabet
  spec |> Seq.toList |> processNextCharacter []

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | "-a" :: spec :: rest
    | "-alphabet" :: spec :: rest ->
      let alphabet = spec |> parseAlphabet
      rest |> parseMore {o with Alphabet = alphabet}
    | "-n" :: countText :: rest ->
      let n = countText |> Int32.Parse
      rest |> parseMore {o with Repeat = n}
    | "-bits" :: bitText :: rest ->
      let bits = bitText |> Int32.Parse
      rest |> parseMore {o with TargetBits = bits}
    | [] ->
      if o.Alphabet |> String.IsNullOrEmpty then
        failwith "Missing alphabet"
      if o.Alphabet.Length < 2 then
        failwith "The alphabet should contain at least 2 characters"
      Some(o)
    | x :: _ ->
      failwith $"Unrecognized argument '{x}'"
  let oo = args |> parseMore {
    Repeat = 1
    Alphabet = "abcdefghijklmnopqrstuvwxyz"
    TargetBits = 128
  }
  match oo with
  | None ->
    Usage.usage "characters"
    1
  | Some(o) ->
    let targetBits = float(o.TargetBits)
    let bitsPerCharacter = o.Alphabet.Length |> entropyBits
    let charCount = int(ceil(targetBits / bitsPerCharacter))
    cp $"Using alphabet '\fy{o.Alphabet}\f0' (\fb{o.Alphabet.Length}\f0 characters)"
    cpx $"Generating \fy{o.Repeat}\f0 strings of \fb{charCount}\f0 characters,"
    cp $" \fg%.2f{bitsPerCharacter}\f0 bits per character, \fc%.2f{bitsPerCharacter * float(charCount)}\f0 total"
    let bitSource = ByteSource.Random().Buffered().ToBitSource()
    for _ in 1 .. o.Repeat do
      let txt = bitSource.RandomAlphabetProjection(o.Alphabet, charCount)
      cp $"\fg{txt}\f0"

    0
