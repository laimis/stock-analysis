import {Component, ElementRef, OnInit, ViewChild} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {
    DailyPositionReport, DataPointContainer,
    OptionChain,
    OptionDefinition,
    PositionInstance,
    StockQuote,
    StocksService,
    TickerCorrelation, TradingStrategyResults
} from '../services/stocks.service';
import {GetErrors} from "../services/utils";
import {concat} from "rxjs";
import {tap} from "rxjs/operators";
import {StockPositionsService} from "../services/stockpositions.service";
import { Input, OnChanges, SimpleChanges } from '@angular/core';
import { CanvasJS } from '@canvasjs/angular-charts';


// option-trade-builder.component.ts
  interface OptionLeg {
    option: OptionDefinition;
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
        private stockService: StocksService,
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
  options: OptionDefinition[] = []; // Will be populated with stub data
  selectedLegs: OptionLeg[] = [];
  
  filteredOptions: OptionDefinition[] = [];
  sortColumn: string = 'strike';
  sortDirection: 'asc' | 'desc' = 'asc';
  filterExpiration: string = '';
  filterType: 'all' | 'call' | 'put' = 'all';
  optionChain: OptionChain;

  
  loadOptions(): void {
    this.stockService.getOptionChain(this.ticker).subscribe((data) => {
            this.optionChain = data
            this.options = this.optionChain.options
            this.applyFiltersAndSort();
        }
    )
  }

  applyFiltersAndSort(): void {
    this.filteredOptions = this.options.filter(option => {
      return (this.filterExpiration === '' || option.expirationDate === this.filterExpiration) &&
             (this.filterType === 'all' || option.side === this.filterType);
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
      if (!expirations.includes(option.expirationDate)) {
        expirations.push(option.expirationDate);
      }
      return expirations;
    }, []);
  }

  addLeg(option: OptionDefinition): void {
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
    const longCalls = this.selectedLegs.filter(leg => leg.action === 'buy' && leg.option.side === 'call');
    const shortCalls = this.selectedLegs.filter(leg => leg.action === 'sell' && leg.option.side === 'call');
    const longPuts = this.selectedLegs.filter(leg => leg.action === 'buy' && leg.option.side === 'put');
    const shortPuts = this.selectedLegs.filter(leg => leg.action === 'sell' && leg.option.side === 'put');

    if (longCalls.length > 0 || longPuts.length > 0) {
      return Infinity; // Unlimited profit potential
    } else if (shortCalls.length > 0 || shortPuts.length > 0) {
      return this.calculateTotalCost(); // Max profit is the credit received
    }
    return 0;
  }

  calculateMaxLoss(): number {
    // This is a simplified calculation and may not be accurate for all strategies
    const longCalls = this.selectedLegs.filter(leg => leg.action === 'buy' && leg.option.side === 'call');
    const shortCalls = this.selectedLegs.filter(leg => leg.action === 'sell' && leg.option.side === 'call');
    const longPuts = this.selectedLegs.filter(leg => leg.action === 'buy' && leg.option.side === 'put');
    const shortPuts = this.selectedLegs.filter(leg => leg.action === 'sell' && leg.option.side === 'put');

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

    const calls = this.selectedLegs.filter(leg => leg.option.side === 'call');
    const puts = this.selectedLegs.filter(leg => leg.option.side === 'put');

    if (calls.length > 0) {
      const lowestCallStrike = Math.min(...calls.map(leg => leg.option.strikePrice));
      breakEvenPoints.push(lowestCallStrike + totalCost / 100);
    }

    if (puts.length > 0) {
      const highestPutStrike = Math.max(...puts.map(leg => leg.option.strikePrice));
      breakEvenPoints.push(highestPutStrike - totalCost / 100);
    }

    return breakEvenPoints;
  }
}


@Component({
    selector: 'app-payoff-diagram',
    standalone: true,
    template: '<div id="payoffChartContainer" style="height: 370px; width: 100%;"></div>',
  })
  export class PayoffDiagramComponent implements OnChanges, OnInit {
    @Input() selectedLegs: OptionLeg[];
    private chart: any;
  
    ngOnInit() {
      this.createChart();
    }
  
    ngOnChanges(changes: SimpleChanges): void {
      if (changes.selectedLegs) {
        this.updateChart();
      }
    }
  
    private createChart(): void {
      this.chart = new CanvasJS.Chart("payoffChartContainer", {
        animationEnabled: true,
        exportEnabled: true,
        title: {
          text: "Option Strategy Payoff Diagram"
        },
        axisX: {
          title: "Underlying Price",
          gridThickness: 1
        },
        axisY: {
          title: "Profit/Loss",
          gridThickness: 1,
          valueFormatString: "$#,##0"
        },
        toolTip: {
          shared: true
        },
        legend: {
          cursor: "pointer",
          verticalAlign: "top",
          horizontalAlign: "center",
          dockInsidePlotArea: true,
        },
        data: [{
          type: "line",
          name: "Payoff",
          showInLegend: true,
          dataPoints: this.calculatePayoffData()
        }]
      });
  
      this.chart.render();
    }
  
    private updateChart(): void {
      if (this.chart) {
        this.chart.options.data[0].dataPoints = this.calculatePayoffData();
        this.chart.render();
      }
    }
  
    private calculatePayoffData(): { x: number, y: number }[] {
      if (!this.selectedLegs || this.selectedLegs.length === 0) {
        return [];
      }
  
      const data: { x: number, y: number }[] = [];
      const minStrike = Math.min(...this.selectedLegs.map(leg => leg.option.strikePrice));
      const maxStrike = Math.max(...this.selectedLegs.map(leg => leg.option.strikePrice));
      const range = maxStrike - minStrike;
  
      for (let price = minStrike - range / 2; price <= maxStrike + range / 2; price += range / 40) {
        let payoff = 0;
        for (const leg of this.selectedLegs) {
          const optionValue = this.calculateOptionValue(leg.option, price);
          // todo: do not use bid below, use mid or last
          payoff += (leg.action === 'buy' ? 1 : -1) * (optionValue - leg.option.bid) * leg.quantity * 100;
        }
        data.push({ x: price, y: payoff });
      }
  
      return data;
    }
  
    private calculateOptionValue(option: OptionDefinition, underlyingPrice: number): number {
      // This is a simplified calculation and doesn't account for time value or volatility
      if (option.side === 'call') {
        return Math.max(0, underlyingPrice - option.strikePrice);
      } else {
        return Math.max(0, option.strikePrice - underlyingPrice);
      }
    }
  }