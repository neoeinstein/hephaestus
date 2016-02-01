﻿module Hephaestus.Console

open Hephaestus

(* Classifications *)

[<Literal>]
let Decision =
    "decision"

let Response =
    "response"

(* Configuration *)

type HttpConfiguration =
    { ServiceAvailable: bool option }

(* State *)

type HttpState =
    { Method: string }

(* Terminals *)

let response name =
    Specification.Terminal.create name (fun _ s ->
        async {
            printfn "%A Method: %s" name s.Method
            return (), s })

(* Core *)

module HttpCore =

    [<Literal>]
    let Name =
        "http.core"

    (* Decisions *)

    let private decision name =
        Specification.Decision.create (Key [ Name; Decision; name ])

    let private serviceAvailable =
        decision "service.available" (fun configuration ->
            match configuration.ServiceAvailable with
            | None ->
                Literal Right
            | Some available ->
                Function (fun state ->
                    async {
                        return match available with
                               | true -> Right, state
                               | _ -> Left, state }))

    (* Responses *)

    let private response name =
        response (Key [ Name; Response; name ])

    (* Composition *)

    let httpCore =
        serviceAvailable (response "service.unavailable", response "ok")

    (* Component *)

    let exports =
        { Metadata =
            { Name = Name
              Description = None }
          Requirements =
            { Required = set []
              Preconditions = [] }
          Operations =
            [ Prepend (fun _ -> httpCore) ] }

(* Options *)

module HttpOptions =

    [<Literal>]
    let Name =
        "http.options"

    (* Decisions *)

    let private decision name =
        Specification.Decision.create (Key [ Name; Decision; name ])

    let private methodOptions =
        decision "method.options" (fun _ ->
            Function (fun state ->
                async {
                    return match state.Method with
                           | m when m = "options" -> Right, state
                           | _ -> Left, state }))

    (* Responses *)

    let private response name =
        response (Key [ Name; Response; name ])

    (* Composition *)

    let httpOptions =
        fun x -> methodOptions (x, response "options")

    (* Component *)

    let exports =
        { Metadata =
            { Name = Name
              Description = None }
          Requirements =
            { Required = set [ HttpCore.Name ]
              Preconditions = [] }
          Operations =
            [ Splice (Key [ HttpCore.Name; Decision; "service.available" ], Right, httpOptions) ] }

(* Main *)

[<EntryPoint>]
let main _ =

    // Data

    let configuration = { ServiceAvailable = None }
    let state = { Method = "get" }

    // Model

    let model = Model.create (set [ HttpOptions.exports; HttpCore.exports ])

    // Prototype

    let prototype, prototypeLog = Prototype.createLogged model

    // Machine

    let machine, machineLog = Machine.createLogged prototype configuration

    // Usage

    let result, state = Machine.execute machine state |> Async.RunSynchronously

    // Wait

    let _ = System.Console.ReadLine ()
    0
