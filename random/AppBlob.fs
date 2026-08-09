module AppBlob

open System
open System.Collections.Generic
open System.Text

open RandomUtilities.ByteSources

open ColorPrint
open CommonTools

type private Options = {
  Count: int
  FileName: string
}

let private parseOptions args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | "-n" :: countText :: rest ->
      let n = countText |> Int32.Parse
      rest |> parseMore {o with Count = n}
    | "-f" :: fileName :: rest ->
      rest |> parseMore {o with FileName = fileName}
    | [] ->
      if o.FileName |> String.IsNullOrEmpty then
        cp "\frNo output file specified\f0."
        None
      else
        o |> Some
    | x :: _ ->
      cp $"\frUnrecognized argument \f0'\fo{x}\f0'"
      None
  args |> parseMore {
    FileName = null
    Count = 1024
  }

let private runInner o =
  let rng = new SecureRandomByteSource()
  do
    use f = o.FileName |> startFileBinary
    rng.PushToStream(f, o.Count, 1024)
  o.FileName |> finishFile
  0

let run args =
  let oo = args |> parseOptions
  match oo with
  | Some o ->
    o |> runInner
  | None ->
    Usage.usage "blob"
    1

