module AppImport

open System
open System.IO

open Newtonsoft.Json

open WikiDataLib.Configuration
open WikiDataLib.Repository

open ColorPrint
open CommonTools

type private ImportOptions = {
  Pending: bool
  Files: string list
}

let private runImport o =
  let repo = new WikiRepo()
  let pendingFiles =
    if o.Pending then
      let pending = repo.PendingFiles() |> Seq.toList
      if pending |> List.isEmpty then
        cp $"\foNo pending files found in \fy{repo.ImportFolder}\f0!"
      pending
    else
      []
  let files = o.Files |> Seq.append pendingFiles |> Seq.toList
  if files |> List.isEmpty then
    cp "\frNo files to import\f0."
    1
  else
    for file in files do
      let wdi = file |> WikiRepo.ImportId
      if wdi = null then
        cp $"\fo{file}\f0:"
        cp "  \frSkipping: \foUnrecognized file type\f0."
      else
        let wd = repo.GetDumpFolder(wdi)
        let targetfile = Path.Combine(wd.Folder, file |> Path.GetFileName)
        if targetfile |> File.Exists then
          cp $"\fo{file}\f0:"
          cp $"  \foTarget file already exists\f0 (\fy{targetfile}\f0)"
        else
          cp $"\fg{file}\f0:"
          File.Move(file, targetfile)
          cp $"  \f0Moved to \fy{targetfile}\f0"
    0

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | "-p" :: rest
    | "-pending" :: rest ->
      rest |> parseMore {o with Pending = true}
    | "-f" :: file :: rest
    | "-file" :: file :: rest ->
      rest |> parseMore {o with Files = file :: o.Files}
    | [] ->
      if o.Files |> List.isEmpty && o.Pending |> not then
        cp "\frNo files specified. \f0(\foexpecting at least one \fy-pending\fo or \fy-f\fo argument\f0)"
        None
      else
        Some({o with Files = o.Files |> List.rev})
    | x :: _ ->
      cp $"\frUnrecognized argument \f0'\fo{x}\f0'"
      None
  let oo = args |> parseMore {
    Pending = false
    Files = []
  }
  match oo with
  | Some(o) ->
    o |> runImport
  | None ->
    Usage.usage "import"
    1
