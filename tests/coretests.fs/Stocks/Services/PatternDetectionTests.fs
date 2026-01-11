module coretests.fs.Stocks.Services.PatternDetectionTests

open Xunit
open core.fs.Adapters.Stocks
open core.fs.Services
open testutils
open FsUnit

[<Fact>]
let ``Pattern detection with only one bar returns nothing`` () =

    TestDataGenerator.PriceBars(TestDataGenerator.ENPH).Bars
    |> Array.take 1
    |> PriceBars
    |> PatternDetection.generate
    |> should be Empty

[<Fact>]
let ``Pattern detection with ENPH finds XVolume`` () =
    
    let bars = TestDataGenerator.PriceBars(TestDataGenerator.ENPH)
    
    // find the bar with the highest volume
    let highestVolume = bars.Bars |> Seq.maxBy (_.Volume)
    let barIndex = bars.Bars |> Seq.findIndex (fun x -> x.Volume = highestVolume.Volume)
    
    // generate new array that contains bars from 0 to the highest bar index (inclusive)
    // and run pattern finder, it should find two: highest volume and x volume
    let patterns =
        bars.Bars
        |> Seq.take (barIndex + 1)
        |> Array.ofSeq
        |> PriceBars
        |> PatternDetection.generate
        
    patterns |> should haveLength 2
    patterns |> Seq.map _.name |> should contain PatternDetection.highest1YearVolumeName
    patterns |> Seq.map _.name |> should contain PatternDetection.highVolumeName
    
[<Fact>]
let ``Generate with small input returns nothing``() =
    TestDataGenerator.IncreasingPriceBars()
    |> PatternDetection.generate
    |> should be Empty

let appendHighVolumeBar (bars:PriceBars) =
    let last = bars.Last
    let newBar = PriceBar(last.Date.AddDays(1), last.Open, last.High, last.Low, last.Close, (last.Volume * 10L))
    PriceBars([newBar] |> Seq.append bars.Bars |> Array.ofSeq)

    
[<Fact>]
let ``Highest Volume on latest bar is detected``() =
    
    let patterns =
        TestDataGenerator.IncreasingPriceBars(100)
        |> appendHighVolumeBar
        |> PatternDetection.generate
        
    patterns |> should haveLength 1
    
    let pattern = patterns |> Seq.head
    pattern.name |> should equal PatternDetection.highVolumeName
    
[<Fact>]
let ``Highest volume on small amount of bars is ignored``() =
    TestDataGenerator.IncreasingPriceBars(10)
    |> appendHighVolumeBar
    |> PatternDetection.generate
    |> should be Empty
    
[<Fact>]
let ``Generate with empty bars does not blow up``() =
    PriceBars(Array.empty)
    |> PatternDetection.generate
    |> should be Empty

[<Fact>]
let ``Generate with gaps finds gap pattern`` () =
    
    let generateTestBars numberOfBars =
        let bars = TestDataGenerator.IncreasingPriceBars(numberOfBars)
    
        // append a bar with a gap up
        let lastBar = bars.Last
        let newBar = PriceBar(lastBar.Date.AddDays(1), lastBar.Open * 1.1m, lastBar.High * 1.1m, lastBar.Close * 1.1m, lastBar.Close * 1.1m, lastBar.Volume)
        PriceBars([newBar] |> Seq.append bars.Bars |> Array.ofSeq)
        
    let bars = generateTestBars 10
    let patterns = PatternDetection.generate bars
    patterns |> should haveLength 1
    
    let pattern = patterns |> Seq.head
    pattern.name |> should equal PatternDetection.gapUpName
    pattern.description |> should equal "Gap Up 10.0%"
    
    // generating with a gap up but with enough bars, should include volume multiplier
    let bars = generateTestBars 100
    let patterns = PatternDetection.generate bars
    
    let pattern = patterns |> Seq.find (fun x -> x.name = PatternDetection.gapUpName)
    pattern.name |> should equal PatternDetection.gapUpName
    pattern.description |> should equal "Gap Up 10.0%, volume x1.4"
