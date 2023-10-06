module AppWikiList

open System
open System.IO

open Newtonsoft.Json

open WikiDataLib.Configuration

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
    Usage.usage "wikis list"
    1
  | Some(o) ->
    let cfg = MachineWikiConfiguration.LoadConfiguration()
    let wikifolder = cfg.RepoFolder
    let repo = new WikiRepo(wikifolder)
    let wikitags = repo.WikiNames(false);
    cp $"wiki stores in repository \fc{wikifolder}\f0:"
    if wikitags.Count = 0 then
      cp "\foNo wiki stores present yet\f0."
    else
      for wikitag in wikitags do
        let folder = Path.Combine(wikifolder, wikitag)
        cp $"  \fg{wikitag}\f0 ({folder})"
    0
