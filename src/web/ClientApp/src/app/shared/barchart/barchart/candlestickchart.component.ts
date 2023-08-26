import { Component, Input } from '@angular/core';
import { Prices } from 'src/app/services/stocks.service';
import { createChart } from 'lightweight-charts';

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

    const barSeries = chart.addCandlestickSeries();
    let priceBars = this._prices.prices.map(toCandleStickData)
    barSeries.setData(priceBars);
    
    const sma20 = chart.addLineSeries({ color: 'red', lineWidth: 1 });
    let sma20LineData = createLineData(this._prices.sma.sma20, 20, priceBars)
    sma20.setData(sma20LineData);

    const sma50 = chart.addLineSeries({ color: 'green', lineWidth: 1 });
    let sma50LineData = createLineData(this._prices.sma.sma50, 50, priceBars)
    sma50.setData(sma50LineData);

    const sma150 = chart.addLineSeries({ color: 'lightblue', lineWidth: 1 });
    let sma150LineData = createLineData(this._prices.sma.sma150, 150, priceBars)
    sma150.setData(sma150LineData);

    const sma200 = chart.addLineSeries({ color: 'blue', lineWidth: 1 });
    let sma200LineData = createLineData(this._prices.sma.sma200, 200, priceBars)
    sma200.setData(sma200LineData);

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
}
