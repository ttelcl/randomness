module AppWikiList

open System
open System.IO

open Newtonsoft.Json

open WikiDataLib.Configuration
open WikiDataLib.Repository

open ColorPrint
open CommonTools

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | [] ->
      Some(o)
    | x :: _ ->
      cp $"\frUnrecognized argument \fo{x}\f0"
      None
  let oo = args |> parseMore ()
  match oo with
  | None ->
    Usage.usage "list"
    1
  | Some(o) ->
    let repo = new WikiRepo()
    let wikis = repo.Wikis;
    cp $"wiki stores in repository \fc{repo.Folder}\f0:"
    if wikis.Count = 0 then
      cp "\foNo wiki stores present yet\f0."
    else
      for wiki in wikis do
        let dumpCountColor = if wiki.Dumps.Count > 0 then "\fb" else "\fr"
        cp $"  \fg{wiki.WikiTag}\f0 ({wiki.Folder})  {dumpCountColor}{wiki.Dumps.Count}\f0 dumps."
        for dump in wiki.Dumps do
          let keyColor = if dump.HasMainFile && dump.HasStreamIndex then "\fy" else "\fo"
          let mainColor = if dump.HasMainFile then "\fg" else "\fr"
          let streamIdxColor =
            if dump.HasStreamIndex then
              "\fg"
            else
              "\fr"
          cp $"      {keyColor}{dump.Id}   {mainColor}master  {streamIdxColor}streamindex\f0"
        
    0
