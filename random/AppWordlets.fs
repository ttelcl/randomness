module AppWordlets

open System
open System.Collections.Generic
open System.Text

open RandomUtilities.ByteSources
open RandomUtilities.Picking

open ColorPrint
open CommonTools

type private Options = {
  Repeat: int
  TargetBits: int
  Template: WordletPicker option
}

let private runWordlets o =
  let template = o.Template.Value
  if template.TotalInformationBits < 1.0 then
    cp $"\frInvalid template\f0 (low on information, probably empty: {template.TotalInformationBits} bits)"
    1
  else
    cp $"\fgBits per segment = \fb{template.TotalInformationBits:F2}\f0."
    let randomness = ByteSource.Random().Buffered().ToBitSource()
    for _ in 1 .. o.Repeat do
      cpx "  "
      let mutable pendingBits = float(o.TargetBits)
      let mutable prefix = ""
      while pendingBits > 0.0 do
        let wordlet = template.PickWordlet(true, randomness)
        pendingBits <- pendingBits - template.TotalInformationBits
        cpx $"{prefix}{wordlet}"
        prefix <- "-"
      cp ""
    0

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | "-n" :: countText :: rest ->
      let n = countText |> Int32.Parse
      rest |> parseMore {o with Repeat = n}
    | "-bits" :: bitText :: rest ->
      let bits = bitText |> Int32.Parse
      rest |> parseMore {o with TargetBits = bits}
    | [] ->
      match o.Template with
      | Some(t) ->
        o |> Some
      | None ->
        cp "\foNo template specified"
        None
    | x :: _ ->
      cp $"\frUnrecognized argument \f0'\fo{x}\f0'"
      None
  let oo = args |> parseMore {
    Repeat = 1
    TargetBits = 128
    Template = WordletPicker.Instance2 |> Some
  }
  match oo with
  | None ->
    Usage.usage "wordlets"
    1
  | Some(o) ->
    o |> runWordlets

