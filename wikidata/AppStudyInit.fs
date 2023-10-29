module AppStudyInit

open System
open System.IO
open System.Text
open System.Xml

open WikiDataLib.Repository

open ColorPrint
open CommonTools

type private StudyInitOptions = {
  WikiId: WikiDumpId option
}

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | "-wiki" :: key :: rest ->
      let wdi = key |> WikiDumpId.Parse
      rest |> parseMore {o with WikiId = Some(wdi)}
    | [] ->
      Some(o)
    | x :: _ ->
      cp $"\frUnrecognized argument\f0: '\fo{x}\f0'"
      None
  let oo = args |> parseMore {
    WikiId = None
  }
  match oo with
  | None ->
    Usage.usage "study"
    1
  | Some(o) ->
    let study = Study.FromFile()
    if study = null then
      match o.WikiId with
      | None ->
        cp "\foNo \fy-wiki\fo specified and no existing study file found\f0."
        1
      | Some(wikiId) ->
        let repo = new WikiRepo()
        let dump = wikiId |> repo.FindDumpFolder
        if dump = null then
          cp $"\frWiki \fy{wikiId}\fr is missing\f0 (cannot be referenced from a Study). See \fowikidata list\f0 for valid names."
          1
        else
          let s = new Study(wikiId.WikiTag, wikiId.DumpTag, [])
          cp "Saving \fgstudy.json\f0."
          "study.json" |> s.SaveToFile
          0
    else
      match o.WikiId with
      | None ->
        cp $"Existing study wiki is \fg{study.WikiId}\f0."
        0
      | Some(wikiId) ->
        if wikiId.WikiTag = study.WikiName && wikiId.DumpTag = study.WikiDate then
          cp "A study already exists, using that same identifier. To create a new one delete or rename \fystudy.json\f0 first."
          0
        else
          cp $"\frStudy already exists\f0 (\fo{study.WikiId}\f0). Delete or rename \fystudy.json\f0 before creating a new study."
          1
