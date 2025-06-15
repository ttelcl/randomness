// (c) 2023  ttelcl / ttelcl

open System

open ColorPrint
open CommonTools
open ExceptionTool
open Usage

let rec run arglist =
  match arglist with
  | "-v" :: rest ->
    verbose <- true
    rest |> run
  | "--help" :: _
  | "-h" :: _
  | [] ->
    usage "all"
    0  // program return status code to the operating system; 0 == "OK"
  | "guid" :: rest ->
    rest |> AppGuid.run
  | "hex" :: rest ->
    rest |> AppBased.runBase AppBased.BaseNames.HexLow
  | "HEX" :: rest ->
    rest |> AppBased.runBase AppBased.BaseNames.HexUpp
  | "base32" :: rest ->
    rest |> AppBased.runBase AppBased.BaseNames.Base32
  | "base64" :: rest ->
    rest |> AppBased.runBase AppBased.BaseNames.Base64
  | "characters" :: rest 
  | "chars" :: rest ->
    rest |> AppCharacters.run
  | "words" :: rest ->
    rest |> AppWords.run
  | "wordlets" :: rest ->
    rest |> AppWordlets.run
  | x :: _ ->
    cp $"\frUnrecognized command \fo{x}\f0."
    1

[<EntryPoint>]
let main args =
  try
    args |> Array.toList |> run
  with
  | ex ->
    ex |> fancyExceptionPrint verbose
    resetColor ()
    1



