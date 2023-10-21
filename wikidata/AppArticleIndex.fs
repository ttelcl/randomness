module AppArticleIndex

open System
open System.IO
open System.Text
open System.Xml

open XsvLib

open WikiDataLib.Repository

open ColorPrint
open CommonTools

type private ArtIdxOptions = {
  WikiId: WikiDumpId option
  StreamCount: int
}

type WikiContext = {
  Dump: WikiDump
  SubIndex: SubstreamIndex
}

let run args =
  0
