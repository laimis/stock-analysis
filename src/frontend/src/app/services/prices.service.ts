import {ChartMarker, ChartType, DataPoint, DataPointContainer, PriceBar} from "./stocks.service";
import {green, red} from "./charts.service";

export enum InfectionPointType { Peak = "Peak", Valley = "Valley" }

export type Gradient = { delta: number, index: number, dataPoint: PriceBar }
export type InflectionPoint = { gradient: Gradient, type: InfectionPointType, priceValue: number }

export function toPeak(gradient: Gradient): InflectionPoint {
    return {gradient: gradient, type: InfectionPointType.Peak, priceValue: gradient.dataPoint.close}
}

export function toValley(gradient: Gradient): InflectionPoint {
    return {gradient: gradient, type: InfectionPointType.Valley, priceValue: gradient.dataPoint.close}
}

export function toChartMarker(inflectionPoint: InflectionPoint): ChartMarker {
    const bar = inflectionPoint.gradient.dataPoint
    return {
        label: inflectionPoint.priceValue.toFixed(2),
        date: bar.dateStr,
        color: inflectionPoint.type === InfectionPointType.Valley ? green : red,
        shape: inflectionPoint.type === InfectionPointType.Valley ? 'arrowUp' : 'arrowDown'
    }
}

function nextDay(currentDate: string): string {
    let d = new Date(currentDate + " 23:00:00") // adding hours to make sure that when input is treated as midnight UTC, it is still the same day as the input
    let date = new Date(d.getFullYear(), d.getMonth(), d.getDate())
    date.setDate(date.getDate() + 1)
    return date.toISOString().substring(0, 10)
}

function filterBySignificance(points: InflectionPoint[], minPercentChange = 0.03, minAge = 5) {
    // Filter out minor fluctuations
    let significant: InflectionPoint[] = [];
    if (points.length <= 1) return points;
    
    significant.push(points[0]);
    
    for (let i = 1; i < points.length; i++) {
        const lastPoint = significant[significant.length - 1];
        const percentChange = Math.abs(
            (points[i].priceValue - lastPoint.priceValue) / lastPoint.priceValue
        );
        const age = Math.abs(points[i].gradient.index - lastPoint.gradient.index);
        
        if (percentChange >= minPercentChange && age >= minAge) {
            significant.push(points[i]);
        }
    }
    
    return significant;
}

export function toDailyBreakdownDataPointCointainer(label: string, points: InflectionPoint[], priceFunc?): DataPointContainer {

    if (priceFunc) {
        points = points.map(p => {
            let bar: PriceBar = {
                dateStr: p.gradient.dataPoint.dateStr,
                close: priceFunc(p.gradient.dataPoint.close),
                open: priceFunc(p.gradient.dataPoint.open),
                high: priceFunc(p.gradient.dataPoint.high),
                low: priceFunc(p.gradient.dataPoint.low),
                volume: p.gradient.dataPoint.volume
            }
            let newGradient: Gradient = {
                dataPoint: bar,
                delta: p.gradient.delta,
                index: p.gradient.index
            }
            return toValley(newGradient)
        })
    }

    // we want to generate a data point for each day, even if there is no peak or valley
    // start with the first date and keep on going until we reach the last date
    let currentDate = points[0].gradient.dataPoint.dateStr
    let currentIndex = 0
    let dataPoints: DataPoint[] = []

    while (currentIndex < points.length) {

        if (currentDate == points[currentIndex].gradient.dataPoint.dateStr) {

            dataPoints.push({
                label: points[currentIndex].gradient.dataPoint.dateStr,
                isDate: true,
                value: points[currentIndex].priceValue
            })

            currentIndex++

        } else {

            dataPoints.push({
                label: currentDate,
                isDate: true,
                value: points[currentIndex - 1].priceValue
            })

        }
        currentDate = nextDay(currentDate)
    }

    return {
        label: label,
        chartType: ChartType.Scatter,
        data: dataPoints
    }
}

