module AppWordCalc

open System
open System.IO

open RandomUtilities.CharacterDistributions
open RandomUtilities.WordLists
open RandomUtilities.ByteSources
open RandomUtilities.WordCounts

open ColorPrint
open CommonTools

type private Options = {
  OutFile: string
  InFiles: string list
  One: bool
  MinLength: int
}

let private runWordCalc o =
  let wcm = new WordCountMap()
  for infile in o.InFiles do
    wcm.AddFile(infile, true, o.MinLength)
  if o.One then
    wcm.ResetCounts()
  let tmpName = o.OutFile + ".tmp"
  cp $"Saving \fg{o.OutFile}\f0."
  tmpName |> wcm.SaveFile
  o.OutFile |> finishFile
  0

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | "-ml" :: minLength :: rest ->
      rest |> parseMore {o with MinLength = minLength |> Int32.Parse}
    | "-1" :: rest ->
      rest |> parseMore {o with One = true}
    | "-o" :: file :: rest ->
      if file.EndsWith(".words.csv") |> not then
        cp "\foExpecting output file to have extension \fy.words.csv\f0!"
        None
      else
        rest |> parseMore {o with OutFile = file}
    | "+" :: infile :: rest
    | "-i" :: infile :: rest ->
      if infile.EndsWith(".words.csv") |> not then
        cp "\foExpecting input files to have extension \fy.words.csv\f0!"
        None
      else
        rest |> parseMore {o with InFiles = infile :: o.InFiles}
    | [] ->
      if o.OutFile |> String.IsNullOrEmpty then
        cp "\foNo output file specified\f0."
        None
      elif o.InFiles.IsEmpty then
        cp "\foNo input file(s) specified\f0."
        None
      else
        {o with InFiles = o.InFiles |> List.rev} |> Some
    | x :: _ ->
      cp $"\frUnrecognized argument \fo{x}\f0"
      None
  let oo = args |> parseMore {
    OutFile = null
    InFiles = []
    One = false
    MinLength = 1
  }
  match oo with
  | None ->
    Usage.usage "wordcalc"
    1
  | Some(o) ->
    o |> runWordCalc

