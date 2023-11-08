import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {
  ChartMarker,
  ChartType,
  DataPoint,
  DataPointContainer,
  PositionChartInformation,
  PriceBar,
  PriceFrequency,
  Prices,
  StocksService
} from '../services/stocks.service';
import {GetErrors} from "../services/utils";
import {green, red} from "../shared/candlestick-chart/candlestick-chart.component";

enum InfectionPointType { Peak= "Peak", Valley = "Valley"}
type Gradient = {delta:number,index:number,bar:PriceBar}
type InflectionPoint = {gradient:Gradient, type:InfectionPointType}

function toPeak(gradient:Gradient) : InflectionPoint {
  return {gradient:gradient, type:InfectionPointType.Peak}
}

function toValley(gradient:Gradient): InflectionPoint {
  return {gradient: gradient, type:InfectionPointType.Valley}
}

function toChartMarker(inflectionPoint:InflectionPoint) : ChartMarker {
  const bar = inflectionPoint.gradient.bar
  return {
    label: bar.close.toFixed(2),
    date: bar.dateStr,
    color: inflectionPoint.type === InfectionPointType.Valley ? green : red,
    shape: inflectionPoint.type === InfectionPointType.Valley ? 'arrowUp' : 'arrowDown'
  }
}

function nextDay(currentDate:string) : string {
  let d = new Date(currentDate + " 23:00:00") // adding hours to make sure that when input is treated as midnight UTC, it is still the same day as the input
  let date = new Date(d.getFullYear(), d.getMonth(), d.getDate())
  date.setDate(date.getDate() + 1)
  return date.toISOString().substring(0,10)
}

function toDailyBreakdownDataPointCointainer(label:string, points:InflectionPoint[], priceFunc?) : DataPointContainer {

  if (priceFunc) {
    points = points.map(p => {
      let bar : PriceBar = {
        dateStr: p.gradient.bar.dateStr,
        close: priceFunc(p.gradient.bar.close),
        open: priceFunc(p.gradient.bar.open),
        high: priceFunc(p.gradient.bar.high),
        low: priceFunc(p.gradient.bar.low),
        volume: p.gradient.bar.volume
      }
      let newGradient : Gradient = {
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
  let dataPoints : DataPoint[] = []

  while (currentIndex < points.length) {

    if (currentDate == points[currentIndex].gradient.bar.dateStr) {

      dataPoints.push({
        label: points[currentIndex].gradient.bar.dateStr,
        isDate: true,
        value: points[currentIndex].gradient.bar.close
      })

      currentIndex++

    } else {

      dataPoints.push({
        label: currentDate,
        isDate: true,
        value: points[currentIndex - 1].gradient.bar.close
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

function calculateInflectionPoints(prices:PriceBar[]) {
  let diffs : Gradient[] = []

  for (let i = 0; i < prices.length - 1; i++) {
    let diff = prices[i+1].close - prices[i].close
    let val : Gradient = {bar: prices[i], delta: diff, index: i}
    diffs.push(val);
  }

  let smoothed : Gradient[] = []
  for (let i = 0; i < diffs.length - 1; i++) {
    if (i == 0) {
      let smoothedDiff = (diffs[i].delta + diffs[i+1].delta) / 2
      smoothed.push({bar: diffs[i].bar, delta: smoothedDiff, index: i});
      continue;
    }
    let smoothedDiff = (diffs[i-1].delta + diffs[i].delta + diffs[i+1].delta) / 3
    smoothed.push({bar: diffs[i].bar, delta: smoothedDiff, index: i});
  }

  let inflectionPoints : InflectionPoint[] = []

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
type InflectionPointLog = {from:InflectionPoint, to:InflectionPoint, days:number, change:number, percentChange:number}
function toInflectionPointLog(inflectionPoints:InflectionPoint[]) : InflectionPointLog[] {

  let log : InflectionPointLog[] = []
  let i = 1
  while (i < inflectionPoints.length) {
    let point1 = inflectionPoints[i-1]
    let point2 = inflectionPoints[i]
    let days = (new Date(point2.gradient.bar.dateStr).getTime() - new Date(point1.gradient.bar.dateStr).getTime()) / (1000 * 3600 * 24)
    let change = point2.gradient.bar.close - point1.gradient.bar.close
    let percentChange = change / point2.gradient.bar.close
    log.push({from: point1, to: point2, days: days, change: change, percentChange: percentChange})
    i += 1
  }
  return log

}

@Component({
  selector: 'app-playground',
  templateUrl: './playground.component.html',
  styleUrls: ['./playground.component.css']
})

export class PlaygroundComponent implements OnInit {
  tickers: string[];
  options: any;
  prices: Prices;
  chartInfo: PositionChartInformation;
  priceFrequency: PriceFrequency = PriceFrequency.Daily;

  lineContainers: DataPointContainer[];
  peaksAndValleys: DataPointContainer[];
  log: InflectionPointLog[];

  constructor(
    private stocks:StocksService,
    private route:ActivatedRoute) { }

  errors: string[];

  ngOnInit() {
    const tickerParam = this.route.snapshot.queryParamMap.get('tickers');
    this.tickers = tickerParam ? tickerParam.split(',') : ['AMD'];
    this.renderPrices(this.tickers)
  }

  renderPrices(tickers:string[]) {
    this.stocks.getStockPrices(tickers[0], 365, this.priceFrequency).subscribe(
      result => {
        this.prices = result

        const inflectionPoints = calculateInflectionPoints(result.prices);
        this.log = toInflectionPointLog(inflectionPoints).reverse()

        this.chartInfo = {
          ticker: this.tickers[0],
          prices: result,
          markers: inflectionPoints.map(toChartMarker),
          averageBuyPrice: null,
          stopPrice: null
        }

        const peaks = toDailyBreakdownDataPointCointainer('peaks', inflectionPoints.filter(p => p.type === InfectionPointType.Peak))
        const valleys = toDailyBreakdownDataPointCointainer('valleys', inflectionPoints.filter(p => p.type === InfectionPointType.Valley))
        const smoothedPeaks = toDailyBreakdownDataPointCointainer(
          'smoothed peaks',
          inflectionPoints.filter(p => p.type === InfectionPointType.Peak), (p: number) => Math.round(p)
        )
        const smoothedValleys = toDailyBreakdownDataPointCointainer(
          'smoothed valleys',
          inflectionPoints.filter(p => p.type === InfectionPointType.Valley), (p: number) => Math.round(p)
        )

        this.lineContainers = [
          peaks, smoothedPeaks, valleys, smoothedValleys
        ]

        this.peaksAndValleys = [smoothedPeaks, smoothedValleys]
      },
      error => this.errors = GetErrors(error)
    );
  }
  priceFrequencyChanged() {
    this.chartInfo = null
    this.renderPrices(this.tickers);
  }

}
