import {Component, Input} from '@angular/core';
import {GetErrors} from 'src/app/services/utils';
import {
    OutcomesReport,
    StockPosition, SimulationNotices,
    StockGaps,
    StocksService,
    TickerCorrelation, PortfolioHoldings
} from '../../services/stocks.service';
import {catchError, finalize, map} from "rxjs/operators";
import {concat, EMPTY} from "rxjs";
import {StockPositionsService} from "../../services/stockpositions.service";

@Component({
    selector: 'app-stock-trading-outcomes-reports',
    templateUrl: './stock-trading-outcomes-reports.component.html',
    styleUrls: ['./stock-trading-outcomes-reports.component.css'],
    standalone: false
})

export class StockPositionReportsComponent {

    allBarsReport: OutcomesReport;
    singleBarReportDaily: OutcomesReport;
    singleBarReportWeekly: OutcomesReport;
    positionsReport: OutcomesReport;
    gaps: StockGaps[] = [];
    correlationReport: TickerCorrelation[];
    tickerFilter: string;
    daysForCorrelations = 60
    
    loading = {
        open: false,
        daily: false,
        weekly: false,
        allBars: false,
        positions: false,
        correlation: false
    }

    errors = {
        open: null,
        daily: null,
        weekly: null,
        allBars: null,
        positions: null,
        correlation: null
    }
    tickers: string[] = []

    constructor(private service: StocksService, private stockPositions: StockPositionsService) {
    }

    @Input()
    set dailyAnalysis(value: PortfolioHoldings) {
        if (value) {
            this.loadPositionData(value)
        }
    }

    @Input()
    set allTimeAnalysis(value: StockPosition[]) {
        if (value) {
            this.loading.allBars = true
            this.loadAllTimeData(value)
        }
    }

    openSimulationNotices: SimulationNotices = null
    openSimulationKeys: string[] = null
    expandedGroups: Set<string> = new Set();

    toggleNotices(group: string) {
        if (this.expandedGroups.has(group)) {
            this.expandedGroups.delete(group);
        } else {
            this.expandedGroups.add(group);
        }
    }

    isExpanded(group: string): boolean {
        return this.expandedGroups.has(group);
    }

    loadPositionData(portfolio: PortfolioHoldings) {
        this.loading.daily = true
        this.loading.weekly = true
        this.loading.positions = true
        this.loading.correlation = true
        this.loading.open = true

        const openSimulationNotices$ = this.stockPositions.openPositionSimulationNotices().pipe(
            map(notices => {
                this.openSimulationNotices = notices
                this.openSimulationKeys = Object.keys(notices)
            }),
            catchError(
                error => {
                    this.handleApiError("Unable to load open simulation notices", error, (e) => this.errors.open = e)
                    return EMPTY
                }
            ),
            finalize(() => this.loading.open = false)
        )
        
        const positionReport$ = this.service.reportPositions().pipe(
            map(report => {
                this.positionsReport = report
            }),
            catchError(
                error => {
                    this.handleApiError("Unable to load position reports", error, (e) => this.errors.positions = e)
                    return EMPTY
                }
            ),
            finalize(() => this.loading.positions = false)
        )

        this.tickers = Array.from(
            new Set(
                portfolio.stocks.map(p => p.ticker).concat(portfolio.options.map(p => p.underlyingTicker))
            )
        )
        
        const dailyReport$ = this.service.reportOutcomesSingleBarDaily(this.tickers).pipe(
            map(report => this.singleBarReportDaily = report),
            catchError(
                error => {
                    this.handleApiError("Unable to load daily data", error, (e) => this.errors.daily = e)
                    return EMPTY
                }
            ),
            finalize(() => this.loading.daily = false)
        )

        const weeklyReport$ = this.service.reportOutcomesSingleBarWeekly(this.tickers).pipe(
            map(report => {
                this.singleBarReportWeekly = report
            }),
            catchError(
                error => {
                    this.handleApiError("Unable to load weekly data", error, (e) => this.errors.weekly = e)
                    return EMPTY
                }
            ),
            finalize(() => this.loading.weekly = false)
        )
        
        const correlationReport$ = this.service.reportPortfolioCorrelations(this.daysForCorrelations).pipe(
            map(report => {
                this.correlationReport = report
            }),
            catchError(
                error => {
                    this.handleApiError("Unable to load correlation data", error, (e) => this.errors.correlation = e)
                    return EMPTY
                }
            ),
            finalize(() => this.loading.correlation = false)
        )

        concat(openSimulationNotices$, positionReport$, dailyReport$, weeklyReport$, correlationReport$).subscribe()
    }

    loadAllTimeData(positions: StockPosition[]) {
        this.tickers = positions.map(p => p.ticker)
        this.service.reportOutcomesAllBars(this.tickers).subscribe(report => {
            this.loading.allBars = false
            this.allBarsReport = report
            this.gaps = report.gaps
        }, error => {
            this.loading.allBars = false
            this.handleApiError("Unable to load all bars", error, (e) => this.errors.allBars = e)
        })
    }

    onTickerChange(ticker: string) {
        this.tickerFilter = ticker
    }

    private handleApiError(errorMessage: string, error: any, assignFunc: (error: any) => void) {
        const extractedErrors = GetErrors(error);
        extractedErrors.forEach(e => console.log(e))
        const fullError = errorMessage + ": " + extractedErrors.join(", ")
        assignFunc([fullError])
    }
}
