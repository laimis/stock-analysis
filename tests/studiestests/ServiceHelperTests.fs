module studiestests.ServiceHelperTests

open System.Collections.Generic
open Microsoft.Extensions.Configuration
open Xunit
open FsUnit
open studies

let initServiceHelper args =
    let myConfiguration = Dictionary<string, string>()
    myConfiguration.Add("storage", "memory")

    let configuration =
        ConfigurationBuilder()
            .AddInMemoryCollection(myConfiguration)
            .Build() :> IConfiguration |> Some
   
    ServiceHelper.init configuration args
    
[<Fact>]
let ``Storage works`` () =
    initServiceHelper [||]
    ServiceHelper.storage() |> ignore
    
[<Fact>]
let ``Brokerage works`` () =
    initServiceHelper [||]
    ServiceHelper.brokerage() |> ignore
    
[<Fact>]
let ``Command line parsing works``() =
    
    let args = [|
        "-d"; System.IO.Path.GetTempPath()
        "-i"; "http://localhost:8080"
        "-o"; "outputFilename"
        "-f"; "inputFilename"
        "-v"
    |]
    
    initServiceHelper args
    
    ServiceHelper.studiesDirectory() |> Option.isSome |> should equal true
    ServiceHelper.importUrl() |> Option.get |> should equal "http://localhost:8080"
    ServiceHelper.outputFilename() |> should equal "outputFilename"
    ServiceHelper.inputFilename() |> should equal "inputFilename"
    ServiceHelper.hasArgument "-d" |> should equal true
    
[<Fact>]
let ``Asking for input or output filenames when none have been provided should fail``() =
    
    initServiceHelper [||]
    
    (fun () -> ServiceHelper.outputFilename() |> ignore) |> should (throwWithMessage "No output file specified, use -o <filename>") typeof<System.Exception>
    (fun () -> ServiceHelper.inputFilename() |> ignore) |> should (throwWithMessage "No input file specified, use -f <filename>") typeof<System.Exception>
    
    
[<Fact>]
let ``Not providing configuration, should fail``() =
    (fun () -> ServiceHelper.init None [||]) |> should throw typeof<System.InvalidOperationException>