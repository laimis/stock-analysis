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

function toDailyBreakdownDataPointCointainer(label:string, points:InflectionPoint[]) : DataPointContainer {

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

  peakContainer: DataPointContainer;
  valleyContainer: DataPointContainer;
  peaksAndValleys: DataPointContainer[];

  constructor(
    private stocks:StocksService,
    private route:ActivatedRoute) { }

  errors: string[];

  ngOnInit() {
    const tickerParam = this.route.snapshot.queryParamMap.get('tickers');
    if (tickerParam) {
      this.tickers = tickerParam.split(',');
      this.renderPrices(this.tickers)
    }
  }

  renderPrices(tickers:string[]) {
    this.stocks.getStockPrices(tickers[0], 365, this.priceFrequency).subscribe(
      result => {
        this.prices = result

        const inflectionPoints = calculateInflectionPoints(result.prices);

        let markers = []
        inflectionPoints.map(toChartMarker).forEach(p => markers.push(p))

        this.chartInfo = {
          ticker: this.tickers[0],
          prices: result,
          markers: markers,
          averageBuyPrice: null,
          stopPrice: null
        }

        this.peakContainer = toDailyBreakdownDataPointCointainer('peaks', inflectionPoints.filter(p => p.type === InfectionPointType.Peak))
        this.valleyContainer = toDailyBreakdownDataPointCointainer('valleys', inflectionPoints.filter(p => p.type === InfectionPointType.Valley))
        this.peaksAndValleys = [this.peakContainer, this.valleyContainer]
      },
      error => this.errors = GetErrors(error)
    );
  }
  priceFrequencyChanged() {
    this.chartInfo = null
    this.renderPrices(this.tickers);
  }

}
