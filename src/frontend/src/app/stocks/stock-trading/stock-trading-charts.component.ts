import {Component, Input} from '@angular/core';
import {StockPosition, StockQuote} from "../../services/stocks.service";
import {CanvasJSAngularChartsModule} from "@canvasjs/angular-charts";
import {blue} from "../../services/charts.service";
import {FormsModule} from '@angular/forms';

const scatterPointMarkerSize = 12;

interface HistogramResult {
    histogramData: Array<{x: number, y: number}>;
    min: number;
    max: number;
    interval: number;
}

function generateHistogramData(data: number[], numIntervals: number): HistogramResult {
    const histogramData = [];
    const minPercentage = Math.min(...data);
    const maxPercentage = Math.max(...data);
    const range = maxPercentage - minPercentage;
    const intervalSize = range / numIntervals;

    for (let i = 0; i < numIntervals; i++) {
        const lowerBound = minPercentage + i * intervalSize;
        let upperBound = i === numIntervals - 1 ? maxPercentage + 1 : lowerBound + intervalSize;
        const count = data.filter(percentage => percentage >= lowerBound && percentage < upperBound).length;
        histogramData.push({ x: lowerBound, y: count });
    }
    return { histogramData, min: minPercentage, max: maxPercentage, interval: intervalSize };
}

function unrealizedProfit(position: StockPosition, quote: StockQuote | undefined): number {
    if (!quote) {
        return 0;
    }
    return position.profit + (quote.price - position.averageCostPerShare) * position.numberOfShares;
}

function createUnrealizedProfitChart(entries: StockPosition[], quotes: Record<string, StockQuote>) {
    const mapped = entries.map(p => {
        const quote = quotes[p.ticker];
        const profit = unrealizedProfit(p, quote);
        return {
            x: p.daysHeld,
            y: profit,
            label: p.ticker,
            toolTipContent: `<strong>${p.ticker}</strong><br/>Days Held: ${p.daysHeld}<br/>Unrealized Gain: $${profit.toFixed(2)}`,
            indexLabel: '{label}',
            indexLabelFontSize: 12,
        };
    });

    return {
        animationEnabled: false,
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Unrealized Profit vs Days Held",
            fontSize: 20
        },
        axisX: {
            title: "Days Held",
            gridThickness: 0.1,
            titleFontSize: 12,
            labelFontSize: 10
        },
        axisY: {
            title: "Profit",
            gridThickness: 0.1,
            titleFontSize: 12,
            labelFontSize: 10
        },
        dataPointWidth: 10,
        data: [
            {
                type: "scatter",
                markerSize: scatterPointMarkerSize,
                color: blue,
                name: "Position",
                dataPoints: mapped
            }
        ]
    };
}

function unrealizedRR(p: StockPosition, quote: StockQuote | undefined): number {
    if (!quote) {
        return 0;
    }
    return (p.profit + p.numberOfShares * (quote.price - p.averageCostPerShare)) / (p.riskedAmount === 0 ? 40 : p.riskedAmount);
}

function createDaysHeldVsGainPercentChart(positions: StockPosition[], quotes: Record<string, StockQuote>) {
    const chartData = positions.map(position => {
        const quote = quotes[position.ticker];
        const unrealizedGainPercent = unrealizedGainPercentage(position, quote);
        return {
            x: position.daysHeld,
            y: unrealizedGainPercent,
            label: position.ticker,
            indexLabel: '{label}',
            indexLabelFontSize: 12,
            toolTipContent: `<strong>${position.ticker}</strong><br/>Days Held: ${position.daysHeld}<br/>Unrealized Gain: ${unrealizedGainPercent.toFixed(2)}%`
        };
    });

    return {
        animationEnabled: false,
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Days Held vs. Unrealized Gain %",
            fontSize: 20
        },
        dataPointWidth: 10,
        axisX: {
            title: "Days Held",
            gridThickness: 0.1,
            titleFontSize: 12,
            labelFontSize: 10
        },
        axisY: {
            title: "Unrealized Gain %",
            gridThickness: 0.1,
            titleFontSize: 12,
            labelFontSize: 10,
            labelFormatter: function(e: any) {
                return e.value.toFixed(2) + "%";
            }
        },
        data: [
            {
                type: "scatter",
                markerSize: scatterPointMarkerSize,
                color: blue,
                dataPoints: chartData
            }
        ]
    };
}

