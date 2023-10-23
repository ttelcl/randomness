module WikiUtils

open System
open System.IO

open WikiDataLib.Repository

open ColorPrint

let resolveWiki wikiIdOption =
  match wikiIdOption with
  | None ->
    let study = Study.FromFile()
    if study = null then
      failwith "No wiki specified, and no study configured to provide the default"
    cp $"Using wiki \fg{study.WikiId}\f0 (the default set in the active study)"
    study.WikiId
  | Some(wid: WikiDumpId) ->
    wid

