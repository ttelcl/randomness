module AppInteger

open System
open System.Security.Cryptography

open ColorPrint
open CommonTools

type private SizeLimit =
  | Bits of int
  | Below of UInt128

type private Options = {
  Limit: SizeLimit
  Repeat: int
}

#nowarn 3391

let private parseArgs args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "--help" :: _ 
    | "-h" :: _ ->
      None
    | "-n" :: count :: rest ->
      let n = count |> Int32.Parse
      rest |> parseMore {o with Repeat = n}
    | "-bits" :: bitsText :: rest ->
      let ok, bits = bitsText |> Int32.TryParse
      if ok then
        if bits < 1 || bits > 128 then
          cp "\fo'\fy-bits\fo': argument must be in the range 1 to 128"
          None
        else
          rest |> parseMore {o with Limit = bits |> SizeLimit.Bits}
      else
        cp "\fo'\fy-bits\fo': argument must be a number (in the range 1 to 128)"
        None
    | "-below" :: belowText :: rest ->
      let ok, below = belowText |> UInt128.TryParse
      if ok then
        if below < 2u then
          cp "\fo'\fy-below\fo': argument must be at least 2"
          None
        else
          rest |> parseMore {o with Limit = below |> SizeLimit.Below}
      else
        cp "\fo'\fy-below\fo': argument must be a number below \fb340282366920938463463374607431768211456\f0 (2^128)"
        None
    | [] ->
      Some(o)
    | x :: _ ->
      cp $"\foUnrecognized argument \f0'\fy{x}\f0'"
      None
  args |> parseMore {
    Limit = SizeLimit.Bits(64)
    Repeat = 1
  }

let private random128 () =
  let bytes = Array.zeroCreate<byte>(16)
  RandomNumberGenerator.Fill(bytes)
  BitConverter.ToUInt128(bytes) // Requires net9 or newer!

let private randomBits bits =
  let r = random128()
  if bits < 128 then
    let one: UInt128 = 1u
    let mask = (one <<< bits) - one
    r &&& mask
  else
    r

let private randomBelow (below: UInt128) =
  random128() % below

let private runApp o =
  for i = 1 to o.Repeat do
    let randomNumber =
      match o.Limit with
      | SizeLimit.Bits(bits) ->
        bits |> randomBits
      | SizeLimit.Below(below) ->
        below |> randomBelow
    //cp $"{randomNumber,39}"
    cp $"{randomNumber}"
  0

let run args =
  match args |> parseArgs with
  | Some(o) ->
    o |> runApp
  | None ->
    cp ""
    Usage.usage "integer"
    1


