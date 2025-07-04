﻿// (c) 2023  ttelcl / ttelcl

open System

open CommonTools
open ColorPrint
open ExceptionTool
open Usage

let rec run arglist =
  // For subcommand based apps, split based on subcommand here
  match arglist with
  | "-v" :: rest ->
    verbose <- true
    rest |> run
  | "--help" :: _
  | "-h" :: _ ->
    usage "all"
    0  // program return status code to the operating system; 0 == "OK"
  | []
  | "-?" :: _ ->
    usage "short"
    0  // program return status code to the operating system; 0 == "OK"
  | "config" :: rest ->
    rest |> AppConfig.run
  | "wikis" :: "list" :: rest
  | "wiki" :: "list" :: rest
  | "list" :: rest
  | "wikilist" :: rest 
  | "wiki-list" :: rest 
  | "wikis-list" :: rest ->
    rest |> AppWikiList.run
  | "import" :: rest ->
    rest |> AppImport.run
  | "streamindex" :: rest ->
    rest |> AppStreamIndex.run
  | "extract" :: rest ->
    rest |> AppExtract.run
  | "articleindex" :: rest ->
    rest |> AppArticleIndex.run
  | "dump" :: rest ->
    rest |> AppDump.run
  | "study" :: "init" :: rest ->
    rest |> AppStudyInit.run
  | "study" :: "export" :: rest ->
    rest |> AppStudyExport.run
  | "study" :: x :: _ ->
    cp $"\frUnknown subcommand\fo study \fy{x}\f0!"
    usage "study"
    1
  | "study" :: [] ->
    usage "study"
    1
  | "search" :: rest ->
    rest |> AppSearch.run
  | x :: _ ->
    cp $"\frUnrecognized argument\f0: \fo{x}\f0."
    usage "short"
    1

[<EntryPoint>]
let main args =
  try
    args |> Array.toList |> run
  with
  | ex ->
    ex |> fancyExceptionPrint verbose
    resetColor ()
    1



