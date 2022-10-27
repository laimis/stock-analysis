import { Component, OnInit, ViewChild } from '@angular/core';
import { StockGapsResponse, StockPercentChangeResponse, StocksService } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { ChartDataset, ChartOptions, ChartType } from 'chart.js';

@Component({
  selector: 'app-playground',
  templateUrl: './playground.component.html',
  styleUrls: ['./playground.component.css']
})

export class PlaygroundComponent implements OnInit {
  data: StockPercentChangeResponse;
  gaps: StockGapsResponse;

  constructor(private stocks:StocksService, private route: ActivatedRoute) { }

  @ViewChild(BaseChartDirective) chart?: BaseChartDirective;
  
  public lineChartPlugins = [];
  public chartType : ChartType = 'bar';
  public lineChartLegend = true;
  public bucketData: ChartDataset[] = [];
  public bucketLabels: string[] = [];
  public chartOptions: ChartOptions = {
    responsive: true,
    plugins: {
      annotation: {
        annotations: []
      }
    }
  };

  ngOnInit() {
    console.log('PlaygroundComponent.ngOnInit()');
    var ticker = this.route.snapshot.queryParamMap.get('ticker');
    console.log('ticker: ' + ticker);
    if (ticker){
      this.stocks.reportTickerPercentChangeDistribution(ticker).subscribe(data => {
        this.data = data

        this.bucketLabels = data.recent.buckets.map(b => b.percentChange.toString());
        this.bucketData = [
          {
            data: data.recent.buckets.map(b => b.frequency),
            label: "Frequency",
            fill: false
          }]
      });

      this.stocks.reportTickerGaps(ticker).subscribe(data => {
        this.gaps = data
      });
    }
  }
}

