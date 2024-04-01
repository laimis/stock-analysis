import {Component, Input} from '@angular/core';
import {PositionInstance, StockQuote} from 'src/app/services/stocks.service';
import {CanvasJSAngularChartsModule} from "@canvasjs/angular-charts";
import {blue} from "../../services/charts.service";

const scatterPointMarkerSize = 12;

function generateHistogramData(data: number[], numIntervals: number) {
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
    return [histogramData, minPercentage, maxPercentage, intervalSize];
}

function unrealizedProfit(position: PositionInstance, quote: StockQuote) {
    return position.profit + (quote.price - position.averageCostPerShare) * position.numberOfShares
}

function createUnrealizedProfitChart(entries: PositionInstance[], quotes: Map<string, StockQuote>) {
    const mapped = entries.map(p => {
        return {
            x: p.daysHeld,
            y: unrealizedProfit(p, quotes[p.ticker]),
            label: p.ticker,
            toolTipContent: `<strong>${p.ticker}</strong><br/>Days Held: ${p.daysHeld}<br/>Unrealized Gain: $${unrealizedProfit(p, quotes[p.ticker]).toFixed(2)}`}
    })

    return {
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Unrealized Profit vs Days Held",
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
                markerSize: scatterPointMarkerSize,
                color: blue,
                name: "Position",
                dataPoints: mapped
            }
        ]
    }
}

function unrealizedRR(p: PositionInstance, quote: StockQuote) {
    return (p.profit + p.numberOfShares * (quote.price - p.averageCostPerShare)) / (p.riskedAmount === 0 ? 40 : p.riskedAmount)
}

function createDaysHeldVsGainPercentChart(positions: PositionInstance[], quotes: Map<string, StockQuote>) {
    
    const chartData = positions.map(position => {
        const unrealizedGainPercent = unrealizedGainPercentage(position, quotes[position.ticker]);
        return {
            x: position.daysHeld,
            y: unrealizedGainPercent,
            label: position.ticker,
            toolTipContent: `<strong>${position.ticker}</strong><br/>Days Held: ${position.daysHeld}<br/>Unrealized Gain: ${unrealizedGainPercent.toFixed(2)}%`
        };
    });

    return {
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Days Held vs. Unrealized Gain %",
        },
        axisX: {
            title: "Days Held",
            gridThickness: 0.1
        },
        axisY: {
            title: "Unrealized Gain %",
            gridThickness: 0.1,
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


function createPositionsOpenedChart(positions: PositionInstance[]) {
    
    const positionsOpenedMap = new Map<string, number>();

    positions.forEach(position => {
        const openedDate = position.opened.slice(0, 10); // Extract the date portion of the opened timestamp

        // If the date already exists in the map, increment the count
        if (positionsOpenedMap.has(openedDate)) {
            positionsOpenedMap.set(openedDate, positionsOpenedMap.get(openedDate)! + 1);
        } else {
            // If the date doesn't exist in the map, initialize the count to 1
            positionsOpenedMap.set(openedDate, 1);
        }
    });

    // Convert the map entries to an array of data points
    const dataPoints = Array.from(positionsOpenedMap.entries()).map(([date, count]) => ({
        x: new Date(date),
        y: count
    }));

    // Sort the data points by date in ascending order
    dataPoints.sort((a, b) => a.x.getTime() - b.x.getTime());

    return {
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Positions Opened by Date",
        },
        axisX: {
            title: "Date",
            valueFormatString: "YYYY-MM-DD",
            labelAngle: -45
        },
        axisY: {
            title: "Number of Positions Opened",
            gridThickness: 0.1,
        },
        data: [
            {
                type: "column",
                color: blue,
                dataPoints: dataPoints
            }
        ]
    };
}

function createProfitDistributionChart(positions: PositionInstance[], quotes: Map<string, StockQuote>) {
    const profitData = positions.map(position => unrealizedProfit(position, quotes[position.ticker]));

    const [histogramData, min, max, interval] = generateHistogramData(profitData, 10)

    return {
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Unrealized Profit Distribution",
        },
        axisX: {
            title: "Profit",
            minimum: min,
            maximum: max,
            interval: interval,
            labelFormatter: function(e: any) {
                return "$" + e.value.toFixed(2);
            }
        },
        axisY: {
            title: "Number of Positions",
            gridThickness: 0.1,
        },
        data: [
            {
                type: "column",
                color: blue,
                dataPoints: histogramData
            }
        ]
    };
}

