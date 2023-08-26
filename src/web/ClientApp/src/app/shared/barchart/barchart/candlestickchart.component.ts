import { Component, Input } from '@angular/core';
import { Prices } from 'src/app/services/stocks.service';
import { PriceLineOptions, createChart } from 'lightweight-charts';

@Component({
  selector: 'app-candlestickchart',
  templateUrl: './candlestickchart.component.html',
  styleUrls: ['./candlestickchart.component.css']
})
export class CandlestickChartComponent {
  renderChart() {
    const chart = createChart(
      document.getElementById('chart'),
      { height: this.chartHeight }
    );

    let createLineData = (sma, interval, priceBars) => {
      return sma.values
        .filter(v => v != null)
        .map( (p, index) => ({ value: p, time: priceBars[index + interval].time }));
    }

    let toCandleStickData = (p) => {
      return { time: p.dateStr, open: p.open, high: p.high, low: p.low, close: p.close }
    }

    let addLineSeries = (sma, color, interval, priceBars) => {
      const smaSeries = chart.addLineSeries({ color: color, lineWidth: 1, crosshairMarkerVisible: false });
      let smaLineData = createLineData(sma, interval, priceBars)
      smaSeries.setData(smaLineData);
    }

    const barSeries = chart.addCandlestickSeries();
    let priceBars = this._prices.prices.map(toCandleStickData)
    barSeries.setData(priceBars);
    
    if (this.averageBuyPrice) {
      var buyPrice : PriceLineOptions = {
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
      var stopPrice : PriceLineOptions = {
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
    
    chart.timeScale().fitContent();
  }

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
}
