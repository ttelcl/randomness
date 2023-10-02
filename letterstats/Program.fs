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
  | "gather" :: rest ->
    rest |> AppGather.run
  | "generate" :: rest ->
    rest |> AppGenerate.run
  | x :: _ ->
    cp $"\frUnknown command: \fo{x}\f0."
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



