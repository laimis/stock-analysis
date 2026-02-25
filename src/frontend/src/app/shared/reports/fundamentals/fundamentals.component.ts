import { Component, Input } from '@angular/core';
import { TickerFundamentals, StockQuote } from 'src/app/services/stocks.service';
import { CanvasJSAngularChartsModule } from '@canvasjs/angular-charts';
import { blue, green, red } from 'src/app/services/charts.service';

const orange = '#ff9800';
const lightgray = '#d0d0d0';
const scatterPointMarkerSize = 10;

function getVal(item: TickerFundamentals, key: string): number {
    const v = (item.fundamentals as any)[key];
    return v != null && v !== '' ? +v : NaN;
}

// quadrantLabels order: [Top-Left, Top-Right, Bottom-Left, Bottom-Right]
function createScatterChart(
    data: TickerFundamentals[],
    title: string,
    xKey: string,
    yKey: string,
    xTitle: string,
    yTitle: string,
    xThreshold: number,
    yThreshold: number,
    quadrantLabels: [string, string, string, string]
) {
    const filtered = data.filter(d => !isNaN(getVal(d, xKey)) && !isNaN(getVal(d, yKey)));

    const dataPoints = filtered.map(item => ({
        x: getVal(item, xKey),
        y: getVal(item, yKey),
        label: item.ticker,
        indexLabel: item.ticker,
        indexLabelFontSize: 11,
        toolTipContent: `<strong>${item.ticker}</strong><br/>${xTitle}: {x}<br/>${yTitle}: {y}`
    }));

    if (!dataPoints.length) {
        return { title: { text: title + ' - No Data' }, data: [] };
    }

    const allX = dataPoints.map(p => p.x);
    const allY = dataPoints.map(p => p.y);
    const xMin = Math.min(...allX);
    const xMax = Math.max(...allX);
    const yMin = Math.min(...allY);
    const yMax = Math.max(...allY);
    const xPad = (xMax - xMin) * 0.06 || 1;
    const yPad = (yMax - yMin) * 0.06 || 1;

    // Invisible data points to render quadrant name labels at the four corners
    const labelPoints = [
        { x: xMin - xPad, y: yMax + yPad, indexLabel: quadrantLabels[0], indexLabelFontColor: '#aaa', indexLabelFontSize: 10, markerSize: 0, toolTipContent: ' ' },
        { x: xMax + xPad, y: yMax + yPad, indexLabel: quadrantLabels[1], indexLabelFontColor: '#aaa', indexLabelFontSize: 10, markerSize: 0, toolTipContent: ' ' },
        { x: xMin - xPad, y: yMin - yPad, indexLabel: quadrantLabels[2], indexLabelFontColor: '#aaa', indexLabelFontSize: 10, markerSize: 0, toolTipContent: ' ' },
        { x: xMax + xPad, y: yMin - yPad, indexLabel: quadrantLabels[3], indexLabelFontColor: '#aaa', indexLabelFontSize: 10, markerSize: 0, toolTipContent: ' ' },
    ];

    return {
        animationEnabled: false,
        exportEnabled: true,
        zoomEnabled: true,
        title: { text: title, fontSize: 20 },
        axisX: {
            title: xTitle,
            gridThickness: 0.1,
            titleFontSize: 12,
            labelFontSize: 10,
            stripLines: [{ value: xThreshold, thickness: 1, color: '#bbb', lineDashType: 'dash' }]
        },
        axisY: {
            title: yTitle,
            gridThickness: 0.1,
            titleFontSize: 12,
            labelFontSize: 10,
            stripLines: [{ value: yThreshold, thickness: 1, color: '#bbb', lineDashType: 'dash' }]
        },
        data: [
            {
                type: 'scatter',
                indexLabelPlacement: 'auto',
                markerSize: scatterPointMarkerSize,
                color: blue,
                dataPoints
            },
            {
                type: 'scatter',
                markerSize: 0,
                color: 'transparent',
                dataPoints: labelPoints
            }
        ]
    };
}