function calculateVolatility(prices: PriceBar[], window = 10): number[] {
    const volatility: number[] = [];
    
    // Need at least window+1 prices to calculate window volatilities
    if (prices.length <= window) {
        return Array(prices.length).fill(0);
    }
    
    for (let i = window; i < prices.length; i++) {
        // Get price changes in the window
        const priceChanges: number[] = [];
        for (let j = i - window + 1; j <= i; j++) {
            if (j > 0) {
                priceChanges.push(Math.abs(prices[j].close - prices[j-1].close));
            }
        }
        
        // Calculate standard deviation
        const mean = priceChanges.reduce((sum, val) => sum + val, 0) / priceChanges.length;
        const squaredDiffs = priceChanges.map(val => Math.pow(val - mean, 2));
        const variance = squaredDiffs.reduce((sum, val) => sum + val, 0) / squaredDiffs.length;
        const stdDev = Math.sqrt(variance);
        
        volatility.push(stdDev);
    }
    
    // Pad the beginning with the first calculated volatility
    const firstVol = volatility[0] || 0;
    return Array(window).fill(firstVol).concat(volatility);
}

function calculateInflectionPointsVolatilitySmoothing(prices: PriceBar[]) {
    // First calculate price differences
    let diffs: Gradient[] = [];
    for (let i = 0; i < prices.length - 1; i++) {
        let diff = prices[i + 1].close - prices[i].close;
        let val: Gradient = {dataPoint: prices[i], delta: diff, index: i};
        diffs.push(val);
    }
    
    // Calculate volatility for the entire price series
    const volatility = calculateVolatility(prices);
    
    // Normalize volatility to a range suitable for smoothing factor
    const maxVol = Math.max(...volatility);
    const minVol = Math.min(...volatility);
    const normalizedVol = volatility.map(vol => {
        if (maxVol === minVol) return 0.5; // Default if all volatility values are the same
        return 0.2 + 0.6 * ((vol - minVol) / (maxVol - minVol)); // Scale to 0.2-0.8 range
    });
    
    // Apply adaptive smoothing
    let smoothed: Gradient[] = [];
    for (let i = 0; i < diffs.length; i++) {
        if (i === 0 || i === diffs.length - 1) {
            // Handle edge cases - first and last point
            smoothed.push(diffs[i]);
            continue;
        }
        
        // Get adaptive smoothing factor based on normalized volatility
        // Higher volatility = higher smoothing factor
        const smoothingFactor = normalizedVol[i];
        
        // Calculate exponential moving average style smoothing
        // Uses current point and previous smoothed point
        const prevSmoothed = smoothed.length > 0 ? smoothed[smoothed.length - 1].delta : diffs[i-1].delta;
        const currentDiff = diffs[i].delta;
        const smoothedDiff = (currentDiff * (1 - smoothingFactor)) + (prevSmoothed * smoothingFactor);
        
        smoothed.push({
            dataPoint: diffs[i].dataPoint, 
            delta: smoothedDiff, 
            index: i
        });
    }
    
    // Detect inflection points
    let inflectionPoints: InflectionPoint[] = [];
    for (let i = 0; i < smoothed.length - 1; i++) {
        if (smoothed[i].delta > 0 && smoothed[i + 1].delta < 0) {
            inflectionPoints.push(toPeak(smoothed[i + 1]));
        } else if (smoothed[i].delta < 0 && smoothed[i + 1].delta > 0) {
            inflectionPoints.push(toValley(smoothed[i + 1]));
        }
    }
    
    return filterBySignificance(inflectionPoints);
}

