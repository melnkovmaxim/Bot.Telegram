﻿[<AutoOpen>]
module Prelude
    open FSharpx
    open System
    open System.Threading.Tasks

    let inline ( ^ ) f x = f x
    let inline ( <?> ) opt def = Option.defaultValue def opt
    let inline ( <?!> ) (vTask: Task<Option<_>>) def = task {
        let! v = vTask
        return v <?> def
    }


    module Option =
        let inline ofObject x =
            if Object.ReferenceEquals(x, null) then None
            else Some x
    
        
        let inline ofTryPattern x =
            match x with
            | true, x -> Some x
            | _ -> None
            
            
        let inline ( >>= ) x f =
            match x with
            | Some x -> f x
            | None -> None
            
    module Int32 =
        let tryParse (s: string) =
            match Int32.TryParse(s) with
            | true, s -> Some s
            | false, _ -> None
          
    module Task =
        let inline ignore (t: Task<'a>) = task {
            let! _ = t
            return ()
        }

