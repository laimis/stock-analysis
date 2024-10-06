import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {
    PositionInstance,
    PriceBar,
    PriceFrequency,
    StocksService,
    TradingStrategyPerformance, TradingStrategyResult
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
    numberOfTrades: number = 40;
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

        }, error => {
            this.errors = GetErrors(error);
        });
    }
    
    simulateTrades() {
        this.loading = true
        this.errors = [];
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
    
    mainStrategy:string = 'Actual trades ⭐';
    alternateStrategy:string = '';
    
    getStrategyEntry(name:string) {
        return this.results.find(result => result.strategyName === name)
    }

    findTrade(position:PositionInstance) {
        let alternateStrategy = this.getStrategyEntry(this.alternateStrategy);
        return alternateStrategy.results.find(r => r.position.ticker === position.ticker && r.position.opened === position.opened)
    }
    
    biggestWinnersComparedToActual() {
        const positions = this.getStrategyEntry(this.mainStrategy).results.slice();
        
        positions.sort((a, b) => {
                let bActualProfit = this.findTrade(b.position).position.profit;
                let aActualProfit = this.findTrade(a.position).position.profit;
                return (b.position.profit - bActualProfit) - (a.position.profit - aActualProfit)
            }
        );
        
        return positions;
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
                this.errors = GetErrors(error);
            }
        );
    }

    backgroundCssClassForActual(results: TradingStrategyPerformance[], strategyIndex: number, positionIndex: number) {
        const simulatedPosition = results[strategyIndex].results[positionIndex];
        const actualPosition = results[0].results[positionIndex];

        const simulatedProfit = simulatedPosition.position.profit;
        const actualProfit = actualPosition.position.profit;

        return actualProfit >= simulatedProfit ? 'bg-success' : '';
    }

    getExportUrl() {
        return this.stockPositions.simulatePositionsExportUrl(this.closePositions, this.numberOfTrades);
    }
}

