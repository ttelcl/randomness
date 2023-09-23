module AppBased

open System

open RandomUtilities.ByteSources

open ColorPrint
open CommonTools

type BaseNames =
  | HexLow
  | HexUpp
  | Base32
  | Base64

let private baseBits baseName =
  match baseName with
  | BaseNames.HexLow
  | BaseNames.HexUpp -> 4
  | BaseNames.Base32 -> 5
  | BaseNames.Base64 -> 6

let private defaultBits baseName =
  match baseName with
  | BaseNames.HexLow
  | BaseNames.HexUpp -> 64
  | BaseNames.Base32 -> 100
  | BaseNames.Base64 -> 96

let private baseAlphabet baseName =
  match baseName with
  | BaseNames.HexLow ->
    "0123456789abcdef"
  | BaseNames.HexUpp ->
    "0123456789ABCDEF"
  | BaseNames.Base32 ->
    "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"
  | BaseNames.Base64 ->
    "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
    "abcdefghijklmnopqrstuvwxyz" +
    "0123456789" +
    "-_" // use the web safe version as default

let private baseLabel baseName =
  match baseName with
  | BaseNames.HexLow -> "hex (lower case)"
  | BaseNames.HexUpp -> "hex (upper case)"
  | BaseNames.Base32 -> "base32"
  | BaseNames.Base64 -> "base64"

type private BaseOptions = {
  BaseName: BaseNames
  BaseBits: int
  TargetBits: int
  Repeat: int
}

let runBase baseName args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | "-n" :: count :: rest ->
      let n = count |> Int32.Parse
      rest |> parseMore {o with Repeat = n}
    | "-bits" :: bitsText :: rest ->
      let bits = bitsText |> Int32.Parse
      if bits % o.BaseBits <> 0 then
        failwith $"Expecting -bits argument to be a multiple of {o.BaseBits}"
      if bits < o.BaseBits then
        failwith $"Expecting -bits argument to be at least {o.BaseBits}"
      rest |> parseMore {o with TargetBits = bits}
    | [] ->
      Some(o)
    | x :: _ ->
      failwith $"Unrecognized argument '{x}'"
  let oo = args |> parseMore {
    BaseName = baseName
    BaseBits = baseName |> baseBits
    TargetBits = baseName |> defaultBits
    Repeat = 1
  }
  match oo with
  | Some(o) ->
    let charCount = (o.TargetBits + o.BaseBits - 1) / o.BaseBits
    let alphabet = baseName |> baseAlphabet
    cpx $"Generating \fy{o.Repeat}\f0 strings of \fb{charCount}\f0 \fo{baseName |> baseLabel}\f0 characters,"
    cp $" \fg{o.BaseBits}\f0 bits per character (\fc{o.TargetBits}\f0 total)"
    let bitSource = ByteSource.Random().Buffered().ToBitSource()
    for _ in 1 .. o.Repeat do
      let txt = bitSource.RandomAlphabetProjection(alphabet, charCount)
      cp $"\fg{txt}\f0"
    0
  | None ->
    Usage.usage "based"
    1