function calculateInflectionPointsFixedSmoothing(prices: PriceBar[], smoothingFactor = 0.5) {
    // First calculate price differences
    let diffs: Gradient[] = [];
    for (let i = 0; i < prices.length - 1; i++) {
        let diff = prices[i + 1].close - prices[i].close;
        let val: Gradient = {dataPoint: prices[i], delta: diff, index: i};
        diffs.push(val);
    }

    // Apply fixed smoothing factor
    let smoothed: Gradient[] = [];
    for (let i = 0; i < diffs.length; i++) {
        if (i === 0 || i === diffs.length - 1) {
            // Handle edge cases - first and last point
            smoothed.push(diffs[i]);
            continue;
        }

        // Calculate exponential moving average style smoothing
        // Uses current point and previous smoothed point
        const prevSmoothed = smoothed.length > 0 ? smoothed[smoothed.length - 1].delta : diffs[i-1].delta;
        const currentDiff = diffs[i].delta;
        const smoothedDiff = (currentDiff * (1 - smoothingFactor)) + (prevSmoothed * smoothingFactor);

        smoothed.push({
            dataPoint: diffs[i].dataPoint,
            delta: smoothedDiff,
            index: i
        });
    }

    // Detect inflection points
    let inflectionPoints: InflectionPoint[] = [];
    for (let i = 0; i < smoothed.length - 1; i++) {
        if (smoothed[i].delta > 0 && smoothed[i + 1].delta < 0) {
            inflectionPoints.push(toPeak(smoothed[i + 1]));
        } else if (smoothed[i].delta < 0 && smoothed[i + 1].delta > 0) {
            inflectionPoints.push(toValley(smoothed[i + 1]));
        }
    }

    return filterBySignificance(inflectionPoints);
}

function calculateInflectionPointsSimpleAverageSmoothing(prices: PriceBar[]) {
    let diffs: Gradient[] = []

    for (let i = 0; i < prices.length - 1; i++) {
        let diff = prices[i + 1].close - prices[i].close
        let val: Gradient = {dataPoint: prices[i], delta: diff, index: i}
        diffs.push(val);
    }

    let smoothed: Gradient[] = []
    for (let i = 0; i < diffs.length - 1; i++) {
        if (i == 0) {
            let smoothedDiff = (diffs[i].delta + diffs[i + 1].delta) / 2
            smoothed.push({dataPoint: diffs[i].dataPoint, delta: smoothedDiff, index: i});
            continue;
        }
        let smoothedDiff = (diffs[i - 1].delta + diffs[i].delta + diffs[i + 1].delta) / 3
        smoothed.push({dataPoint: diffs[i].dataPoint, delta: smoothedDiff, index: i});
    }

    let inflectionPoints: InflectionPoint[] = []

    for (let i = 0; i < smoothed.length - 1; i++) {
        if (smoothed[i].delta > 0 && smoothed[i + 1].delta < 0) {
            inflectionPoints.push(toPeak(smoothed[i + 1]));
        } else if (smoothed[i].delta < 0 && smoothed[i + 1].delta > 0) {
            inflectionPoints.push(toValley(smoothed[i + 1]));
        }
    }

    return filterBySignificance(inflectionPoints);
}

// main thing
export function calculateInflectionPoints(prices: PriceBar[]) {
    return calculateInflectionPointsFixedSmoothing(prices);
    // return calculateInflectionPointsVolatilitySmoothing(prices);
}

// this function will traverse the inflection points and calculate
// the change from peak to valley and valley to peak, and how many days
// have past between the points
export type InflectionPointLog = {
    from: InflectionPoint,
    to: InflectionPoint,
    days: number,
    change: number,
    percentChange: number
}

export function toInflectionPointLog(inflectionPoints: InflectionPoint[]): InflectionPointLog[] {

    let log: InflectionPointLog[] = []
    let i = 1
    while (i < inflectionPoints.length) {
        let point1 = inflectionPoints[i - 1]
        let point2 = inflectionPoints[i]
        let days = (new Date(point2.gradient.dataPoint.dateStr).getTime() - new Date(point1.gradient.dataPoint.dateStr).getTime()) / (1000 * 3600 * 24)
        let change = point2.gradient.dataPoint.close - point1.gradient.dataPoint.close
        let percentChange = change / point2.gradient.dataPoint.close
        log.push({from: point1, to: point2, days: days, change: change, percentChange: percentChange})
        i += 1
    }
    return log
}

