import { Component, Input, OnDestroy } from '@angular/core';
import {PriceBar, Prices, SMA} from 'src/app/services/stocks.service';
import {IChartApi, PriceLineOptions, createChart} from 'lightweight-charts';

@Component({
  selector: 'app-lightweight-chart-candlestick',
  templateUrl: './lightweight-chart-candlestick.component.html'
})
export class LightWeightChartCandlestickComponent implements OnDestroy {
  chart: IChartApi;

  @Input()
  chartHeight: number = 400;
  private _prices: Prices;

  @Input()
  set prices(value: Prices) {
    if (value) {
      this._prices = value;
      this.renderChart();
    }
  }

  @Input()
  averageBuyPrice = null;

  @Input()
  stopPrice = null;

  @Input()
  buyDates:string[] = null;

  @Input()
  sellDates:string[] = null;

  ngOnDestroy(): void {
    this.removeChart();
  }

  private removeChart() {
    if (this.chart) {
      this.chart.remove();
    }
  }

  renderChart() {

    console.log('rendering chart')
    console.log(this.buyDates)
    console.log(this.sellDates)

    this.removeChart();

    this.chart = createChart(
      document.getElementById('chart'),
      { height: this.chartHeight }
    );

    let createLineData = (sma:SMA, interval:number, priceBars) => {
      return sma.values
        .filter(v => v != null)
        .map( (p, index) => ({ value: p, time: priceBars[index + interval].time }));
    }

    let toPriceBar = (p:PriceBar) => {
      return { time: p.dateStr, open: p.open, high: p.high, low: p.low, close: p.close }
    }

    let toVolumeBar = (p:PriceBar) => {
      let color = p.close > p.open ? '#26a69a' : '#ef5350'
      return { time: p.dateStr, value: p.volume, color: color }
    }

    let addLineSeries = (sma:SMA, color:string, interval:number, priceBars) => {
      const smaSeries = this.chart.addLineSeries({ color: color, lineWidth: 1, crosshairMarkerVisible: false });
      let smaLineData = createLineData(sma, interval, priceBars)
      smaSeries.setData(smaLineData);
    }

    const barSeries = this.chart.addCandlestickSeries();
    let priceBars = this._prices.prices.map(toPriceBar)
    barSeries.setData(priceBars);

    if (this.averageBuyPrice) {
      let buyPrice : PriceLineOptions = {
        'title': 'avg cost',
        price: this.averageBuyPrice,
        color: 'blue',
        lineWidth: 1,
        lineVisible: true,
        axisLabelColor: 'blue',
        axisLabelTextColor: 'white',
        lineStyle: 0,
        axisLabelVisible: true
      };

      barSeries.createPriceLine(buyPrice);
    }

    if (this.stopPrice) {
      let stopPrice : PriceLineOptions = {
        'title': 'stop',
        price: this.stopPrice,
        color: 'red',
        lineWidth: 1,
        lineVisible: true,
        axisLabelColor: 'red',
        axisLabelTextColor: 'white',
        lineStyle: 0,
        axisLabelVisible: true
      };

      barSeries.createPriceLine(stopPrice);
    }

    addLineSeries(this._prices.sma.sma20, 'red', 20, priceBars);
    addLineSeries(this._prices.sma.sma50, 'green', 50, priceBars);
    addLineSeries(this._prices.sma.sma150, 'lightblue', 150, priceBars);
    addLineSeries(this._prices.sma.sma200, 'blue', 200, priceBars);

    let markers = []
    if (this.buyDates) {
      this.buyDates.forEach(d => {
        markers.push({ time: d, position: 'belowBar', color: 'green', shape: 'arrowUp', text: 'buy' })
      })
    }
    if (this.sellDates) {
      this.sellDates.forEach(d => {
        markers.push({ time: d, position: 'aboveBar', color: 'red', shape: 'arrowDown', text: 'sell' })
      })
    }
    barSeries.setMarkers(markers)
    barSeries.priceScale().applyOptions({
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
    let volumeData = this._prices.prices.map(toVolumeBar)
    volumeSeries.setData(volumeData);

    this.chart.timeScale().setVisibleRange({
      from: priceBars[priceBars.length - 60].time,
      to: priceBars[priceBars.length - 1].time
    });


  }
}