function createProfitDistributionChart(positions: StockPosition[], quotes: Record<string, StockQuote>) {
    const profitData = positions.map(position => unrealizedProfit(position, quotes[position.ticker]));

    const { histogramData, min, max, interval } = generateHistogramData(profitData, 10);

    return {
        animationEnabled: false,
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Unrealized Profit Distribution",
            fontSize: 20
        },
        axisX: {
            title: "Profit",
            minimum: min,
            maximum: max,
            interval: interval,
            titleFontSize: 12,
            labelFontSize: 10,
            labelFormatter: function(e: any) {
                return "$" + e.value.toFixed(2);
            }
        },
        axisY: {
            title: "Number of Positions",
            gridThickness: 0.1,
            titleFontSize: 12,
            labelFontSize: 10
        },
        data: [
            {
                type: "bar",
                color: blue,
                dataPoints: histogramData
            }
        ]
    };
}

function createPositionSizePieChart(positions: StockPosition[]) {
    const totalCost = positions.reduce((sum, position) => sum + Math.abs(position.cost), 0);
    
    const pieChartData = positions.map(position => {
        const absoluteCost = Math.abs(position.cost);
        const percentage = Math.round((absoluteCost / totalCost) * 100);
        return {
            y: percentage,
            label: position.ticker
        };
    }).sort((a, b) => b.y - a.y);

    return {
        animationEnabled: false,
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Position Sizes",
            fontSize: 20
        },
        data: [
            {
                type: "pie",
                showInLegend: true,
                legendText: "{label}",
                indexLabel: "{label}: {y}%",
                indexLabelFontColor: "white",
                indexLabelPlacement: "inside",
                yValueFormatString: "#0.##",
                dataPoints: pieChartData
            }
        ]
    };
}

function createDaysHeldDistributionChart(positions: StockPosition[]) {
    const data = positions.map(position => position.daysHeld);
    const { histogramData, min, max, interval } = generateHistogramData(data, 10);

    return {
        animationEnabled: false,
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Days Held Distribution",
            fontSize: 20
        },
        axisX: {
            title: "Days Held",
            minimum: min,
            maximum: max,
            interval: interval,
            titleFontSize: 12,
            labelFontSize: 10
        },
        axisY: {
            title: "Number of Positions",
            gridThickness: 0.1,
            titleFontSize: 12,
            labelFontSize: 10
        },
        data: [
            {
                type: "bar",
                color: blue,
                dataPoints: histogramData
            }
        ]
    };
}

function unrealizedGainPercentage(position: StockPosition, quote: StockQuote | undefined): number {
    if (!quote) {
        return 0;
    }

    // need abs for short positions
    return position.isShort ?
        (position.averageCostPerShare - quote.price) / position.averageCostPerShare * 100
        :
        (quote.price - position.averageCostPerShare) / position.averageCostPerShare * 100;
}

function createUnrealizedGainPercentageDistributionChart(positions: StockPosition[], quotes: Record<string, StockQuote>) {
    // Calculate unrealized gain percentage for each position
    const gainPercentageData = positions.map(position => unrealizedGainPercentage(position, quotes[position.ticker]));

    const { histogramData, min, max, interval } = generateHistogramData(gainPercentageData, 10);
    
    return {
        animationEnabled: false,
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Unrealized Gain Percentage Distribution",
            fontSize: 20
        },
        axisX: {
            title: "Gain Percentage",
            minimum: min,
            maximum: max,
            interval: interval,
            titleFontSize: 12,
            labelFontSize: 10,
            labelFormatter: function(e: any) {
                return e.value.toFixed(2) + "%";
            }
        },
        axisY: {
            title: "Number of Positions",
            gridThickness: 0.1,
            titleFontSize: 12,
            labelFontSize: 10
        },
        data: [
            {
                type: "bar",
                color: blue,
                dataPoints: histogramData
            }
        ]
    };
}

function createUnrealizedRRDistributionChart(positions: StockPosition[], quotes: Record<string, StockQuote>) {
    // Calculate unrealized RR for each position
    const rrData = positions.map(position => unrealizedRR(position, quotes[position.ticker]));

    const { histogramData, min, max, interval } = generateHistogramData(rrData, 10);

    return {
        animationEnabled: false,
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Unrealized RR Distribution",
            fontSize: 20
        },
        axisX: {
            title: "Risk-Reward Ratio",
            minimum: min,
            maximum: max,
            interval: interval,
            titleFontSize: 12,
            labelFontSize: 10,
            labelFormatter: function(e: any) {
                return e.value.toFixed(2);
            }
        },
        axisY: {
            title: "Number of Positions",
            gridThickness: 0.1,
            titleFontSize: 12,
            labelFontSize: 10
        },
        data: [
            {
                type: "bar",
                color: blue,
                dataPoints: histogramData
            }
        ]
    };
}

