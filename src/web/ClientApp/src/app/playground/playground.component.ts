import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {
    DailyPositionReport, DataPointContainer,
    PositionInstance,
    StockQuote,
    StocksService,
    TickerCorrelation, TradingStrategyResults
} from '../services/stocks.service';
import {GetErrors} from "../services/utils";
import {concat} from "rxjs";
import {tap} from "rxjs/operators";
import {StockPositionsService} from "../services/stockpositions.service";

// option-trade-builder.component.ts
interface Option {
    strike: number;
    expiration: string;
    type: 'call' | 'put';
    bid: number;
    ask: number;
    lastPrice: number;
    change: number;
    volume: number;
    openInterest: number;
    impliedVolatility: number;
    delta: number;
    gamma: number;
    theta: number;
    vega: number;
  }
  
  interface OptionLeg {
    option: Option;
    action: 'buy' | 'sell';
    quantity: number;
  }
   

function unrealizedProfit(position: PositionInstance, quote: StockQuote) {
    return position.profit + (quote.price - position.averageCostPerShare) * position.numberOfShares
}

function createProfitScatter(entries: PositionInstance[], quotes: Map<string, StockQuote>) {
    const mapped = entries.map(p => {
        return {x: p.daysHeld, y: unrealizedProfit(p, quotes[p.ticker]), label: p.ticker}
    })

    return {
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Position Profit / Days Held",
        },
        axisX: {
            title: "Days Held",
            // valueFormatString: "YYYY-MM-DD",
            gridThickness: 0.1,
        },
        axisY: {
            title: "Profit",
            gridThickness: 0.1,
        },
        data: [
            {
                type: "scatter",
                showInLegend: true,
                name: "Position",
                dataPoints: mapped
            }
        ]
    }
}

@Component({
    selector: 'app-playground',
    templateUrl: './playground.component.html',
    styleUrls: ['./playground.component.css']
})
export class PlaygroundComponent implements OnInit {
    tickers: string[];
    errors: string[];
    status: string;
    testTicker: string;
    Infinity = Infinity
    
    constructor(
        private stocks: StocksService,
        private stockPositions: StockPositionsService,
        private route: ActivatedRoute) {
    }

    ngOnInit() {
        const tickerParam = this.route.snapshot.queryParamMap.get('tickers')
        this.tickers = tickerParam ? tickerParam.split(',') : ['AMD']
        this.testTicker = this.tickers[0]

        this.loadOptions();
        this.applyFiltersAndSort();
    }

    correlations: TickerCorrelation[];
    daysForCorrelations: number = 60
    loadingCorrelations: boolean = false
    runCorrelations() {
        this.loadingCorrelations = true
        this.stocks.reportPortfolioCorrelations(this.daysForCorrelations).subscribe((data) => {
            this.correlations = data
            this.loadingCorrelations = false
        })
    }

    chartOptions: any[] = []
    loadingScatterPlot: boolean = false
    runScatterPlot() {
        this.loadingScatterPlot = true
        this.stockPositions.getTradingEntries().subscribe((data) => {
            const tradingPositions = data.current
            const quotes = data.prices

            const profitScatter = createProfitScatter(tradingPositions, quotes)

            this.chartOptions.push(profitScatter)
            this.loadingScatterPlot = false
        }, (error) => {
            this.errors = GetErrors(error)
            this.loadingScatterPlot = false
        })
    }
    
    loadingCombinedDailyChart: boolean = false
    dailyReport: DailyPositionReport
    runCombinedDailyChart() {
        this.loadingCombinedDailyChart = true
        this.stocks.reportDailyPositionReport("6d5e5329-ecc8-41c5-aba1-d94ed914885f").subscribe((data) => {
            this.dailyReport = data
            this.loadingCombinedDailyChart = false
        }, (error) => {
            this.errors = GetErrors(error)
            this.loadingCombinedDailyChart = false
        })
    }

    run() {

        this.status = "Running"

        let positionReport = this.stocks.reportPositions().pipe(
            tap((data) => {
                this.status = "Positions done"
                console.log("Positions")
                console.log(data)
            })
        )

        let singleBarDaily = this.stocks.reportOutcomesSingleBarDaily(this.tickers).pipe(
            tap((data) => {
                this.status = "Single bar daily done"
                console.log("Single bar daily")
                console.log(data)
            }, (error) => {
                console.log("Single bar daily error")
                this.errors = GetErrors(error)
            })
        )

        let multiBarDaily = this.stocks.reportOutcomesAllBars(this.tickers).pipe(
            tap((data) => {
                this.status = "Multi bar daily done"
                console.log("Multi bar daily")
                console.log(data)
            }, (error) => {
                console.log("Multi bar daily error")
                this.errors = GetErrors(error)
            })
        )

        let singleBarWeekly = this.stocks.reportOutcomesSingleBarWeekly(this.tickers).pipe(
            tap((data) => {
                this.status = "Single bar weekly done"
                console.log("Single bar weekly")
                console.log(data)
            })
        )

        let gapReport = this.stocks.reportTickerGaps(this.tickers[0]).pipe(
            tap((data) => {
                this.status = "Gaps done"
                console.log("Gaps")
                console.log(data)
            })
        )

        this.status = "Running..."

        concat([positionReport, gapReport, singleBarDaily, singleBarWeekly, multiBarDaily]).subscribe()
    }

