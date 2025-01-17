import {Component, Input} from '@angular/core';
import {StockPosition} from 'src/app/services/stocks.service';
import {stockClosedPositionExportLink} from "../../services/links.service";
import {GetStockStrategies} from "../../services/utils";


@Component({
    selector: 'app-stock-trading-closed-positions',
    templateUrl: './stock-trading-closed-positions.component.html',
    styleUrls: ['./stock-trading-closed-positions.component.css'],
    standalone: false
})
export class StockTradingClosedPositionsComponent {
    tickers: string[];
    groupedByMonth: {
        month: string,
        positions: StockPosition[],
        wins: StockPosition[],
        losses: StockPosition[]
    }[]
    strategies: string[] = []
    sortColumn: string = 'closed'
    sortDirection: number = -1
    tickerFilter: string = 'all'
    gradeFilter: string = 'all'
    outcomeFilter: string = 'all'
    plFilter: string = 'all'
    strategyFilter: string = 'all'
    showNotes: number = -1
    LAYOUT_OPTION_TABLE: string = 'table'
    LAYOUT_OPTION_SPLIT_OUTCOME: string = 'splitoutcome'
    layout: string = this.LAYOUT_OPTION_TABLE

    private _positions: StockPosition[];

    get positions(): StockPosition[] {
        return this._positions
    }

    @Input()
    set positions(value: StockPosition[]) {
        this._positions = value
        this.tickers = value
            .map(p => p.ticker)
            .filter((v, i, a) => a.indexOf(v) === i)
            .sort()
        let groupedByMonth = value.reduce((a, b) => {
            var key = b.closed.substring(0, 7)
            if (!a.has(key)) {
                a.set(key, [])
            }
            let arr = a.get(key)
            arr.push(b)
            return a
        }, new Map<string, StockPosition[]>())

        let groupedByMonthArray = []
        groupedByMonth.forEach((value, key) => {
            groupedByMonthArray.push(
                {
                    month: key,
                    positions: value,
                    wins: value.filter(p => p.profit >= 0),
                    losses: value.filter(p => p.profit < 0)
                }
            )
        })
        this.groupedByMonth = groupedByMonthArray

        value.map(
            (s) => {
                let strategyLabel = s.labels.findIndex(l => l.key === 'strategy')
                if (strategyLabel === -1) {
                    return null
                }
                return s.labels[strategyLabel].value
            }
        )
            .filter((v, i, a) => v !== null && a.indexOf(v) === i) // unique
            .forEach(s => this.strategies.push(s))

        GetStockStrategies().forEach(
            s => {
                if (this.strategies.indexOf(s.key) === -1) {
                    this.strategies.push(s.key)
                }
            })

        this.strategies.sort()
    }

    getClosedPositionExportLink() {
        return stockClosedPositionExportLink()
    }

    matchesFilter(position: StockPosition) {

        if (this.tickerFilter != 'all' && !position.ticker.toLowerCase().includes(this.tickerFilter.toLowerCase())) {
            return false
        }

        if (this.gradeFilter != 'all' && position.grade != this.gradeFilter) {
            return false
        }

        if (this.strategyFilter != 'all' && !this.matchesStrategyCheck(position, this.strategyFilter)) {
            return false
        }

        if (this.plFilter != 'all') {
            let value = parseFloat(this.plFilter)
            if (value > 0) {
                return position.profit >= value
            } else {
                return position.profit < value
            }
        }

        let winMasmatch = position.profit >= 0 && this.outcomeFilter === 'loss'
        let lossMismatch = position.profit < 0 && this.outcomeFilter === 'win'

        return !(this.outcomeFilter != 'all' && (winMasmatch || lossMismatch));
    }

    toggleShowNotes(index: number) {
        if (this.showNotes == index) {
            this.showNotes = -1
        } else {
            this.showNotes = index
        }
    }

    filterByTickerChanged(value: string) {
        this.tickerFilter = value
    }

