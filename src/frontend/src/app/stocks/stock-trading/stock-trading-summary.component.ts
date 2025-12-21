import {Component, Input} from '@angular/core';
import {
    AccountStatus,
    BrokerageAccount,
    BrokerageAccountSnapshot,
    StockPosition,
    StockQuote
} from "../../services/stocks.service";
import {isLongTermStrategy, parseDate} from "../../services/utils";
import { CurrencyPipe, PercentPipe } from '@angular/common';
import { CanvasJSAngularChartsModule } from "@canvasjs/angular-charts";

interface PositionGroup {
    strategy: string;
    positions: StockPosition[];
    cost: number;
    risk: number;
    profit: number;
    length: number;
}

@Component({
    selector: 'app-stock-trading-summary',
    templateUrl: './stock-trading-summary.component.html',
    styleUrls: ['./stock-trading-summary.component.css'],
    imports: [CurrencyPipe, PercentPipe, CanvasJSAngularChartsModule]
})
export class StockTradingSummaryComponent {

    positionGroups: PositionGroup[] = [];

    sortProperty: string = "";
    sortDirection: number = 1;
    longPositions: StockPosition[] = [];
    shortPositions: StockPosition[] = [];
    totalLongCost: number = 0;
    totalShortCost: number = 0;
    totalProfit: number = 0;
    @Input()
    quotes: Record<string, StockQuote> = {};
    @Input()
    brokerageAccount: BrokerageAccount | null = null;
    @Input()
    userState: AccountStatus | null = null;
    chartOptionsArray: any[] = [];

    @Input()
    set positions(value: StockPosition[]) {

        this.longPositions = value.filter(p => p.isShort === false)
        this.shortPositions = value.filter(p => p.isShort === true)

        this.totalLongCost = this.reduce(this.longPositions, (p: StockPosition) => p.averageCostPerShare * p.numberOfShares)
        this.totalShortCost = this.reduce(this.shortPositions, (p: StockPosition) => p.averageCostPerShare * p.numberOfShares)
        this.totalProfit = this.reduce(value, (p: StockPosition) => this.getUnrealizedProfit(p))
        this.positionGroups = this.breakdownByStrategy(value)
    }
    
    @Input()
    set balances(value:BrokerageAccountSnapshot[]) {
        this.updateChartOptions(value);
    }

    getStrategy(position: StockPosition): string {
        let strategy = position.labels.find(l => l.key == 'strategy')
        return strategy ? strategy.value : "none"
    }

    getUnrealizedProfit(position: StockPosition): number {
        let quote = this.quotes[position.ticker]
        return quote ? (quote.price - position.averageCostPerShare) * position.numberOfShares + position.profitWithoutDividendsAndFees : 0
    }

    getSortFunc(property: string): (a: PositionGroup, b: PositionGroup) => number {
        switch (property) {
            case 'cost':
                return (a, b) => b.cost - a.cost
            case 'risk':
                return (a, b) => b.risk - a.risk
            case 'profit':
                return (a, b) => b.profit - a.profit
            case 'gain':
                return (a, b) => b.profit / b.cost - a.profit / a.cost
            default:
                return (a, b) => b.strategy.localeCompare(a.strategy)
        }
    }

    sort(property: string) {
        this.sortDirection = this.sortProperty == property ? 1 : -1

        this.sortProperty = property

        const sortFunc = this.getSortFunc(property)

        const adjustedFunc = (a: PositionGroup, b: PositionGroup) => this.sortDirection * sortFunc(a, b)

        this.positionGroups.sort(adjustedFunc)
    }

    breakdownByStrategy(positions: StockPosition[]): PositionGroup[] {
        console.log("breaking down positions")

        if (!positions) return []

        let strategyGroups: { [key: string]: StockPosition[] } = {}

        // custom groups
        strategyGroups["allbutlongterm"] = positions.filter(p => !isLongTermStrategy(this.getStrategy(p)))
        strategyGroups["long"] = positions.filter((p: StockPosition) => p.isShort === false && !isLongTermStrategy(this.getStrategy(p)))
        strategyGroups["short"] = positions.filter((p: StockPosition) => p.isShort === true && !isLongTermStrategy(this.getStrategy(p)))

        positions.reduce((acc, cur) => {
            let strategyKey = this.getStrategy(cur)

            if (!acc[strategyKey]) {
                acc[strategyKey] = []
            }

            acc[strategyKey].push(cur)

            return acc
        }, strategyGroups)

        let groupsArray = []

        for (const key in strategyGroups) {
            let groupPositions = strategyGroups[key]

            let group = {
                strategy: key,
                positions,
                cost: this.reduce(groupPositions, (p: StockPosition) => p.isShort === false ? p.averageCostPerShare * p.numberOfShares : 0),
                risk: this.reduce(groupPositions, (p: StockPosition) => p.costAtRiskBasedOnStopPrice),
                profit: this.reduce(groupPositions, (p: StockPosition) => this.getUnrealizedProfit(p)),
                length: groupPositions.length
            }

            groupsArray.push(group)
        }

        return groupsArray
    }

    private reduce(positions: StockPosition[], func: (p: StockPosition) => number): number {
        return positions.reduce((acc, cur) => acc + func(cur), 0)
    }

    private updateChartOptions(value: BrokerageAccountSnapshot[]) {
        const cashData = value.map(snapshot => ({ x: parseDate(snapshot.date), y: snapshot.cash }));
        const equityData = value.map(snapshot => ({ x: parseDate(snapshot.date), y: snapshot.equity }));
        const longValueData = value.map(snapshot => ({ x: parseDate(snapshot.date), y: snapshot.longValue }));
        const shortValueData = value.map(snapshot => ({ x: parseDate(snapshot.date), y: snapshot.shortValue }));

        const axisY = {
            // includeZero: true,
                gridThickness: 1,
                gridColor: 'rgba(0, 0, 0, 0.1)'
        }
        
        this.chartOptionsArray = [
            {
                title: { text: 'Cash Balance' },
                data: [{ type: 'line', dataPoints: cashData, markerSize: 1 }],
                axisY: axisY
            },
            {
                title: { text: 'Equity Balance' },
                data: [{ type: 'line', dataPoints: equityData, markerSize: 1 }],
                axisY: axisY
            },
            {
                title: { text: 'Long Value' },
                data: [{ type: 'line', dataPoints: longValueData, markerSize: 1 }],
                axisY: axisY
            },
            {
                title: { text: 'Short Value' },
                data: [{ type: 'line', dataPoints: shortValueData, markerSize: 1 }],
                axisY: axisY
            }
        ];
    }
}
