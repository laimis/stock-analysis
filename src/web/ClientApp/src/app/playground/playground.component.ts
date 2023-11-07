import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {
  ChartMarker,
  DataPointContainer,
  PositionChartInformation,
  PriceBar, PriceFrequency,
  Prices,
  StocksService
} from '../services/stocks.service';
import {GetErrors} from "../services/utils";
import {green, red} from "../shared/candlestick-chart/candlestick-chart.component";

type gradient = {delta:number,index:number,bar:PriceBar}

@Component({
  selector: 'app-playground',
  templateUrl: './playground.component.html',
  styleUrls: ['./playground.component.css']
})

export class PlaygroundComponent implements OnInit {
  tickers: string[];
  options: any;
  prices: Prices;
  container: DataPointContainer;
  chartInfo: PositionChartInformation;
  priceFrequency: PriceFrequency = PriceFrequency.Daily;

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

        let peaksAndTroughs = this.calculatePeaksAndValleys(result.prices);

        let markers = []
        peaksAndTroughs.peaks.forEach(p => markers.push(p))
        peaksAndTroughs.valleys.forEach(t => markers.push(t))

        this.chartInfo = {
          ticker: this.tickers[0],
          prices: result,
          markers: markers,
          averageBuyPrice: null,
          stopPrice: null
        }
      },
      error => this.errors = GetErrors(error)
    );
  }
  priceFrequencyChanged() {
    this.chartInfo = null
    this.renderPrices(this.tickers);
  }


  calculatePeaksAndValleys(prices:PriceBar[]) {
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
      if (smoothed[i].delta > 0 && smoothed[i+1].delta < 0) {
        peakGradients.push(smoothed[i+1]);
      } else if (smoothed[i].delta < 0 && smoothed[i+1].delta > 0) {
        valleyGradients.push(smoothed[i+1]);
      }
    }

    let toChartMarker = (g:gradient) => {
      const marker: ChartMarker = {
        label: g.bar.close.toFixed(2), //g.delta.toFixed(2),
        date: g.bar.dateStr,
        color: g.delta > 0 ? green : red,
        shape:  g.delta > 0 ? 'arrowUp' : 'arrowDown'
      }
      return marker
    }

    let peaks = peakGradients.map(toChartMarker)
    let valleys = valleyGradients.map(toChartMarker)

    return {peaks, valleys};
  }
}
