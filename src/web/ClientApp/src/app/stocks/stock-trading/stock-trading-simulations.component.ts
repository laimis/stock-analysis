import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {PriceBar, PriceFrequency, StocksService, TradingStrategyPerformance} from '../../services/stocks.service';
import {GetErrors} from 'src/app/services/utils';
import {StockPositionsService} from "../../services/stockpositions.service";

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
    private stockPositions:StockPositionsService,
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

      this.stockPositions.simulatePositions(this.closePositions, this.numberOfTrades).subscribe(results => {
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

    this.stocks.getStockPricesForDates("SPY", PriceFrequency.Daily, earliestDate, latestDate).subscribe(prices => {
      this.spyPrices = prices.prices;
    });

    this.stocks.getStockPricesForDates("QQQ", PriceFrequency.Daily, earliestDate, latestDate).subscribe(prices => {
      this.qqqPrices = prices.prices;
    });
  }

  backgroundCssClassForActual(results:TradingStrategyPerformance[], strategyIndex: number, positionIndex: number) {
    const simulatedPosition = results[strategyIndex].positions[positionIndex];
    const actualPosition = results[0].positions[positionIndex];

    const simulatedProfit = simulatedPosition.profit;
    const actualProfit = actualPosition.profit;

    return actualProfit >= simulatedProfit ? 'bg-success' : '';
  }

  getExportUrl() {
    return this.stockPositions.simulatePositionsExportUrl(this.closePositions, this.numberOfTrades);
  }
}

