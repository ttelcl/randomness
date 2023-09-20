module AppGuid

open System

open ColorPrint
open CommonTools

type private GuidOptions = {
  Repeat: int
}

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | "-n" :: countText :: rest ->
      let count = countText |> Int32.Parse
      rest |> parseMore {o with Repeat = count}
    | [] ->
      Some(o)
    | x :: _ ->
      failwith $"Unrecognized argument '{x}'"
  let oo = args |> parseMore {
    Repeat = 1
  }
  match oo with
  | None ->
    Usage.usage "guid"
  | Some(o) ->
    for i in 1..o.Repeat do
      let guid = Guid.NewGuid()
      cp $"\fg%O{guid}\f0"
  0