    loadingActualVsSimulated: boolean = false
    actualVsSimulated: TradingStrategyResults
    runActualVsSimulated() {
        this.loadingActualVsSimulated = true
        this.stockPositions.simulatePosition("4fe57556-b6de-4c0c-b996-798e6159b2ce", true).subscribe((data) => {
            this.actualVsSimulated = data
            this.loadingActualVsSimulated = false
        }, (error) => {
            this.errors = GetErrors(error)
            this.loadingActualVsSimulated = false
        })
    }

    // option business
    ticker: string = 'AAPL'; // Example ticker
  options: Option[] = []; // Will be populated with stub data
  selectedLegs: OptionLeg[] = [];
  
  filteredOptions: Option[] = [];
  sortColumn: string = 'strike';
  sortDirection: 'asc' | 'desc' = 'asc';
  filterExpiration: string = '';
  filterType: 'all' | 'call' | 'put' = 'all';

  
  loadOptions(): void {
    // Stub data - replace with actual service call later
    this.options = [
        {
            strike: 150, expiration: '2023-06-16', type: 'call', bid: 5.10, ask: 5.20,
            lastPrice: 5.15, change: 0.25, volume: 1500, openInterest: 10000,
            impliedVolatility: 0.3, delta: 0.65, gamma: 0.04, theta: -0.05, vega: 0.1
          },
          {
            strike: 155, expiration: '2023-06-16', type: 'call', bid: 3.20, ask: 3.30,
            lastPrice: 3.25, change: 0.15, volume: 1200, openInterest: 8000,
            impliedVolatility: 0.28, delta: 0.55, gamma: 0.05, theta: -0.04, vega: 0.09
          },
      // Add more stub data as needed
    ];
  }

  applyFiltersAndSort(): void {
    this.filteredOptions = this.options.filter(option => {
      return (this.filterExpiration === '' || option.expiration === this.filterExpiration) &&
             (this.filterType === 'all' || option.type === this.filterType);
    });

    this.filteredOptions.sort((a, b) => {
      const aValue = a[this.sortColumn];
      const bValue = b[this.sortColumn];
      return this.sortDirection === 'asc' ? aValue - bValue : bValue - aValue;
    });
  }

  setSort(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
    this.applyFiltersAndSort();
  }

  getUniqueExpirations(): string[] {
    return this.options.reduce((expirations, option) => {
      if (!expirations.includes(option.expiration)) {
        expirations.push(option.expiration);
      }
      return expirations;
    }, []);
  }

  addLeg(option: Option): void {
    this.selectedLegs.push({ option, action: 'buy', quantity: 1 });
  }

  updateLegAction(index: number, action: 'buy' | 'sell'): void {
    this.selectedLegs[index].action = action;
  }

  updateLegQuantity(index: number, quantity: number): void {
    this.selectedLegs[index].quantity = quantity;
  }

  removeLeg(index: number): void {
    this.selectedLegs.splice(index, 1);
  }

  calculateTotalCost(): number {
    return this.selectedLegs.reduce((total, leg) => {
      const price = leg.action === 'buy' ? leg.option.ask : leg.option.bid;
      return total + (price * leg.quantity * 100); // Multiply by 100 for contract size
    }, 0);
  }

  calculateMaxProfit(): number {
    // This is a simplified calculation and may not be accurate for all strategies
    const longCalls = this.selectedLegs.filter(leg => leg.action === 'buy' && leg.option.type === 'call');
    const shortCalls = this.selectedLegs.filter(leg => leg.action === 'sell' && leg.option.type === 'call');
    const longPuts = this.selectedLegs.filter(leg => leg.action === 'buy' && leg.option.type === 'put');
    const shortPuts = this.selectedLegs.filter(leg => leg.action === 'sell' && leg.option.type === 'put');

    if (longCalls.length > 0 || longPuts.length > 0) {
      return Infinity; // Unlimited profit potential
    } else if (shortCalls.length > 0 || shortPuts.length > 0) {
      return this.calculateTotalCost(); // Max profit is the credit received
    }
    return 0;
  }

  calculateMaxLoss(): number {
    // This is a simplified calculation and may not be accurate for all strategies
    const longCalls = this.selectedLegs.filter(leg => leg.action === 'buy' && leg.option.type === 'call');
    const shortCalls = this.selectedLegs.filter(leg => leg.action === 'sell' && leg.option.type === 'call');
    const longPuts = this.selectedLegs.filter(leg => leg.action === 'buy' && leg.option.type === 'put');
    const shortPuts = this.selectedLegs.filter(leg => leg.action === 'sell' && leg.option.type === 'put');

    if (shortCalls.length > 0 || shortPuts.length > 0) {
      return Infinity; // Unlimited loss potential
    } else if (longCalls.length > 0 || longPuts.length > 0) {
      return this.calculateTotalCost(); // Max loss is the debit paid
    }
    return 0;
  }

  calculateBreakEven(): number[] {
    // This is a simplified calculation and may not be accurate for all strategies
    const totalCost = this.calculateTotalCost();
    const breakEvenPoints: number[] = [];

    const calls = this.selectedLegs.filter(leg => leg.option.type === 'call');
    const puts = this.selectedLegs.filter(leg => leg.option.type === 'put');

    if (calls.length > 0) {
      const lowestCallStrike = Math.min(...calls.map(leg => leg.option.strike));
      breakEvenPoints.push(lowestCallStrike + totalCost / 100);
    }

    if (puts.length > 0) {
      const highestPutStrike = Math.max(...puts.map(leg => leg.option.strike));
      breakEvenPoints.push(highestPutStrike - totalCost / 100);
    }

    return breakEvenPoints;
  }
}
