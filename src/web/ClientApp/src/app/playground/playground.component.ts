import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';

import { ChartDataset, ChartOptions, Chart, LogarithmicScale } from 'chart.js';

@Component({
  selector: 'app-playground',
  templateUrl: './playground.component.html',
  styleUrls: ['./playground.component.css']
})

export class PlaygroundComponent implements OnInit {

  public lineChartData: ChartDataset[] = [];
  public lineChartLabels: string[] = [];
  public lineChartOptions: ChartOptions = {
    responsive: true,
    scales: {
      y: {
        type: 'logarithmic'
      }
    }
  };
  
  public lineChartLegend = true;
  public lineChartType = 'line';
  public lineChartPlugins = [];

  constructor(private stocks:StocksService) { }

  ngOnInit() {
    Chart.register(LogarithmicScale);
  }

  render() {

    var ticker = "AAPL"

    this.stocks.getStockPrices2y(ticker).subscribe(r => {
      
      var labels = r.map(x => x.date)
      var prices = r.map(x => x.close)

      this.lineChartData = [
        {
          data: prices,
          label: ticker,
          fill: false,
          // borderColor: '#4bc0c0',
          tension: 0.1,
          pointRadius: 1,
          pointBackgroundColor: '#ff0000',
          pointStyle: 'line'
        }]
      this.lineChartLabels = labels

      var minPrice = Math.min.apply(null, prices)
      var maxPrice = Math.max.apply(null, prices)

      this.lineChartOptions.scales.y.max = maxPrice + 20
      this.lineChartOptions.scales.y.min = minPrice - 20
    })
  }

  fetch(ticker:string) {
    console.log(ticker)
    
    // this.stocks.registerForTracking(ticker).subscribe(r => {
    //   this.result = r
    // })
  }

}