function stopPriceDistance(position: StockPosition, quote: StockQuote | undefined): number {
    if (!quote) {
        return 0;
    }
    const referencePrice = position.stopPrice ? position.stopPrice : position.averageCostPerShare;
    // need abs for short positions
    return Math.abs( ((quote.price - referencePrice) / quote.price) * 100 );
}

function createStopPriceDistanceChart(positions: StockPosition[], quotes: Record<string, StockQuote>) {
    // Calculate stop price distance and unrealized gain percentage for each position
    const scatterData = positions.map(position => {
        const quote = quotes[position.ticker];
        const stopDist = stopPriceDistance(position, quote);
        const gainPct = unrealizedGainPercentage(position, quote);
        return {
            x: stopDist,
            y: gainPct,
            label: position.ticker,
            indexLabel: '{label}',
            indexLabelFontSize: 12,
            toolTipContent: `<strong>${position.ticker}</strong><br/>Stop Distance: ${stopDist.toFixed(2)}%<br/>Unrealized Gain %: ${gainPct.toFixed(2)}%`
        };
    });

    return {
        animationEnabled: false,
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Unrealized Gain % vs Stop Price Distance %",
            fontSize: 20
        },
        dataPointWidth: 10,
        axisX: {
            title: "Stop Price Distance (%)",
            gridThickness: 0.1,
            titleFontSize: 12,
            labelFontSize: 10,
            labelFormatter: function(e: any) {
                return e.value.toFixed(2) + "%";
            }
        },
        axisY: {
            title: "Unrealized Gain %",
            titleFontSize: 12,
            labelFontSize: 10,
            labelFormatter: function(e: any) {
                return e.value.toFixed(2) + "%";
            }
        },
        data: [
            {
                type: "scatter",
                markerSize: scatterPointMarkerSize,
                color: blue,
                dataPoints: scatterData
            }
        ]
    };
}

function createPositionLabelsPieChart(positions: StockPosition[]) {
    // Sum the cost for each strategy label
    const strategyCosts = new Map<string, number>();
    positions.forEach(position => {
        position.labels
            .filter(label => label.key === "strategy")
            .forEach(label => {
                const currentCost = strategyCosts.get(label.value) || 0;
                strategyCosts.set(label.value, currentCost + Math.abs(position.cost));
            });
    });

    const pieChartData = Array.from(strategyCosts.entries()).map(([label, cost]) => ({
        label: label,
        y: cost
    }));

    return {
        animationEnabled: false,
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Positions by Strategy",
            fontSize: 20
        },
        data: [
            {
                type: "pie",
                showInLegend: true,
                legendText: "{label}",
                indexLabel: "{label}: ${y}",
                indexLabelPlacement: "outside",
                dataPoints: pieChartData
            }
        ]
    };
}

function createProfitByStrategyChart(positions: StockPosition[], quotes: Record<string, StockQuote>) {
    // Group positions by strategy and calculate realized and unrealized profit
    const strategyProfits = new Map<string, { realized: number, unrealized: number }>();
    
    positions.forEach(position => {
        position.labels
            .filter(label => label.key === "strategy")
            .forEach(label => {
                const current = strategyProfits.get(label.value) || { realized: 0, unrealized: 0 };
                const quote = quotes[position.ticker];
                const totalProfit = unrealizedProfit(position, quote);
                const realizedProfit = position.profit;
                const unrealizedProfitAmt = totalProfit - realizedProfit;
                
                strategyProfits.set(label.value, {
                    realized: current.realized + realizedProfit,
                    unrealized: current.unrealized + unrealizedProfitAmt
                });
            });
    });

    const chartData = Array.from(strategyProfits.entries())
        .map(([strategy, profits]) => ({
            label: strategy,
            realized: profits.realized,
            unrealized: profits.unrealized
        }))
        .sort((a, b) => (a.realized + a.unrealized) - (b.realized + b.unrealized));

    return {
        animationEnabled: false,
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Profit by Strategy",
            fontSize: 20
        },
        axisX: {
            title: "Strategy",
            labelAngle: -45,
            interval: 1,
            titleFontSize: 12,
            labelFontSize: 10
        },
        axisY: {
            title: "Profit",
            gridThickness: 0.1,
            titleFontSize: 12,
            labelFontSize: 10,
            labelFormatter: function(e: any) {
                return "$" + e.value.toFixed(2);
            }
        },
        data: [
            {
                type: "stackedBar",
                name: "Realized Profit",
                dataPoints: chartData.map(data => ({ label: data.label, y: data.realized }))
            },
            {
                type: "stackedBar",
                name: "Unrealized Profit",
                dataPoints: chartData.map(data => ({ label: data.label, y: data.unrealized }))
            }
        ]
    };
}

