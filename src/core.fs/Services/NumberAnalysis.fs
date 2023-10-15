namespace core.fs.Services

open System.Collections.Generic
open core.fs.Services.Analysis
open core.fs.Shared.Adapters.Stocks

module NumberAnalysis =
    
    // by default number of buckets should be 21
    let calculateHistogram (numbers:decimal array) (min:decimal) max numberOfBuckets : seq<ValueWithFrequency> =
        
        let bucketSize = (max - min) / decimal(numberOfBuckets)
        
        // if the bucket size is really small, use just two decimal places
        // otherwise use 0 decimal places
        let bucketSize = 
            if bucketSize < 1m then
                System.Math.Round(bucketSize, 4)
            else
                System.Math.Floor(bucketSize)
        
        let min = 
            if bucketSize < 1m then
                System.Math.Round(min, 4)
            else
                System.Math.Floor(min)
        
        let result =
            List<ValueWithFrequency>(
                [0 .. numberOfBuckets - 1]
                |> List.map (fun i -> {value = min + (decimal(i) * bucketSize); frequency = 0})
            )
        numbers
        |> Array.iter ( fun n ->
            if n > result[result.Count - 1].value then
                result[result.Count - 1] <- {value = result[result.Count - 1].value; frequency = result[result.Count - 1].frequency + 1}
            else
                let firstSlot = result |> Seq.findIndex (fun x -> x.value >= n)
                result[firstSlot] <- {value = result[firstSlot].value; frequency = result[firstSlot].frequency + 1}
        )
        
        result
    
    let private calculateStats (numbers:decimal[]) : DistributionStatistics =
        if numbers.Length = 0 then
            {
                count = 0m
                kurtosis = 0m
                min = 0m
                max = 0m
                mean = 0m
                median = 0m
                skewness = 0m
                stdDev = 0m
                buckets = [||] 
            }
        else
            let mean = System.Math.Round(numbers |> Array.average, 2)
            let min = System.Math.Round(numbers |> Array.min, 2)
            let max = System.Math.Round(numbers |> Array.max, 2)
            
            let median =
                System.Math.Round(
                numbers
                |> Array.sort
                |> Array.skip (numbers.Length / 2)
                |> Array.head,
                2)
            
            let count = numbers.Length
            
            let stdDevDouble =
                System.Math.Round(
                numbers
                |> Array.map (fun x -> System.Math.Pow(double(x - mean), 2))
                |> Array.sum
                |> fun x -> x / ((numbers.Length - 1) |> float)
                |> System.Math.Sqrt,
                2)
            
            let stdDev = 
                match stdDevDouble with
                | double.PositiveInfinity -> 0m
                | double.NegativeInfinity -> 0m
                | _ -> (decimal)stdDevDouble
            
            let skewnessDouble =
                numbers
                |> Array.map (fun x -> System.Math.Pow(double(x - mean), 3))
                |> Array.sum
                |> fun x -> x / double(count) / System.Math.Pow(double stdDev, 3)
                
            
            let skewness = 
                match skewnessDouble with
                | double.PositiveInfinity -> 0m
                | double.NegativeInfinity -> 0m
                | _ -> (decimal)skewnessDouble
            
            let kurtosisDouble = 
                numbers
                |> Array.map (fun x -> System.Math.Pow(double(x - mean), 4))
                |> Array.sum
                |> fun x -> x / double(count) / System.Math.Pow(double stdDev, 4) - 3.0
            
            let kurtosis = 
                match kurtosisDouble with
                | double.PositiveInfinity -> 0m
                | double.NegativeInfinity -> 0m
                | _ -> (decimal)kurtosisDouble
                
            let buckets = calculateHistogram numbers min max 21
            
            {
                count = decimal count
                kurtosis = kurtosis
                min = min
                max = max
                mean = mean
                median = median
                skewness = skewness
                stdDev = stdDev
                buckets = buckets
            }
                
                
    let PercentChanges multipleByHundred numbers =
        
        let percentChanges = 
            numbers 
            |> Seq.pairwise 
            |> Seq.map (fun (x, y) -> (y - x) / x)
            |> Seq.toArray

        let percentChanges = 
            if multipleByHundred then
                percentChanges
                |> Array.map (fun x -> x * 100m)
                |> Array.map (fun x -> System.Math.Round(x, 2))
            else
                percentChanges

        calculateStats percentChanges
        
    let PercentChangesForPriceBars priceBars =
        priceBars
        |> Array.map (fun (x:PriceBar) -> x.Close)
        |> PercentChanges true
    
    
    