export function toHistogram(inflectionPoints: InflectionPoint[]) {
    // build a histogram of the inflection points
    // the histogram will have a key for each price
    // and the value will be the number of times that price was hit
    let histogram = {}

    for (let i = 0; i < inflectionPoints.length; i++) {
        let price = inflectionPoints[i].priceValue

        // we will round the price to the nearest dollar
        let rounded = Math.round(price)

        if (histogram[rounded]) {
            histogram[rounded] += 1
        } else {
            histogram[rounded] = 1
        }
    }

    return histogram
}

export function histogramToDataPointContainer(title: string, histogram: {}) {
    let dataPoints: DataPoint[] = []
    for (let key in histogram) {
        dataPoints.push({
            label: key,
            isDate: false,
            value: histogram[key]
        })
    }
    return {
        label: title,
        chartType: ChartType.Column,
        data: dataPoints
    }
}

export function age(point: InflectionPoint): number {
    const date = new Date(point.gradient.dataPoint.dateStr).getTime()
    const now = new Date().getTime()
    return (now - date) / (1000 * 3600 * 24)
}

export function logToDataPointContainer(label: string, log: InflectionPointLog[]) {
    let dataPoints: DataPoint[] = []
    for (let i = 0; i < log.length; i++) {
        let point = log[i]
        dataPoints.push({
            label: point.from.gradient.dataPoint.dateStr,
            isDate: true,
            value: Math.round(point.percentChange * 100)
        })
    }
    return {
        label: label,
        chartType: ChartType.Column,
        data: dataPoints
    }
}

// TREND FUNCTIONS
export enum TrendDirection {
    Uptrend = "Uptrend",
    Downtrend = "Downtrend",
    Sideways = "Sideways",
    InsufficientData = "Insufficient data"
}

export type TrendDirectionAndStrength = {
    direction: TrendDirection;
    strength: number;
}

export type TrendAnalysisDetails = {
    slopeAnalysis: TrendDirectionAndStrength;
    patternAnalysis: TrendDirectionAndStrength;
    rangeAnalysis: TrendDirectionAndStrength;
    strengthAnalysis: TrendDirectionAndStrength;
}

export interface TrendAnalysisResult {
    trend: TrendDirection;
    confidence: number;
    details: TrendAnalysisDetails;
}

export interface TrendChangeAlert {
    detected: boolean;
    direction: TrendDirection;
    strength: number; // 0-1 scale
    evidence: string[];
  }
  

function linearRegressionAnalysis(inflectionPoints: InflectionPoint[], minPoints = 8): TrendDirectionAndStrength {
    if (inflectionPoints.length < minPoints) return { direction: TrendDirection.InsufficientData, strength: 0 };
    
    // Extract recent points (last N points)
    const recentPoints = inflectionPoints.slice(-minPoints);
    
    // Prepare data for regression
    const x: number[] = recentPoints.map((p, i) => i);
    const y: number[] = recentPoints.map(p => p.priceValue);
    
    // Calculate linear regression slope
    const n = x.length;
    let sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
    
    for (let i = 0; i < n; i++) {
        sumX += x[i];
        sumY += y[i];
        sumXY += x[i] * y[i];
        sumX2 += x[i] * x[i];
    }
    
    const slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
    const threshold = 0.005; // Adjust based on sensitivity needed
    
    if (slope > threshold) return { direction: TrendDirection.Uptrend, strength: 1 };
    if (slope < -threshold) return { direction: TrendDirection.Downtrend, strength: 1 };
    return { direction: TrendDirection.Sideways, strength: 1 };
}

