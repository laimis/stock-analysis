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
    numberOfTrades: number = 40;
    closePositions: boolean = true;
    loading: boolean = false;

    constructor(
        private stockPositions: StockPositionsService,
        private stocks: StocksService,
        private route: ActivatedRoute) {
    }

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

        });
    }
    
    simulateTrades() {
        this.loading = true
        this.stockPositions.simulatePositions(this.closePositions, this.numberOfTrades).subscribe(results => {
            this.results = results.sort((a, b) => b.performance.profit - a.performance.profit);
            this.loading = false;
            this.fetchSpyPrices();
        }, error => {
            this.errors = GetErrors(error)
            this.loading = false;
        });
    }

    fetchSpyPrices() {
        const earliest = this.results[0].performance.earliestDate;
        const latest = this.results[0].performance.latestDate;
        
        // take only date portion of the date
        const earliestDate = earliest.substring(0, earliest.indexOf('T'));
        const latestDate = latest.substring(0, latest.indexOf('T'));
        
        this.stocks.getStockPricesForDates("SPY", PriceFrequency.Daily, earliestDate, latestDate).subscribe(prices => {
            this.spyPrices = prices.prices;
        });

        this.stocks.getStockPricesForDates("QQQ", PriceFrequency.Daily, earliestDate, latestDate).subscribe(prices => {
            this.qqqPrices = prices.prices;
        });
    }

    backgroundCssClassForActual(results: TradingStrategyPerformance[], strategyIndex: number, positionIndex: number) {
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

