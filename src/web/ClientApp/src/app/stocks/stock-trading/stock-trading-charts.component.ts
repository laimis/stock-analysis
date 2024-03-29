import {Component, Input} from '@angular/core';
import {PositionInstance, StockQuote} from 'src/app/services/stocks.service';
import {CanvasJSAngularChartsModule} from "@canvasjs/angular-charts";

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
            text: "Profit and Days Held",
        },
        axisX: {
            title: "Days Held",
            gridThickness: 0.1,
        },
        axisY: {
            title: "Profit",
            gridThickness: 0.1,
        },
        data: [
            {
                type: "scatter",
                // showInLegend: true,
                name: "Position",
                dataPoints: mapped
            }
        ]
    }
}

@Component({
    selector: 'app-stock-trading-charts',
    templateUrl: './stock-trading-charts.component.html',
    styleUrls: ['./stock-trading-charts.component.css'],
    imports: [
        CanvasJSAngularChartsModule
    ],
    standalone: true
})
export class StockTradingChartsComponent {

    chartOptions : any[] = []
    private _positions: PositionInstance[];
    private _quotes: Map<string, StockQuote>;
    
    @Input()
    set quotes(value: Map<string, StockQuote>) {
        this._quotes = value
        this.generateChartOptions()
    }
    
    @Input()
    set positions(positions: PositionInstance[]) {
        this._positions = positions
        this.generateChartOptions()
    }

    private generateChartOptions() {
        if (this._positions && this._quotes) {
            this.chartOptions = [
                createProfitScatter(this._positions, this._quotes)
            ]
        }
    }
}
