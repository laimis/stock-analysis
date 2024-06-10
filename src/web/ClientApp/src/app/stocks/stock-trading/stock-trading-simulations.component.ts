import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {
    PositionInstance,
    PriceBar,
    PriceFrequency,
    StocksService,
    TradingStrategyPerformance
} from '../../services/stocks.service';
import {GetErrors} from 'src/app/services/utils';
import {StockPositionsService} from "../../services/stockpositions.service";
import {catchError, concatAll, tap} from "rxjs/operators";
import {concat, forkJoin} from "rxjs";

@Component({
    selector: 'app-stock-trading-simulations',
    templateUrl: './stock-trading-simulations.component.html',
    styleUrls: ['./stock-trading-simulations.component.css']
})

export class StockTradingSimulationsComponent implements OnInit {
    results: TradingStrategyPerformance[];
    errors: string[];
    numberOfTrades: number = 10;
    closePositions: boolean = true;
    loading: boolean = false;
    benchmarks: {ticker: string, prices: PriceBar[] }[] = [];

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
        this.benchmarks = [];
        this.stockPositions.simulatePositions(this.closePositions, this.numberOfTrades).subscribe(results => {
            this.results = results.sort((a, b) => b.performance.profit - a.performance.profit);
            this.loading = false;
            this.fetchBenchmarks();
        }, error => {
            this.errors = GetErrors(error)
            this.loading = false;
        });
    }
    
    selectedStrategy:string = 'Actual trades ⭐';
    getSelectedStrategyEntry() {
        return this.getStrategyEntry(this.selectedStrategy);
    }
    getStrategyEntry(name:string) {
        return this.results.find(result => result.strategyName === name)
    }
    findActualTrade(position:PositionInstance) {
        return this.getStrategyEntry("Actual trades ⭐").positions.find(p => p.ticker === position.ticker && p.opened === position.opened)
    }
    
    biggestWinnersComparedToActual() {
        const positions = this.getSelectedStrategyEntry().positions.slice();
        
        positions.sort((a, b) => {
                let bActualProfit = this.findActualTrade(b).profit;
                let aActualProfit = this.findActualTrade(a).profit;
                return (b.profit - bActualProfit) - (a.profit - aActualProfit)
            }
        );
        
        return positions.slice(0, 20);
    }

    fetchBenchmarks() {
        const earliest = this.results[0].performance.earliestDate;
        const latest = this.results[0].performance.latestDate;
        
        // take only date portion of the date
        const earliestDate = earliest.substring(0, earliest.indexOf('T'));
        const latestDate = latest.substring(0, latest.indexOf('T'));
        
        let benchmarkTickers = ["SPY", "QQQ", "ARKK", "IWM"];
        let observables = benchmarkTickers.map(benchmark =>
            this.stocks.getStockPricesForDates(benchmark, PriceFrequency.Daily, earliestDate, latestDate).pipe(
                tap(
                    prices => {
                        this.benchmarks.push({ticker: benchmark, prices: prices.prices});
                    }
                ),
                catchError(error => {
                    this.errors = GetErrors(error);
                    return [];
                })
            )
        )
        
        // wait for all observables to complete
        forkJoin(observables).subscribe(
            () => {
                console.log('Benchmark prices fetched:', this.benchmarks);
            },
            error => {
                // Handle any errors that occurred
                console.error('Error fetching benchmark prices:', error);
            }
        );
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

