module AppConfig

open System
open System.IO

open Newtonsoft.Json

open WikiDataLib.Configuration

open ColorPrint
open CommonTools

type private ConfigOptions = {
  InitPath: string
}

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | "-init" :: path :: rest ->
      rest |> parseMore {o with InitPath = path}
    | [] ->
      Some(o)
    | x :: _ ->
      cp $"\frUnrecognized argument\f0: \fo{x}\f0."
      None
  let oo = args |> parseMore {
    InitPath = null
  }
  match oo with
  | None ->
    Usage.usage "config"
    1
  | Some(o) ->
    let defaultPath = MachineWikiConfiguration.DefaultConfigFilePath
    if defaultPath |> File.Exists then
      cp $"Loading configuration file \fy{defaultPath}\f0"
      let cfg = MachineWikiConfiguration.LoadConfiguration()
      if cfg.RepoFolder |> Directory.Exists then
        cp $"The configured repository folder is \fg{cfg.RepoFolder}\f0"
      else
        cp $"\frThe configured repository folder does not exist\f0: \fo{cfg.RepoFolder}\f0"
      if o.InitPath |> String.IsNullOrEmpty |> not then
        cp "\foThe configuration file already exists \fr=>\fo ignoring \fy-init\fo argument"
        1
      else
        0
    else
      if o.InitPath |> String.IsNullOrEmpty |> not then
        cp $"The configuration file does not exist yet: \fg{defaultPath}\f0 (creating it)"
        let cfg = new MachineWikiConfiguration(o.InitPath |> Path.GetFullPath)
        let json = JsonConvert.SerializeObject(cfg, Formatting.Indented)
        do
          let cfgFolder = defaultPath |> Path.GetDirectoryName
          if cfgFolder |> Directory.Exists |> not then
            cp $"Creating configuration folder \fc{cfgFolder}\f0."
            cfgFolder |> Directory.CreateDirectory |> ignore
          use w = defaultPath |> startFile
          w.WriteLine(json)
        defaultPath |> finishFile
        0
      else
        cp $"\foThe configuration file does not exist yet\f0: \fy{defaultPath}\f0"
        1
