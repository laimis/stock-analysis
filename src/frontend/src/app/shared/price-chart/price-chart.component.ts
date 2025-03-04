import {AfterViewInit, Component, Input, OnDestroy} from '@angular/core';
import {
    ChartMarker,
    MovingAverages,
    PositionChartInformation,
    PriceBar,
    StockTransaction
} from 'src/app/services/stocks.service';
import {
    createChart,
    CrosshairMode,
    IChartApi,
    PriceLineOptions,
    PriceScaleMode,
    SeriesMarker,
    Time
} from 'lightweight-charts';
import {blue, green, lightblue, red, white} from "../../services/charts.service";

const numberOfVisibleBars = 60

function createLineData(movingAverages: MovingAverages, interval: number, priceBars) {
    return movingAverages.values.slice(interval)
        .map((p, index) => ({value: p, time: priceBars[index + interval].time}));
}

function toPriceBar(p: PriceBar) {
    return {time: p.dateStr, open: p.open, high: p.high, low: p.low, close: p.close}
}

function toVolumeBar(p: PriceBar) {
    let color = p.close > p.open ? green : red
    return {time: p.dateStr, value: p.volume, color: color}
}

function addLineSeries(chart: IChartApi, movingaAverages: MovingAverages, color: string, interval: number, priceBars) {
    const smaSeries = chart.addLineSeries({color: color, lineWidth: 1, crosshairMarkerVisible: false});
    let smaLineData = createLineData(movingaAverages, interval, priceBars)
    smaSeries.setData(smaLineData);
}

function createPriceLine(title: string, price: number, color: string) {
    let priceLine: PriceLineOptions = {
        'title': title,
        price: price,
        color: color,
        lineWidth: 1,
        lineVisible: true,
        axisLabelColor: color,
        axisLabelTextColor: white,
        lineStyle: 0,
        axisLabelVisible: true
    };

    return priceLine
}

export function toSeriesMarker(marker: ChartMarker): SeriesMarker<Time> {
    return {
        time: marker.date,
        position: marker.shape == 'arrowUp' ? 'belowBar' : 'aboveBar',
        color: marker.color,
        shape: marker.shape,
        text: marker.label
    }
}

@Component({
    selector: 'app-price-chart',
    templateUrl: './price-chart.component.html',
})
export class PriceChartComponent implements OnDestroy, AfterViewInit {
    chart: IChartApi;
    chartInformationData: PositionChartInformation;
    viewInitialized = false;

    @Input()
    chartHeight: number = 400;
    
    chartId: string;

    constructor() {
        this.chartId = 'chart-' + Math.random().toString(36).substring(7);
        console.log("chartId: " + this.chartId)
    }

    @Input()
    chartType: 'candlestick' | 'line' = 'candlestick';

    @Input()
    priceScaleMode: 'normal' | 'logarithmic' = 'logarithmic';

    @Input()
    set chartInformation(value: PositionChartInformation) {
        this.chartInformationData = value;
        if (value && this.viewInitialized) {
            this.renderChart(value);
        }
    }
    
    ngOnDestroy(): void {
        this.removeChart();
    }

    ngAfterViewInit(): void {
        this.viewInitialized = true;
        if (this.chartInformationData) {
            this.renderChart(this.chartInformationData);
        }
    }

    renderChart(info: PositionChartInformation) {

        const element = document.getElementById(this.chartId);

        this.removeChart();

        let priceScaleMode = this.priceScaleMode == 'logarithmic' ? PriceScaleMode.Logarithmic : PriceScaleMode.Normal

        this.chart = createChart(
            element,
            {
                height: this.chartHeight,
                rightPriceScale: {
                    mode: priceScaleMode
                },
                crosshair: {
                    mode: CrosshairMode.Normal
                }
            }
        );

        let priceBars = info.prices.prices.map(toPriceBar)
        
        let mainSeries;

        if (this.chartType == 'candlestick') {
            mainSeries = this.chart.addCandlestickSeries();
            mainSeries.setData(priceBars);
        } else {
            // For line chart, we need to transform the data to close prices only
            mainSeries = this.chart.addLineSeries({
                color: 'rgba(56, 121, 217, 1)',
                lineWidth: 2,
                crosshairMarkerVisible: true,
                crosshairMarkerRadius: 4
            });
            
            const lineData = info.prices.prices.map(p => ({
                time: p.dateStr,
                value: p.close
            }));
            
            mainSeries.setData(lineData);
        }

        if (info.averageBuyPrice) {
            mainSeries.createPriceLine(
                createPriceLine('avg cost', info.averageBuyPrice, blue)
            );
        }

        if (info.stopPrice) {
            mainSeries.createPriceLine(
                createPriceLine('stop', info.stopPrice, red)
            );
        }
        
        if (info.buyOrders) {
            info.buyOrders.forEach((b) => {
                mainSeries.createPriceLine(
                    createPriceLine('B', b, green)
                );
            });
        }
        
        if (info.sellOrders) {
            info.sellOrders.forEach((s) => {
                mainSeries.createPriceLine(
                    createPriceLine('S', s, red)
                );
            });
        }

        if (info.renderMovingAverages) {
            addLineSeries(this.chart, info.prices.movingAverages.ema20, red, info.prices.movingAverages.ema20.interval, priceBars);
            addLineSeries(this.chart, info.prices.movingAverages.sma50, green, info.prices.movingAverages.sma50.interval, priceBars);
            addLineSeries(this.chart, info.prices.movingAverages.sma150, lightblue, info.prices.movingAverages.sma150.interval, priceBars);
            addLineSeries(this.chart, info.prices.movingAverages.sma200, blue, info.prices.movingAverages.sma200.interval, priceBars);
        }

        let markers = []

        info.transactions
            .filter((t: StockTransaction) => t.type == 'buy')
            .forEach((t: StockTransaction) => {
                markers.push({date: t.date, label: 'B ' + t.numberOfShares, color: green, shape: 'arrowUp'})
            })

        info.transactions
            .filter((t: StockTransaction) => t.type == 'sell')
            .forEach((t: StockTransaction) => {
                markers.push({date: t.date, label: 'S ' + t.numberOfShares, color: red, shape: 'arrowDown'})
            })

        info.markers.forEach((m: ChartMarker) => markers.push(m))

        console.log("markers: " + markers.length)

        if (markers.length > 0) {
            let seriesMarkers = markers.map(toSeriesMarker)
            console.log(seriesMarkers)
            seriesMarkers.sort((a, b) => a.time.toLocaleString().localeCompare(b.time.toLocaleString()))
            mainSeries.setMarkers(seriesMarkers)
        }

        mainSeries.priceScale().applyOptions({
            scaleMargins: {
                top: 0.1, // highest point of the series will be 10% away from the top
                bottom: 0.4, // lowest point will be 40% away from the bottom
            },
        });

        // volume
        const volumeSeries = this.chart.addHistogramSeries({
            priceFormat: {
                type: 'volume',
            },

            priceScaleId: '', // set as an overlay by setting a blank priceScaleId
        });
        volumeSeries.priceScale().applyOptions({
            // set the positioning of the volume series
            scaleMargins: {
                top: 0.7, // highest point of the series will be 70% away from the top
                bottom: 0,
            },
        });
        let volumeData = info.prices.prices.map(toVolumeBar)
        volumeSeries.setData(volumeData);

        this.chart.timeScale().setVisibleRange({
            from: priceBars[priceBars.length - numberOfVisibleBars].time,
            to: priceBars[priceBars.length - 1].time
        });
    
    }

    private removeChart() {
        if (this.chart) {
            this.chart.remove();
        }
    }
}
