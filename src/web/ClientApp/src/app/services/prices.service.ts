import {ChartMarker, ChartType, DataPoint, DataPointContainer, PriceBar} from "./stocks.service";
import {green, red} from "../shared/candlestick-chart/candlestick-chart.component";

export enum InfectionPointType { Peak = "Peak", Valley = "Valley" }

export type Gradient = { delta: number, index: number, bar: PriceBar }
export type InflectionPoint = { gradient: Gradient, type: InfectionPointType, priceValue: number }

export function toPeak(gradient: Gradient): InflectionPoint {
  return {gradient: gradient, type: InfectionPointType.Peak, priceValue: gradient.bar.high}
}

export function toValley(gradient: Gradient): InflectionPoint {
  return {gradient: gradient, type: InfectionPointType.Valley, priceValue: gradient.bar.low}
}

export function toChartMarker(inflectionPoint: InflectionPoint): ChartMarker {
  const bar = inflectionPoint.gradient.bar
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

export function toDailyBreakdownDataPointCointainer(label: string, points: InflectionPoint[], priceFunc?): DataPointContainer {

  if (priceFunc) {
    points = points.map(p => {
      let bar: PriceBar = {
        dateStr: p.gradient.bar.dateStr,
        close: priceFunc(p.gradient.bar.close),
        open: priceFunc(p.gradient.bar.open),
        high: priceFunc(p.gradient.bar.high),
        low: priceFunc(p.gradient.bar.low),
        volume: p.gradient.bar.volume
      }
      let newGradient: Gradient = {
        bar: bar,
        delta: p.gradient.delta,
        index: p.gradient.index
      }
      return toValley(newGradient)
    })
  }

  // we want to generate a data point for each day, even if there is no peak or valley
  // start with the first date and keep on going until we reach the last date
  let currentDate = points[0].gradient.bar.dateStr
  let currentIndex = 0
  let dataPoints: DataPoint[] = []

  while (currentIndex < points.length) {

    if (currentDate == points[currentIndex].gradient.bar.dateStr) {

      dataPoints.push({
        label: points[currentIndex].gradient.bar.dateStr,
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
    chartType: ChartType.Line,
    data: dataPoints
  }
}

export function calculateInflectionPoints(prices: PriceBar[]) {
  let diffs: Gradient[] = []

  for (let i = 0; i < prices.length - 1; i++) {
    let diff = prices[i + 1].close - prices[i].close
    let val: Gradient = {bar: prices[i], delta: diff, index: i}
    diffs.push(val);
  }

  let smoothed: Gradient[] = []
  for (let i = 0; i < diffs.length - 1; i++) {
    if (i == 0) {
      let smoothedDiff = (diffs[i].delta + diffs[i + 1].delta) / 2
      smoothed.push({bar: diffs[i].bar, delta: smoothedDiff, index: i});
      continue;
    }
    let smoothedDiff = (diffs[i - 1].delta + diffs[i].delta + diffs[i + 1].delta) / 3
    smoothed.push({bar: diffs[i].bar, delta: smoothedDiff, index: i});
  }

  let inflectionPoints: InflectionPoint[] = []

  for (let i = 0; i < smoothed.length - 1; i++) {
    if (smoothed[i].delta > 0 && smoothed[i + 1].delta < 0) {
      inflectionPoints.push(toPeak(smoothed[i + 1]));
    } else if (smoothed[i].delta < 0 && smoothed[i + 1].delta > 0) {
      inflectionPoints.push(toValley(smoothed[i + 1]));
    }
  }

  return inflectionPoints;
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
    let days = (new Date(point2.gradient.bar.dateStr).getTime() - new Date(point1.gradient.bar.dateStr).getTime()) / (1000 * 3600 * 24)
    let change = point2.gradient.bar.close - point1.gradient.bar.close
    let percentChange = change / point2.gradient.bar.close
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
  const date = new Date(point.gradient.bar.dateStr).getTime()
  const now = new Date().getTime()
  return (now - date) / (1000 * 3600 * 24)
}

export function logToDataPointContainer(label: string, log: InflectionPointLog[]) {
  let dataPoints: DataPoint[] = []
  for (let i = 0; i < log.length; i++) {
    let point = log[i]
    dataPoints.push({
      label: point.from.gradient.bar.dateStr,
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

// TODO: this function does not belong here, another module would be good
export function humanFriendlyTime(ageValueToUse: number) {
  // check if we are dealing with years
  if (ageValueToUse > 365) {
    return Math.round(ageValueToUse / 365) + ' years'
  }

  // check if we are dealing with months
  if (ageValueToUse > 30) {
    return Math.round(ageValueToUse / 30) + ' months'
  }

  // check if we are dealing with weeks
  if (ageValueToUse > 7) {
    return Math.round(ageValueToUse / 7) + ' weeks'
  }

  // check if we are dealing with days
  return Math.round(ageValueToUse) + ' days'
}