function createRealizedVsUnrealizedProfitChart(positions: StockPosition[], quotes: Record<string, StockQuote>) {
    const chartData = positions.map(position => {
        const realizedProfit = position.profit;
        const quote = quotes[position.ticker];
        const unrealizedProfitAmt = unrealizedProfit(position, quote) - realizedProfit;
        return {
            label: position.ticker,
            realized: realizedProfit,
            unrealized: unrealizedProfitAmt
        };
    }).sort((a, b) => (a.realized + a.unrealized) - (b.realized + b.unrealized));

    return {
        animationEnabled: false,
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Realized vs. Unrealized Profit",
            fontSize: 20
        },
        axisX: {
            title: "Position",
            labelAngle: -45,
            interval: 1,
            titleFontSize: 12,
            labelFontSize: 10
        },
        axisY: {
            title: "Profit",
            gridThickness: 0.1,
            titleFontSize: 12,
            labelFontSize: 10,
            labelFormatter: function(e: any) {
                return "$" + e.value.toFixed(2);
            }
        },
        data: [
            {
                type: "stackedBar",
                name: "Realized Profit",
                dataPoints: chartData.map(data => ({ label: data.label, y: data.realized }))
            },
            {
                type: "stackedBar",
                name: "Unrealized Profit",
                dataPoints: chartData.map(data => ({ label: data.label, y: data.unrealized }))
            }
        ]
    };
}

function createPositionsByCostChart(positions: StockPosition[]) {
    // Sort positions from smallest to largest based on cost
    const sortedPositions = [...positions].sort((a, b) => Math.abs(a.cost) - Math.abs(b.cost));

    const chartData = sortedPositions.map(position => ({
        label: position.ticker,
        y: Math.abs(position.cost)
    }));

    return {
        animationEnabled: false,
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Positions by Cost",
            fontSize: 20
        },
        axisX: {
            title: "Position",
            labelAngle: -45,
            interval: 1,
            titleFontSize: 12,
            labelFontSize: 10
        },
        axisY: {
            title: "Cost",
            gridThickness: 0.1,
            titleFontSize: 12,
            labelFontSize: 10,
            labelFormatter: function(e: any) {
                return "$" + e.value.toFixed(2);
            }
        },
        data: [
            {
                type: "bar",
                color: blue,
                dataPoints: chartData
            }
        ]
    };
}

function createPositionMarketValuePieChart(positions: StockPosition[], quotes: Record<string, StockQuote>) {
    // Calculate current market value for each position
    const positionsWithMarketValue = positions.map(position => {
        const quote = quotes[position.ticker];
        const currentMarketValue = quote ? (position.numberOfShares * quote.price) + position.profit : position.profit;
        return {
            ticker: position.ticker,
            marketValue: currentMarketValue
        };
    });

    const totalMarketValue = positionsWithMarketValue.reduce((sum, p) => sum + Math.abs(p.marketValue), 0);
    
    const pieChartData = positionsWithMarketValue.map(p => {
        const percentage = Math.round((Math.abs(p.marketValue) / totalMarketValue) * 100);
        return {
            y: percentage,
            label: p.ticker
        };
    }).sort((a, b) => b.y - a.y);

    return {
        animationEnabled: false,
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Position Market Values",
            fontSize: 20
        },
        data: [
            {
                type: "pie",
                showInLegend: true,
                legendText: "{label}",
                indexLabel: "{label}: {y}%",
                indexLabelFontColor: "white",
                indexLabelPlacement: "inside",
                yValueFormatString: "#0.##",
                dataPoints: pieChartData
            }
        ]
    };
}

function createPositionsByMarketValueChart(positions: StockPosition[], quotes: Record<string, StockQuote>) {
    // Calculate current market value for each position and sort from smallest to largest
    const positionsWithMarketValue = positions.map(position => {
        const quote = quotes[position.ticker];
        const currentMarketValue = quote ? (position.numberOfShares * quote.price) + position.profit : position.profit;
        return {
            ticker: position.ticker,
            marketValue: currentMarketValue
        };
    });
    const sortedPositions = [...positionsWithMarketValue].sort((a, b) => Math.abs(a.marketValue) - Math.abs(b.marketValue));

    const chartData = sortedPositions.map(p => ({
        label: p.ticker,
        y: Math.abs(p.marketValue)
    }));

    return {
        animationEnabled: false,
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Positions by Market Value (Smallest to Largest)",
            fontSize: 20
        },
        axisX: {
            title: "Position",
            labelAngle: -45,
            interval: 1,
            titleFontSize: 12,
            labelFontSize: 10
        },
        axisY: {
            title: "Market Value",
            gridThickness: 0.1,
            titleFontSize: 12,
            labelFontSize: 10,
            labelFormatter: function(e: any) {
                return "$" + e.value.toFixed(2);
            }
        },
        data: [
            {
                type: "bar",
                color: blue,
                dataPoints: chartData
            }
        ]
    };
}

