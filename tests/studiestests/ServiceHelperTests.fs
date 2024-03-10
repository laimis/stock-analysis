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
    
    let tempPath = System.IO.Path.GetTempPath()
    
    let args = [|
        "-d"; tempPath
        "-i"; "http://localhost:8080"
        "-o"; "outputFilename"
        "-f"; "inputFilename"
        "-v"
    |]
    
    initServiceHelper args
    
    ServiceHelper.getArgumentValue "-d" |> should equal tempPath
    ServiceHelper.getArgumentValue "-i" |> should equal "http://localhost:8080"
    ServiceHelper.getArgumentValue "-o" |> should equal "outputFilename"
    ServiceHelper.inputFilename() |> should equal "inputFilename"
    ServiceHelper.hasArgument "-d" |> should equal true
    
[<Fact>]
let ``Asking for input or output filenames when none have been provided should fail``() =
    
    initServiceHelper [||]
    
    (fun () -> ServiceHelper.getArgumentValue "-o" |> ignore) |> should (throwWithMessage "No value specified for -o") typeof<System.Exception>
    
    
[<Fact>]
let ``Not providing configuration, should fail``() =
    (fun () -> ServiceHelper.init None [||]) |> should throw typeof<System.InvalidOperationException>
