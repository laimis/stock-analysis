import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';

import { ChartDataset, ChartOptions, Color } from 'chart.js';

@Component({
  selector: 'app-playground',
  templateUrl: './playground.component.html',
  styleUrls: ['./playground.component.css']
})

export class PlaygroundComponent implements OnInit {

  public lineChartData: ChartDataset[] = [
    { data: [65, 59, 80, 81, 56, 55, 40], label: 'Series A' },
  ];
  public lineChartLabels: string[] = ['January', 'February', 'March', 'April', 'May', 'June', 'July'];
  public lineChartOptions: ChartOptions = {
    responsive: true,
  };
  public lineChartColors: Color[] = ["#000000"];
  public lineChartLegend = true;
  public lineChartType = 'line';
  public lineChartPlugins = [];
  constructor(private stocks:StocksService) { }

  ngOnInit() {}

  fetch(ticker:string) {
    console.log(ticker)
    
    // this.stocks.registerForTracking(ticker).subscribe(r => {
    //   this.result = r
    // })
  }

}