function createPositionSizePieChart(positions: PositionInstance[]) {
    
    const pieChartData = positions.map(position => ({
        y: position.cost,
        label: position.ticker
    })).sort((a, b) => b.y - a.y);

    return {
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Position Sizes",
        },
        data: [
            {
                type: "pie",
                showInLegend: true,
                legendText: "{label}",
                indexLabel: "{label}: {y}",
                indexLabelFontColor: "white",
                indexLabelPlacement: "inside",
                dataPoints: pieChartData
            }
        ]
    };
}

function createDaysHeldDistributionChart(positions: PositionInstance[]) {
    const data = positions.map(position => position.daysHeld);
    const [histogramData, min, max, interval] = generateHistogramData(data, 10)

    return {
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Days Held Distribution",
        },
        axisX: {
            title: "Days Held",
            minimum: min,
            maximum: max,
            interval: interval
        },
        axisY: {
            title: "Number of Positions",
            gridThickness: 0.1,
        },
        data: [
            {
                type: "column",
                color: blue,
                dataPoints: histogramData
            }
        ]
    };
}

function unrealizedGainPercentage(position: PositionInstance, quote: StockQuote) {
    return (quote.price - position.averageCostPerShare) / position.averageCostPerShare * 100;
}

function createUnrealizedGainPercentageDistributionChart(positions: PositionInstance[], quotes: Map<string, StockQuote>) {
    // Calculate unrealized gain percentage for each position
    const gainPercentageData = positions.map(position => unrealizedGainPercentage(position, quotes[position.ticker]));

    const [histogramData, min, max, interval] = generateHistogramData(gainPercentageData, 10)
    
    return {
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Unrealized Gain Percentage Distribution",
        },
        axisX: {
            title: "Gain Percentage",
            minimum: min,
            maximum: max,
            interval: interval,
            labelFormatter: function(e: any) {
                return e.value.toFixed(2) + "%";
            }
        },
        axisY: {
            title: "Number of Positions",
            gridThickness: 0.1,
        },
        data: [
            {
                type: "column",
                color: blue,
                dataPoints: histogramData
            }
        ]
    };
}

function createUnrealizedRRDistributionChart(positions: PositionInstance[], quotes: Map<string, StockQuote>) {
    // Calculate unrealized RR for each position
    const rrData = positions.map(position => unrealizedRR(position, quotes[position.ticker]));

    const [histogramData, min, max, interval] = generateHistogramData(rrData, 10)

    return {
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Unrealized RR Distribution",
        },
        axisX: {
            title: "Risk-Reward Ratio",
            minimum: min,
            maximum: max,
            interval: interval,
            labelFormatter: function(e: any) {
                return e.value.toFixed(2);
            }
        },
        axisY: {
            title: "Number of Positions",
            gridThickness: 0.1,
        },
        data: [
            {
                type: "column",
                color: blue,
                dataPoints: histogramData
            }
        ]
    };
}

function stopPriceDistance(position: PositionInstance, quote: StockQuote) {
    let referencePrice = position.stopPrice ? position.stopPrice : position.averageCostPerShare;
    return ((quote.price - referencePrice) / quote.price) * 100;
}

