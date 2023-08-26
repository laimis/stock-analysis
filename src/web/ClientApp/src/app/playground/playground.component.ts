import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DailyScore, PriceBar, StocksService } from '../services/stocks.service';
import { IChartApi, createChart } from 'lightweight-charts';


@Component({
  selector: 'app-playground',
  templateUrl: './playground.component.html',
  styleUrls: ['./playground.component.css']
})

export class PlaygroundComponent implements OnInit {
  dailyScores: DailyScore[];
  tickers: string[];
  chart: IChartApi;
  
  constructor(
    private stocks:StocksService,
    private route:ActivatedRoute) { }

  ticker:string;
  startDate:string;

  private toCandleStickData(p:PriceBar) {
    return { time: p.dateStr, open: p.open, high: p.high, low: p.low, close: p.close }
  }

  ngOnInit() {
    var tickerParam = this.route.snapshot.queryParamMap.get('tickers');
    if (tickerParam) {
      this.tickers = tickerParam.split(',');
    }

    let createLineData = (sma, interval, priceBars) => {
      return sma.values.filter(v => v != null).map( (p, index) => ({ value: p, time: priceBars[index + interval].time }));
    }

    // ask prices for the ticker for the last 365 days

    this.stocks.getStockPrices(this.tickers[0], 365).subscribe(result => {
      this.chart = createChart(document.getElementById('chart'));
      
      const barSeries = this.chart.addCandlestickSeries();
      let priceBars = result.prices.map(this.toCandleStickData)
      barSeries.setData(priceBars);
      
      const sma20 = this.chart.addLineSeries({ color: 'red', lineWidth: 1 });
      let sma20LineData = createLineData(result.sma.sma20, 20, priceBars)
      sma20.setData(sma20LineData);

      const sma50 = this.chart.addLineSeries({ color: 'green', lineWidth: 1 });
      let sma50LineData = createLineData(result.sma.sma50, 50, priceBars)
      sma50.setData(sma50LineData);

      const sma150 = this.chart.addLineSeries({ color: 'lightblue', lineWidth: 1 });
      let sma150LineData = createLineData(result.sma.sma150, 150, priceBars)
      sma150.setData(sma150LineData);

      const sma200 = this.chart.addLineSeries({ color: 'blue', lineWidth: 1 });
      let sma200LineData = createLineData(result.sma.sma200, 200, priceBars)
      sma200.setData(sma200LineData);

      barSeries.createPriceLine({ price: 156, color: 'red', lineWidth: 1, lineStyle: 0, axisLabelVisible: true, title: '100' })

      this.chart.timeScale().fitContent();
    });
    
  }
}