    filterByGradeChanged(value: string) {
        this.gradeFilter = value
    }

    filterByOutcomeChanged(value: string) {
        this.outcomeFilter = value
    }

    // ugly name, but essentially this returns a property for grouping
    // trades, and it's either closed date or open date of a position right now, otherwise, no grouping is

    filterByPLChanged(value: string) {
        this.plFilter = value
    }

    filterByStrategyChanged(value: string) {
        this.strategyFilter = value
    }

    matchesStrategyCheck(p: StockPosition, strategy: string) {
        return p.labels.findIndex(l => l.key === "strategy" && l.value === strategy) !== -1
    }

    // returned as we want no separator in that case (ie, if sorted by gain, grouping those by month makes no sense)
    getPropertyForSeperatorGrouping(position: StockPosition) {
        var groupingInput = ""
        if (this.sortColumn === 'opened') {
            groupingInput = position.opened
        }
        if (this.sortColumn === 'closed') {
            groupingInput = position.closed
        }

        if (groupingInput === "") {
            return ""
        }

        return groupingInput.substring(0, 7)
    }

    getPositionsForMonth(month: string) {
        return this.positions.filter(p => this.getPropertyForSeperatorGrouping(p) === month)
    }

    getRRSumForMonth(position: StockPosition) {
        var positions = this.getPositionsForMonth(this.getPropertyForSeperatorGrouping(position))
        return positions.reduce((a, b) => a + b.rr, 0)
    }

    getProfitSumForMonth(position: StockPosition) {
        var positions = this.getPositionsForMonth(this.getPropertyForSeperatorGrouping(position))
        return this.getProfitSum(positions)
    }

    getProfitSum(positions: StockPosition[]) {
        return positions.reduce((a, b) => a + b.profit, 0)
    }

    getTradeCountByGradeForMonth(position: StockPosition, grade: string) {
        var positions = this.getPositionsForMonth(this.getPropertyForSeperatorGrouping(position))
        return positions.filter(p => p.grade === grade).length
    }

    getTradeCountForMonth(position: StockPosition) {
        var positions = this.getPositionsForMonth(this.getPropertyForSeperatorGrouping(position))
        return positions.length
    }

    sort(column: string) {
        var func = this.getSortFunc(column);

        if (this.sortColumn != column) {
            this.sortDirection = -1
        } else {
            this.sortDirection *= -1
        }
        this.sortColumn = column

        var finalFunc = (a, b) => {
            var result = func(a, b)
            return result * this.sortDirection
        }

        this.positions.sort(finalFunc)
    }

    toggleLayout() {
        if (this.layout === this.LAYOUT_OPTION_TABLE) {
            this.layout = this.LAYOUT_OPTION_SPLIT_OUTCOME
        } else {
            this.layout = this.LAYOUT_OPTION_TABLE
        }
    }

    private getSortFunc(column: string) {
        switch (column) {
            case "daysHeld":
                return (a: StockPosition, b: StockPosition) => a.daysHeld - b.daysHeld
            case "opened":
                return (a: StockPosition, b: StockPosition) => a.opened.localeCompare(b.opened)
            case "closed":
                return (a: StockPosition, b: StockPosition) => a.closed.localeCompare(b.closed)
            case "rr":
                return (a: StockPosition, b: StockPosition) => a.rr - b.rr
            case "profit":
                return (a: StockPosition, b: StockPosition) => a.profit - b.profit
            case "gainPct":
                return (a: StockPosition, b: StockPosition) => a.gainPct - b.gainPct
            case "grade":
                return (a: StockPosition, b: StockPosition) => (a.grade ?? "").localeCompare((b.grade ?? ""))
            case "ticker":
                return (a: StockPosition, b: StockPosition) => a.ticker.localeCompare(b.ticker)
        }

        console.log("unrecognized sort column " + column)
        return null;
    }
}