function createStopPriceDistanceChart(positions: PositionInstance[], quotes: Map<string, StockQuote>) {
    // Calculate stop price distance and position cost for each position
    const scatterData = positions.map(position => ({
        x: unrealizedProfit(position, quotes[position.ticker]),
        y: stopPriceDistance(position, quotes[position.ticker]),
        label: position.ticker,
        toolTipContent: `<strong>${position.ticker}</strong><br/>Stop Distance: ${stopPriceDistance(position, quotes[position.ticker])}<br/>Unrealized Gain: ${unrealizedProfit(position, quotes[position.ticker]).toFixed(2)}%`
    }));

    return {
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Stop Price Distance vs. Position Profit",
        },
        axisX: {
            title: "Position Profit",
            labelFormatter: function(e: any) {
                return "$" + e.value.toFixed(2);
            }
        },
        axisY: {
            title: "Stop Price Distance (%)",
            gridThickness: 0.1,
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

function createPositionLabelsPieChart(positions: PositionInstance[]) {
    // Count the frequency of each label
    const labelCounts = new Map<string, number>();
    positions.forEach(position => {
        position.labels.forEach(label => {
            const currentCount = labelCounts.get(label.value) || 0;
            labelCounts.set(label.value, currentCount + 1);
        });
    });

    const pieChartData = Array.from(labelCounts.entries()).map(([label, count]) => ({
        label: label,
        y: count
    }));

    return {
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Position Labels",
        },
        data: [
            {
                type: "pie",
                showInLegend: true,
                legendText: "{label}",
                indexLabel: "{label}: {y}",
                indexLabelPlacement: "outside",
                dataPoints: pieChartData
            }
        ]
    };
}

function createRealizedVsUnrealizedProfitChart(positions: PositionInstance[], quotes: Map<string, StockQuote>) {
    const chartData = positions.map(position => {
        const realizedProfit = position.profit;
        const unrealizedProfitAmt = unrealizedProfit(position, quotes[position.ticker]);
        return {
            label: position.ticker,
            realized: realizedProfit,
            unrealized: unrealizedProfitAmt
        };
    }).sort((a, b) => (a.realized + a.unrealized) - (b.realized + b.unrealized));

    return {
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Realized vs. Unrealized Profit",
        },
        axisX: {
            title: "Position",
            labelAngle: -45,
            interval: 1
        },
        axisY: {
            title: "Profit",
            gridThickness: 0.1,
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

function createPositionsByCostChart(positions: PositionInstance[]) {
    // Sort positions from smallest to largest based on cost
    const sortedPositions = [...positions].sort((a, b) => a.cost - b.cost);

    const chartData = sortedPositions.map(position => ({
        label: position.ticker,
        y: position.cost
    }));

    return {
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Positions by Cost (Smallest to Largest)",
        },
        axisX: {
            title: "Position",
            labelAngle: -45,
            interval: 1
        },
        axisY: {
            title: "Cost",
            gridThickness: 0.1,
            labelFormatter: function(e: any) {
                return "$" + e.value.toFixed(2);
            }
        },
        data: [
            {
                type: "column",
                dataPoints: chartData
            }
        ]
    };
}

function createPositionsByGainsChart(positions: PositionInstance[], quotes: Map<string, StockQuote>) {
    // Calculate unrealized gains for each position and sort from smallest to largest
    const positionsWithGains = positions.map(position => ({
        ...position,
        unrealizedGain: unrealizedProfit(position, quotes[position.ticker])
    }));
    const sortedPositions = [...positionsWithGains].sort((a, b) => a.unrealizedGain - b.unrealizedGain);

    const chartData = sortedPositions.map(position => ({
        label: position.ticker,
        y: position.unrealizedGain
    }));

    return {
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Positions by Unrealized Gains (Smallest to Largest)",
        },
        axisX: {
            title: "Position",
            labelAngle: -45,
            interval: 1
        },
        axisY: {
            title: "Unrealized Gain",
            gridThickness: 0.1,
            labelFormatter: function(e: any) {
                return "$" + e.value.toFixed(2);
            }
        },
        data: [
            {
                type: "column",
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
                createUnrealizedProfitChart(this._positions, this._quotes),
                createDaysHeldVsGainPercentChart(this._positions, this._quotes),
                createPositionsOpenedChart(this._positions),
                createProfitDistributionChart(this._positions, this._quotes),
                createPositionSizePieChart(this._positions),
                createDaysHeldDistributionChart(this._positions),
                createUnrealizedGainPercentageDistributionChart(this._positions, this._quotes),
                createUnrealizedRRDistributionChart(this._positions, this._quotes),
                createStopPriceDistanceChart(this._positions, this._quotes),
                createPositionLabelsPieChart(this._positions),
                createRealizedVsUnrealizedProfitChart(this._positions, this._quotes),
                createPositionsByCostChart(this._positions),
                createPositionsByGainsChart(this._positions, this._quotes)
            ]
        }
    }
}