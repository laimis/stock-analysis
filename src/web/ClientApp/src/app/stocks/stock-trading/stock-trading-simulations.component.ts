import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PriceBar, StocksService, TradingStrategyPerformance } from '../../services/stocks.service';
import { GetErrors } from 'src/app/services/utils';

@Component({
  selector: 'app-stock-trading-simulations',
  templateUrl: './stock-trading-simulations.component.html',
  styleUrls: ['./stock-trading-simulations.component.css']
})

export class StockTradingSimulationsComponent implements OnInit {
  results: TradingStrategyPerformance[];
  spyPrices: PriceBar[];
  qqqPrices: PriceBar[];
  errors: string[];

  constructor(
    private stocks:StocksService,
    private route:ActivatedRoute) { }

  numberOfTrades:number = 40;
  closePositions:boolean = true;

  ngOnInit() {
    this.route.queryParams.subscribe(queryParams => {
      const n = queryParams['n'];
      const closePositionsParam = queryParams['closePositions'];

      if (closePositionsParam) {
        this.closePositions = closePositionsParam === 'true';
      }

      if (n) {
        this.numberOfTrades = parseInt(n);
      }

      this.stocks.simulatePositions(this.closePositions, this.numberOfTrades).subscribe(results => {
        this.results = results.sort((a, b) => b.performance.profit - a.performance.profit);
        this.fetchSpyPrices();
      }, error => {
        this.errors = GetErrors(error)
      });
    });
  }
  fetchSpyPrices() {
    const earliestDate = this.results[0].performance.earliestDate;
    const latestDate = this.results[0].performance.latestDate;

    this.stocks.getStockPricesForDates("SPY", earliestDate, latestDate).subscribe(prices => {
      this.spyPrices = prices.prices;
    });

    this.stocks.getStockPricesForDates("QQQ", earliestDate, latestDate).subscribe(prices => {
      this.qqqPrices = prices.prices;
    });
  }

  backgroundCssClassForActual(results:TradingStrategyPerformance[], strategyIndex: number, positionIndex: number) {
    const simulatedPosition = results[strategyIndex].positions[positionIndex];
    const actualPosition = results[0].positions[positionIndex];

    const simulatedProfit = simulatedPosition.combinedProfit;
    const actualProfit = actualPosition.combinedProfit;

    return actualProfit >= simulatedProfit ? 'bg-success' : '';
  }

  getExportUrl() {
    return this.stocks.simulatePositionsExportUrl(this.closePositions, this.numberOfTrades);
  }
}

