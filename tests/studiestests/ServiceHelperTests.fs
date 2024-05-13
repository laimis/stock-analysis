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
    initServiceHelper [||] |> _.Storage() |> ignore
    
[<Fact>]
let ``Brokerage works`` () =
    initServiceHelper [||] |> _.Brokerage() |> ignore
    
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
    
    let context = initServiceHelper args
    
    context.GetArgumentValue "-d" |> should equal tempPath
    context.GetArgumentValue "-i" |> should equal "http://localhost:8080"
    context.GetArgumentValue "-o" |> should equal "outputFilename"
    context.GetArgumentValue "-f" |> should equal "inputFilename"
    context.HasArgument "-d" |> should equal true
    
[<Fact>]
let ``Asking for input or output filenames when none have been provided should fail``() =
    
    let context = initServiceHelper [||]
    
    (fun () -> context.GetArgumentValue "-o" |> ignore) |> should (throwWithMessage "No value specified for -o") typeof<System.Exception>
    
    
[<Fact>]
let ``Not providing configuration, should fail``() =
    (fun () -> ServiceHelper.init None [||] |> ignore) |> should throw typeof<System.InvalidOperationException>