function createPositionsByGainsChart(positions: StockPosition[], quotes: Record<string, StockQuote>) {
    // Calculate unrealized gains for each position and sort from smallest to largest
    const positionsWithGains = positions.map(position => {
        const quote = quotes[position.ticker];
        return {
            ...position,
            unrealizedGain: unrealizedProfit(position, quote) - position.profit
        };
    });
    const sortedPositions = [...positionsWithGains].sort((a, b) => a.unrealizedGain - b.unrealizedGain);

    const chartData = sortedPositions.map(position => ({
        label: position.ticker,
        y: position.unrealizedGain
    }));

    return {
        animationEnabled: false,
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Positions by Unrealized Gains (Smallest to Largest)",
            fontSize: 20
        },
        axisX: {
            title: "Position",
            labelAngle: -45,
            interval: 1,
            titleFontSize: 12,
            labelFontSize: 10
        },
        axisY: {
            title: "Unrealized Gain",
            gridThickness: 0.1,
            titleFontSize: 12,
            labelFontSize: 10,
            labelFormatter: function(e: any) {
                return "$" + e.value.toFixed(2);
            }
        },
        data: [
            {
                type: "bar",
                color: blue,
                dataPoints: chartData
            }
        ]
    };
}

@Component({
    selector: 'app-stock-trading-charts',
    templateUrl: './stock-trading-charts.component.html',
    styleUrls: ['./stock-trading-charts.component.css'],
    imports: [
        CanvasJSAngularChartsModule,
        FormsModule
    ]
})
export class StockTradingChartsComponent {

    chartOptions : any[] = []
    allTickers: string[] = [];
    private excludedTickers: Set<string> = new Set<string>(['USFR']);
    private _positions: StockPosition[] = [];
    private _quotes: Record<string, StockQuote> = {};
    
    @Input()
    set quotes(value: Record<string, StockQuote>) {
        this._quotes = value
        this.generateChartOptions()
    }
    
    @Input()
    set positions(positions: StockPosition[]) {
        this._positions = positions
        this.updateAllTickers();
        this.generateChartOptions()
    }

    private updateAllTickers() {
        // Extract all unique tickers from positions and sort them
        const tickerSet = new Set(this._positions.map(p => p.ticker.toUpperCase()));
        this.allTickers = Array.from(tickerSet).sort();
    }

    toggleTicker(ticker: string) {
        const upperTicker = ticker.toUpperCase();
        if (this.excludedTickers.has(upperTicker)) {
            this.excludedTickers.delete(upperTicker);
        } else {
            this.excludedTickers.add(upperTicker);
        }
        this.generateChartOptions();
    }

    isTickerExcluded(ticker: string): boolean {
        return this.excludedTickers.has(ticker.toUpperCase());
    }

    private generateChartOptions() {
        if (this._positions && this._quotes) {
            // Filter out excluded tickers
            const filteredPositions = this._positions.filter(
                position => !this.excludedTickers.has(position.ticker.toUpperCase())
            );
            
            this.chartOptions = [
                createPositionSizePieChart(filteredPositions),
                createPositionsByCostChart(filteredPositions),
                createPositionMarketValuePieChart(filteredPositions, this._quotes),
                createPositionsByMarketValueChart(filteredPositions, this._quotes),
                createPositionLabelsPieChart(filteredPositions),
                createProfitByStrategyChart(filteredPositions, this._quotes),
                createUnrealizedProfitChart(filteredPositions, this._quotes),
                createDaysHeldVsGainPercentChart(filteredPositions, this._quotes),
                createStopPriceDistanceChart(filteredPositions, this._quotes),
                createPositionsByGainsChart(filteredPositions, this._quotes),
                createRealizedVsUnrealizedProfitChart(filteredPositions, this._quotes),
                createUnrealizedGainPercentageDistributionChart(filteredPositions, this._quotes),
                createUnrealizedRRDistributionChart(filteredPositions, this._quotes),
                createProfitDistributionChart(filteredPositions, this._quotes),
                createDaysHeldDistributionChart(filteredPositions),
            ]
        }
    }
}