function analyzePeaksAndValleys(inflectionPoints: InflectionPoint[], lookback = 4): TrendDirectionAndStrength {
    if (inflectionPoints.length < lookback * 2) return {direction: TrendDirection.InsufficientData, strength: 0};
    
    // Extract peaks and valleys
    const peaks = inflectionPoints.filter(p => p.type === InfectionPointType.Peak).slice(-lookback);
    const valleys = inflectionPoints.filter(p => p.type === InfectionPointType.Valley).slice(-lookback);
    
    // Check if we have at least 2 peaks and 2 valleys
    if (peaks.length < 2 || valleys.length < 2) return {direction: TrendDirection.InsufficientData, strength: 0};
    
    // Check for higher highs and higher lows (uptrend)
    const higherHighs = areIncreasing(peaks.map(p => p.priceValue));
    const higherLows = areIncreasing(valleys.map(p => p.priceValue));
    
    // Check for lower highs and lower lows (downtrend)
    const lowerHighs = areDecreasing(peaks.map(p => p.priceValue));
    const lowerLows = areDecreasing(valleys.map(p => p.priceValue));
    
    if (higherHighs && higherLows) return {direction: TrendDirection.Uptrend, strength: 1};
    if (lowerHighs && lowerLows) return {direction: TrendDirection.Downtrend, strength: 1};
    if (higherHighs || higherLows) return {direction: TrendDirection.Uptrend, strength: 0.5};
    if (lowerHighs || lowerLows) return {direction: TrendDirection.Downtrend, strength: 0.5};
    
    return {direction: TrendDirection.Sideways, strength: 0};
}

function areIncreasing(values: number[]): boolean {
    for (let i = 1; i < values.length; i++) {
        if (values[i] <= values[i-1]) return false;
    }
    return true;
}

function areDecreasing(values: number[]): boolean {
    for (let i = 1; i < values.length; i++) {
        if (values[i] >= values[i-1]) return false;
    }
    return true;
}

// NOTE: not used, keeping it to think about it if to add it or not
function analyzePercentChanges(inflectionPoints: InflectionPoint[], threshold = 0.03): TrendDirectionAndStrength {
    if (inflectionPoints.length < 4) return { direction: TrendDirection.InsufficientData, strength: 0 };
    
    // Calculate cumulative percentage change over the most recent inflection points
    let cumulativeChange = 0;
    for (let i = 1; i < inflectionPoints.length; i++) {
        const change = (inflectionPoints[i].priceValue - inflectionPoints[i-1].priceValue) / 
                        inflectionPoints[i-1].priceValue;
        cumulativeChange += change;
    }
    
    // Look at the total change from first to last point
    const totalChange = (inflectionPoints[inflectionPoints.length-1].priceValue - 
                         inflectionPoints[0].priceValue) / 
                         inflectionPoints[0].priceValue;
    
    // Adjust these thresholds based on your sensitivity preferences
    if (totalChange > threshold) return { direction: TrendDirection.Uptrend, strength: 1 };
    if (totalChange < -threshold) return { direction: TrendDirection.Downtrend, strength: 1 };
    return { direction: TrendDirection.Sideways, strength: 1 };
}

function analyzeRange(inflectionPoints: InflectionPoint[]): TrendDirectionAndStrength {
    if (inflectionPoints.length < 4) return { direction: TrendDirection.InsufficientData, strength: 0 };
    
    // Find min and max prices
    const prices = inflectionPoints.map(p => p.priceValue);
    const minPrice = Math.min(...prices);
    const maxPrice = Math.max(...prices);
    
    // Calculate the range as a percentage of the average price
    const range = maxPrice - minPrice;
    const avgPrice = prices.reduce((sum, price) => sum + price, 0) / prices.length;
    const rangePercent = range / avgPrice;
    
    // If range is small relative to price, it's sideways
    if (rangePercent < 0.05) return {direction: TrendDirection.Sideways, strength: 1}; // 5% range threshold
    
    // Check position of latest price within the range
    const latestPrice = inflectionPoints[inflectionPoints.length-1].priceValue;
    const positionInRange = (latestPrice - minPrice) / range;
    
    if (positionInRange > 0.7) return { direction: TrendDirection.Uptrend, strength: 1 }; // Near the top of range
    if (positionInRange < 0.3) return { direction: TrendDirection.Downtrend, strength: 1 }; // Near the bottom of range
    return { direction: TrendDirection.Sideways, strength: 1 }; // In middle of range
}

