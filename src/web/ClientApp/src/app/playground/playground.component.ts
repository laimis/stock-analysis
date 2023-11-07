import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {
  ChartMarker,
  ChartType, DataPoint,
  DataPointContainer,
  PositionChartInformation,
  PriceBar,
  PriceFrequency,
  Prices,
  StocksService
} from '../services/stocks.service';
import {GetErrors} from "../services/utils";
import {green, red} from "../shared/candlestick-chart/candlestick-chart.component";

type gradient = {delta:number,index:number,bar:PriceBar}

function toChartMarker(g:gradient) : ChartMarker {
  return {
    label: g.bar.close.toFixed(2),
    date: g.bar.dateStr,
    color: g.delta > 0 ? green : red,
    shape:  g.delta > 0 ? 'arrowUp' : 'arrowDown'
  }
}

function toDataPointContainer(label:string, g:gradient[]) : DataPointContainer {
  let dataPoints : DataPoint[] = g.map(p => {
    return {
      label: p.bar.dateStr,
      isDate: true,
      value: p.bar.close
    }
  })

  return {
    label: label,
    chartType: ChartType.Line,
    data: dataPoints
  }
}

function calculatePeaksAndValleys(prices:PriceBar[]) {
  let diffs : gradient[] = []

  for (let i = 0; i < prices.length - 1; i++) {
    let diff = prices[i+1].close - prices[i].close
    let val : gradient = {bar: prices[i], delta: diff, index: i}
    diffs.push(val);
  }

  let smoothed : gradient[] = []
  for (let i = 0; i < diffs.length - 1; i++) {
    if (i == 0) {
      let smoothedDiff = (diffs[i].delta + diffs[i+1].delta) / 2
      smoothed.push({bar: diffs[i].bar, delta: smoothedDiff, index: i});
      continue;
    }
    let smoothedDiff = (diffs[i-1].delta + diffs[i].delta + diffs[i+1].delta) / 3
    smoothed.push({bar: diffs[i].bar, delta: smoothedDiff, index: i});
  }

  let peakGradients: gradient[] = []
  let valleyGradients: gradient[] = []

  for (let i = 0; i < smoothed.length - 1; i++) {
    if (smoothed[i].delta > 0 && smoothed[i + 1].delta < 0) {
      peakGradients.push(smoothed[i + 1]);
    } else if (smoothed[i].delta < 0 && smoothed[i + 1].delta > 0) {
      valleyGradients.push(smoothed[i + 1]);
    }
  }

  return {peakGradients, valleyGradients};
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

        const peaksAndValleys = calculatePeaksAndValleys(result.prices);
        const peaks = peaksAndValleys.peakGradients
        const valleys = peaksAndValleys.valleyGradients

        let markers = []
        peaks.map(toChartMarker).forEach(p => markers.push(p))
        valleys.map(toChartMarker).forEach(v => markers.push(v))

        this.chartInfo = {
          ticker: this.tickers[0],
          prices: result,
          markers: markers,
          averageBuyPrice: null,
          stopPrice: null
        }

        this.peakContainer = toDataPointContainer('peaks', peaks)
        this.valleyContainer = toDataPointContainer('valleys', valleys)
      },
      error => this.errors = GetErrors(error)
    );
  }
  priceFrequencyChanged() {
    this.chartInfo = null
    this.renderPrices(this.tickers);
  }

}
