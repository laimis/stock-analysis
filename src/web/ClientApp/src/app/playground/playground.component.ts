import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {
  DataPointContainer,
  PositionChartInformation,
  PriceBar, PriceFrequency,
  Prices,
  StocksService
} from '../services/stocks.service';
import {GetErrors} from "../services/utils";


@Component({
  selector: 'app-playground',
  templateUrl: './playground.component.html',
  styleUrls: ['./playground.component.css']
})

export class PlaygroundComponent implements OnInit {
  tickers: string[];
  options: any;
  prices: Prices;
  manualOptions: any;
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

  renderPrices(tickers) {
    this.stocks.getStockPrices(tickers[0], 365, this.priceFrequency).subscribe(
      result => {
        this.prices = result

        let peaksAndTroughs = this.calculatePeaksAndTroughs(result.prices);

        this.chartInfo = {
          ticker: this.tickers[0],
          prices: result,
          buyDates: [], //peaksAndTroughs.troughs,
          sellDates: peaksAndTroughs.peaks,
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


  calculatePeaksAndTroughs(prices:PriceBar[]) {
    let diff = []

    for (let i = 0; i < prices.length - 1; i++) {
      diff.push(prices[i+1].close - prices[i].close);
    }

    let smoothed = []
    for (let i = 0; i < diff.length - 1; i++) {
      if (i == 0) {
        smoothed.push((diff[i] + diff[i+1]) / 2);
        continue;
      }
      smoothed.push((diff[i-1] + diff[i] + diff[i+1]) / 3);
    }

    let peakIndexes = []
    let troughIndexes = []

    for (let i = 0; i < smoothed.length - 1; i++) {
      if (smoothed[i] > 0 && smoothed[i+1] < 0) {
        peakIndexes.push(i);
      } else if (smoothed[i] < 0 && smoothed[i+1] > 0) {
        troughIndexes.push(i);
      }
    }

    let peaks = peakIndexes.map(i => prices[i+1].dateStr);
    let troughs = troughIndexes.map(i => prices[i+1].dateStr);

    return {peaks, troughs};
  }
}