function calculateTrendStrength(inflectionPoints: InflectionPoint[], numberOfPoints = 8): TrendDirectionAndStrength {
    if (inflectionPoints.length < numberOfPoints) return {direction: TrendDirection.InsufficientData, strength: 0};
    
    // Extract recent points
    const recentPoints = inflectionPoints.slice(-numberOfPoints);
    
    // 1. Calculate linear regression slope
    const x: number[] = recentPoints.map((p, i) => i);
    const y: number[] = recentPoints.map(p => p.priceValue);
    
    const n = x.length;
    let sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
    
    for (let i = 0; i < n; i++) {
        sumX += x[i];
        sumY += y[i];
        sumXY += x[i] * y[i];
        sumX2 += x[i] * x[i];
    }
    
    const slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
    
    // 2. Check for higher highs/lower lows
    const peaks = recentPoints.filter(p => p.type === InfectionPointType.Peak);
    const valleys = recentPoints.filter(p => p.type === InfectionPointType.Valley);
    
    let patternScore = 0;
    
    if (peaks.length >= 2) {
        // Check if peaks are increasing (uptrend) or decreasing (downtrend)
        const peakDiff = peaks[peaks.length-1].priceValue - peaks[0].priceValue;
        patternScore += Math.sign(peakDiff);
    }
    
    if (valleys.length >= 2) {
        // Check if valleys are increasing (uptrend) or decreasing (downtrend)
        const valleyDiff = valleys[valleys.length-1].priceValue - valleys[0].priceValue;
        patternScore += Math.sign(valleyDiff);
    }
    
    // 3. Calculate total percentage change
    const totalChange = (recentPoints[recentPoints.length-1].priceValue - 
                         recentPoints[0].priceValue) / 
                         recentPoints[0].priceValue;
    
    // 4. Normalize and combine factors
    const normalizedSlope = Math.min(Math.max(slope * 20, -1), 1); // Scale slope to [-1, 1]
    const normalizedChange = Math.min(Math.max(totalChange * 10, -1), 1); // Scale change to [-1, 1]
    const normalizedPattern = patternScore / 2; // Already in [-1, 1] range
    
    // Combine scores (can adjust weights as needed)
    const combinedScore = (normalizedSlope + normalizedChange + normalizedPattern) / 3;
    
    // Convert to trend and strength
    let direction: TrendDirection;
    if (combinedScore > 0.2) direction = TrendDirection.Uptrend;
    else if (combinedScore < -0.2) direction = TrendDirection.Downtrend;
    else direction = TrendDirection.Sideways;
    
    // Return both trend and strength [-1 to 1]
    return {
        direction: direction,
        strength: Math.abs(combinedScore)
    };
}

