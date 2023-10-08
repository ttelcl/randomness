module AppExtract

open System
open System.IO

open Newtonsoft.Json

open WikiDataLib.Configuration
open WikiDataLib.Repository
open WikiDataLib.Utilities

open ColorPrint
open CommonTools

type private SectionIndex =
  | ByOffset of int64
  | ByIndex of int

type private ExtractOptions = {
  InfoMode: bool
  Raw: bool
  Wrap: bool
  WikiId: WikiDumpId option
  Sections: SectionIndex list
}

let private runExtract o =
  cp "\frNot Yet Implemented\f0."
  0

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
    | "-raw" :: rest ->
      rest |> parseMore {o with Raw = true}
    | "-wrap" :: rest ->
      rest |> parseMore {o with Wrap = true}
    | "-info" :: rest ->
      rest |> parseMore {o with InfoMode = true}
    | "-p" :: position :: rest ->
      let idx = position |> Int64.Parse |> SectionIndex.ByOffset
      rest |> parseMore {o with Sections = idx :: o.Sections}
    | "-i" :: index :: rest ->
      let idx = index |> Int32.Parse |> SectionIndex.ByIndex
      rest |> parseMore {o with Sections = idx :: o.Sections}
    | [] ->
      if o.WikiId.IsNone then
        cp "\frNo wikidump specified\f0 (Missing \fo-wiki\f0 argument. Use \fowikidata list\f0 to find valid values)"
        None
      else
        Some({o with Sections = o.Sections |> List.rev})
    | x :: _ ->
      cp $"\frUnrecognized argument\f0: \fo{x}\f0"
      None
  let oo = args |> parseMore {
    InfoMode = false
    Raw = false
    Wrap = false
    WikiId = None
    Sections = []
  }
  match oo with
  | None ->
    Usage.usage "extract"
    0
  | Some(o) ->
    o |> runExtract

