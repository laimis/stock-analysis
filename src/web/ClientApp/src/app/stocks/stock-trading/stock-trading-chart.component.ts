import { Component, Input, OnInit } from '@angular/core';
import { Chart, ChartDataset, ChartOptions, ChartType, LogarithmicScale } from 'chart.js';
import { StockHistoricalPrice } from 'src/app/services/stocks.service';
import annotationPlugin from 'chartjs-plugin-annotation';

@Component({
  selector: 'stock-trading-chart',
  templateUrl: './stock-trading-chart.component.html',
})
export class StockTradingChartComponent implements OnInit {
  
  public lineChartPlugins = [];
  public lineChartType : ChartType = 'line';
  public lineChartLegend = true;
  public lineChartData: ChartDataset[] = [];
  public lineChartLabels: string[] = [];
  public lineChartOptions: ChartOptions = {
    responsive: true,
    scales: {
      y: {
        type: 'logarithmic'
      }
    },
    plugins: {
      annotation: {
        annotations: []
      }
    }
  };
  
  _prices: StockHistoricalPrice[];
  
  @Input()
  set prices(prices: StockHistoricalPrice[]) {

    this._prices = prices

    var closes = prices.map(p => p.close)

    var cutoff = 300 // take the last 300 days, this will be changed later

    var data = [
      {
        data: closes.slice(-cutoff),
        label: "Price",
        fill: false,
        tension: 0.1,
        pointRadius: 1,
        borderWidth: 1,
        pointBackgroundColor: '#ff0000',
        pointStyle: 'line'
      }]

    this.lineChartData = data

    this.lineChartLabels = this._prices.map(x => x.date).slice(-cutoff)

    var minPrice = Math.min.apply(null, closes)
    var maxPrice = Math.max.apply(null, closes)
    
    this.lineChartOptions.scales.y.max = maxPrice + maxPrice * 0.1
    this.lineChartOptions.scales.y.min = minPrice - minPrice * 0.1
    
  }
  
  ngOnInit() {
    console.log("StockTradingChartComponent.ngOnInit()");
    Chart.register(LogarithmicScale);
    Chart.register(annotationPlugin);
  }

  ngOnDestroy() {
    console.log("StockTradingChartComponent.ngOnDestroy()");
    Chart.unregister(LogarithmicScale);
    Chart.unregister(annotationPlugin);
  }

}