export function analyzeTrend(inflectionPoints: InflectionPoint[]): TrendAnalysisResult {
    if (inflectionPoints.length < 8) {
        return {
            trend: TrendDirection.InsufficientData,
            confidence: 0,
            details: {
                slopeAnalysis: {direction: TrendDirection.InsufficientData, strength: 0},
                patternAnalysis: {direction: TrendDirection.InsufficientData, strength: 0},
                rangeAnalysis: {direction: TrendDirection.InsufficientData, strength: 0},
                strengthAnalysis: {direction: TrendDirection.InsufficientData, strength: 0}
            }
        };
    }
    
    // Get results from different methods
    const slopeResult = linearRegressionAnalysis(inflectionPoints);
    const patternResult = analyzePeaksAndValleys(inflectionPoints);
    const rangeResult = analyzeRange(inflectionPoints);
    const strengthResult = calculateTrendStrength(inflectionPoints);
    
    // Vote for the final trend
    let upVotes = 0, downVotes = 0, sidewaysVotes = 0;
    
    [slopeResult, patternResult, rangeResult, strengthResult].forEach(result => {
        if (result.direction === TrendDirection.Uptrend) {
            upVotes += result.strength;
        } else if (result.direction === TrendDirection.Downtrend) {
            downVotes += result.strength;
        } else {
            sidewaysVotes += result.strength;
        }
    });
    
    let finalTrend: TrendDirection;
    let confidence: number;
    let numberOfMethods = 4;
    
    if (upVotes > downVotes && upVotes > sidewaysVotes) {
        finalTrend = TrendDirection.Uptrend;
        confidence = upVotes;
    } else if (downVotes > upVotes && downVotes > sidewaysVotes) {
        finalTrend = TrendDirection.Downtrend;
        confidence = downVotes;
    } else {
        finalTrend = TrendDirection.Sideways;
        confidence = sidewaysVotes;
    }
    
    // Adjust confidence by trend strength
    const adjustedConfidence = (confidence / numberOfMethods + strengthResult.strength) / 2;
    
    return {
        trend: finalTrend,
        confidence: adjustedConfidence,
        details: {
            slopeAnalysis: slopeResult,
            patternAnalysis: patternResult,
            rangeAnalysis: rangeResult,
            strengthAnalysis: strengthResult
        }
    };
}