function createWeek52RangeChart(data: TickerFundamentals[], quotes: Record<string, StockQuote>) {
    const sorted = [...data].sort((a, b) =>
        (getVal(a, 'high52') - getVal(a, 'low52')) - (getVal(b, 'high52') - getVal(b, 'low52'))
    );

    const rangePoints = sorted.map(item => ({
        label: item.ticker,
        y: [getVal(item, 'low52'), getVal(item, 'high52')],
        toolTipContent: `<strong>${item.ticker}</strong><br/>52W Low: $${getVal(item, 'low52').toFixed(2)}<br/>52W High: $${getVal(item, 'high52').toFixed(2)}`
    }));

    const hasQuotes = Object.keys(quotes).length > 0;
    const pricePoints = hasQuotes
        ? sorted
            .filter(item => quotes[item.ticker])
            .map(item => {
                const quote = quotes[item.ticker];
                const price = quote.lastPrice || quote.closePrice;
                const low52 = getVal(item, 'low52');
                const high52 = getVal(item, 'high52');
                const pos = (price - low52) / (high52 - low52);
                const markerColor = pos >= 0.7 ? green : pos <= 0.3 ? red : orange;
                const tick = (high52 - low52) * 0.012;
                return {
                    label: item.ticker,
                    y: [price - tick, price + tick],
                    color: markerColor,
                    toolTipContent: `<strong>${item.ticker}</strong><br/>Price: $${price.toFixed(2)}<br/>Range position: ${(pos * 100).toFixed(0)}%`
                };
            })
        : [];

    const series: any[] = [{
        type: 'rangeBar',
        color: lightgray,
        dataPoints: rangePoints
    }];

    if (pricePoints.length > 0) {
        series.push({ type: 'rangeBar', dataPoints: pricePoints });
    }

    return {
        animationEnabled: false,
        exportEnabled: true,
        title: { text: '52-Week Price Range', fontSize: 20 },
        axisX: { title: 'Price ($)', titleFontSize: 12, labelFontSize: 10, gridThickness: 0.1, prefix: '$' },
        axisY: { titleFontSize: 12, labelFontSize: 10, gridThickness: 0 },
        data: series
    };
}

function createMarginProfileChart(data: TickerFundamentals[]) {
    return {
        animationEnabled: false,
        exportEnabled: true,
        title: { text: 'Margin Profile (TTM)', fontSize: 20 },
        axisX: { titleFontSize: 12, labelFontSize: 10, gridThickness: 0 },
        axisY: { title: 'Margin %', titleFontSize: 12, labelFontSize: 10, gridThickness: 0.1, suffix: '%' },
        legend: { horizontalAlign: 'center', verticalAlign: 'bottom', fontSize: 12 },
        data: [
            {
                type: 'column',
                name: 'Gross Margin',
                showInLegend: true,
                color: green,
                dataPoints: data.map(item => ({
                    label: item.ticker,
                    y: getVal(item, 'grossMarginTTM'),
                    toolTipContent: `<strong>${item.ticker}</strong><br/>Gross Margin: {y}%`
                }))
            },
            {
                type: 'column',
                name: 'Operating Margin',
                showInLegend: true,
                color: blue,
                dataPoints: data.map(item => ({
                    label: item.ticker,
                    y: getVal(item, 'operatingMarginTTM'),
                    toolTipContent: `<strong>${item.ticker}</strong><br/>Operating Margin: {y}%`
                }))
            },
            {
                type: 'column',
                name: 'Net Margin',
                showInLegend: true,
                color: orange,
                dataPoints: data.map(item => ({
                    label: item.ticker,
                    y: getVal(item, 'netProfitMarginTTM'),
                    toolTipContent: `<strong>${item.ticker}</strong><br/>Net Margin: {y}%`
                }))
            }
        ]
    };
}

@Component({
  selector: 'app-fundamentals',
  standalone: true,
  imports: [CanvasJSAngularChartsModule],
  templateUrl: './fundamentals.component.html',
  styleUrl: './fundamentals.component.css'
})
export class FundamentalsComponent {
    chartOptions: any[] = [];
    private _data: TickerFundamentals[] = [];
    private _quotes: Record<string, StockQuote> = {};

    @Input()
    set data(value: TickerFundamentals[]) {
        if (!value) return;
        this._data = value;
        this.generateCharts();
    }

    get data() { return this._data; }

    @Input()
    set quotes(value: Record<string, StockQuote>) {
        this._quotes = value || {};
        this.generateCharts();
    }

    private generateCharts() {
        if (!this._data?.length) return;

        this.chartOptions = [
            createScatterChart(
                this._data,
                'PE Ratio vs EPS Growth',
                'epsChangePercentTTM', 'peRatio',
                'EPS Growth %', 'PE Ratio',
                15, 25,
                ['Value Traps', 'Growth Premium', 'Bargains', 'Compounders']
            ),
            createScatterChart(
                this._data,
                'Net Margin vs Revenue Growth',
                'revChangeTTM', 'netProfitMarginTTM',
                'Revenue Growth %', 'Net Margin %',
                10, 10,
                ['Profitable but Stagnant', 'Profitable Growers', 'Struggling', 'Burning Cash']
            ),
            createScatterChart(
                this._data,
                'Beta vs EPS Growth',
                'epsChangePercentTTM', 'beta',
                'EPS Growth %', 'Beta',
                15, 1.0,
                ['Risky Laggards', 'Risky Growers', 'Defensive', 'Low Risk Growers']
            ),
            createScatterChart(
                this._data,
                'ROA vs PS Ratio',
                'prRatio', 'returnOnAssets',
                'PS Ratio', 'ROA %',
                3, 5,
                ['Undervalued', 'Fairly Priced', 'Cheap for a Reason', 'Overvalued']
            ),
            createWeek52RangeChart(this._data, this._quotes),
            createMarginProfileChart(this._data),
        ];
    }
}