export function detectPotentialTrendChange(
    inflectionPoints: InflectionPoint[], 
    latestPrice: number,
    latestBar: PriceBar
  ): TrendChangeAlert {
    if (inflectionPoints.length < 4) {
      return { detected: false, direction: null, strength: 0, evidence: ["Insufficient data"] };
    }
    
    // Extract recent peaks and valleys
    const recentPeaks = inflectionPoints
      .filter(p => p.type === InfectionPointType.Peak)
      .slice(-3);
    
    const recentValleys = inflectionPoints
      .filter(p => p.type === InfectionPointType.Valley)
      .slice(-3);
    
    const lastInflectionPoint = inflectionPoints[inflectionPoints.length - 1];
    const daysSinceLastInflection = (new Date(latestBar.dateStr).getTime() - 
                                    new Date(lastInflectionPoint.gradient.dataPoint.dateStr).getTime()) / 
                                    (1000 * 3600 * 24);
    
    let evidence: string[] = [];
    let bullishStrength = 0;
    let bearishStrength = 0;
    
    // Track BULLISH signals
    
    // Check if latest price is breaking above recent peaks (bullish)
    const peaksExceeded = recentPeaks.filter(p => latestPrice > p.priceValue);
    if (peaksExceeded.length > 0) {
      evidence.push(`BULLISH: Price (${latestPrice.toFixed(2)}) exceeds ${peaksExceeded.length} of the last ${recentPeaks.length} peaks`);
      bullishStrength += peaksExceeded.length / recentPeaks.length * 0.5;
    }
    
    // Check if price is rising significantly from last valley
    if (recentValleys.length > 0) {
      const lastValley = recentValleys[recentValleys.length - 1];
      const percentRise = (latestPrice - lastValley.priceValue) / lastValley.priceValue;
      
      if (percentRise > 0.05) { // 5% rise from valley
        evidence.push(`BULLISH: Price has risen ${(percentRise * 100).toFixed(1)}% from last valley`);
        bullishStrength += Math.min(percentRise, 0.2) * 2.5; // Cap at 0.5 contribution
      }
    }
    
    // Check for higher lows pattern (bullish)
    if (recentValleys.length >= 2) {
      const increasing = areIncreasing(recentValleys.map(v => v.priceValue));
      if (increasing) {
        evidence.push("BULLISH: Pattern of higher lows detected");
        bullishStrength += 0.2;
      }
    }
    
    // Track BEARISH signals
    
    // Check if latest price is breaking below recent valleys (bearish)
    const valleysExceeded = recentValleys.filter(v => latestPrice < v.priceValue);
    if (valleysExceeded.length > 0) {
      evidence.push(`BEARISH: Price (${latestPrice.toFixed(2)}) is below ${valleysExceeded.length} of the last ${recentValleys.length} valleys`);
      bearishStrength += valleysExceeded.length / recentValleys.length * 0.5;
    }
    
    // Check if price is falling significantly from last peak
    if (recentPeaks.length > 0) {
      const lastPeak = recentPeaks[recentPeaks.length - 1];
      const percentFall = (lastPeak.priceValue - latestPrice) / lastPeak.priceValue;
      
      if (percentFall > 0.05) { // 5% fall from peak
        evidence.push(`BEARISH: Price has fallen ${(percentFall * 100).toFixed(1)}% from last peak`);
        bearishStrength += Math.min(percentFall, 0.2) * 2.5; // Cap at 0.5 contribution
      }
    }
    
    // Check for lower highs pattern (bearish)
    if (recentPeaks.length >= 2) {
      const decreasing = areDecreasing(recentPeaks.map(p => p.priceValue));
      if (decreasing) {
        evidence.push("BEARISH: Pattern of lower highs detected");
        bearishStrength += 0.2;
      }
    }
    
    // Check time since last inflection point - neutral factor
    if (daysSinceLastInflection > 10) {
    //   evidence.push(`NEUTRAL: ${Math.floor(daysSinceLastInflection)} days since last inflection point (unusual duration)`);
      // This doesn't affect directional strength
    }
    
    // Determine final direction and strength
    let directionSignal: TrendDirection | null = null;
    let netStrength = 0;
    
    // Only set a direction if one signal is significantly stronger
    const SIGNIFICANCE_THRESHOLD = 0.2; // Minimum difference to consider one signal stronger
    
    if (bullishStrength > bearishStrength + SIGNIFICANCE_THRESHOLD) {
      directionSignal = TrendDirection.Uptrend;
      netStrength = bullishStrength;
    } else if (bearishStrength > bullishStrength + SIGNIFICANCE_THRESHOLD) {
      directionSignal = TrendDirection.Downtrend;
      netStrength = bearishStrength;
    } else {
      // If signals are contradictory and similar in strength, it's unclear
      if (bullishStrength > 0.3 && bearishStrength > 0.3) {
        evidence.push("CONFLICT: Strong but contradictory signals detected");
        netStrength = Math.max(bullishStrength, bearishStrength) * 0.5; // Reduce strength due to conflict
        directionSignal = TrendDirection.Sideways;
      } else {
        netStrength = Math.max(bullishStrength, bearishStrength);
        directionSignal = bullishStrength >= bearishStrength ? TrendDirection.Uptrend : TrendDirection.Downtrend;
      }
    }
    
    // Add strength details to evidence
    if (bullishStrength > 0 || bearishStrength > 0) {
      evidence.push(`Signal strength: Bullish=${bullishStrength.toFixed(2)}, Bearish=${bearishStrength.toFixed(2)}`);
    }
    
    // Determine if signal is strong enough
    const detected = netStrength >= 0.4; // Threshold for significance
    
    return {
      detected,
      direction: directionSignal,
      strength: Math.min(netStrength, 1), // Cap at 1
      evidence
    };
  }
  
  // Helper for combined analysis including trend change alerts
  export function getCompleteTrendAnalysis(
    inflectionPoints: InflectionPoint[],
    latestBar: PriceBar
  ): {
    establishedTrend: TrendAnalysisResult,
    potentialChange: TrendChangeAlert
  } {
    const trendAnalysis = analyzeTrend(inflectionPoints);
    const changeAlert = detectPotentialTrendChange(
      inflectionPoints, 
      latestBar.close, 
      latestBar
    );
    
    return {
      establishedTrend: trendAnalysis,
      potentialChange: changeAlert
    };
  }