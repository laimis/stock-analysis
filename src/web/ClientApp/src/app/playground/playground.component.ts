import { Component, OnInit } from '@angular/core';
import { StocksService, Transaction } from '../services/stocks.service';
import { ChartDataset, ChartOptions, Chart, LogarithmicScale } from 'chart.js';
import annotationPlugin from 'chartjs-plugin-annotation';
import { PointAnnotationOptions } from 'chartjs-plugin-annotation';
import { ActivatedRoute } from '@angular/router';

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
    },
    plugins: {
      annotation: {
        annotations: []
      }
    }
  };
  
  public ticker = undefined;
  public lineChartLegend = true;
  public lineChartType = 'line';
  public lineChartPlugins = [];
  public readyToRender = false;
  public transactions:Transaction[] = []

  constructor(private stocks:StocksService, private route: ActivatedRoute) { }

  ngOnInit() {
    Chart.register(LogarithmicScale);
    Chart.register(annotationPlugin);
    
    this.route.queryParams
      .subscribe(params => {
        console.log(params);
        this.ticker = params.ticker;
        if (this.ticker === undefined) {
          this.ticker = "STNG"
        }
        console.log(this.ticker);
      }
    );

  }

  ngOnDestroy() {
    Chart.unregister(LogarithmicScale);
  }

  render() {

    this.addAnnotations(this.ticker)
  }

  renderPrices(ticker:string) {

    var cutoff = 300
    var buys = this.transactions.filter(x => x.debit > 0)
    if (buys.length > 0) {
      var date = new Date(buys[0].date)
      var today = new Date()
      var days = Math.round((today.getTime() - date.getTime()) / (1000 * 60 * 60 * 24))
      cutoff = days + 20
    }

    this.stocks.getStockPrices2y(ticker).subscribe(r => {
      
      var prices = r.prices.map(x => x.close).slice(-cutoff)

      var data = [
        {
          data: prices,
          label: ticker,
          fill: false,
          tension: 0.1,
          pointRadius: 1,
          borderWidth: 1,
          pointBackgroundColor: '#ff0000',
          pointStyle: 'line'
        }]

      var smaData = r.sma.map(
        x => {
          return {
            data: x.values.slice(-cutoff),
            label: x.description,
            fill: false,
            tension: 0.1,
            pointRadius: 0.5,
            borderWidth: 0.5,
            pointBackgroundColor: null,
            pointStyle: 'line'
          }
        }
      )

      this.lineChartData = data.concat(smaData)

      this.lineChartLabels = r.prices.map(x => x.date).slice(-cutoff)

      var minPrice = Math.min.apply(null, prices)
      var maxPrice = Math.max.apply(null, prices)
      
      this.lineChartOptions.scales.y.max = maxPrice + 20
      this.lineChartOptions.scales.y.min = minPrice - 20

      this.readyToRender = true
    })
  }

  addAnnotations(ticker:string) {
    this.stocks.getStockOwnership(ticker).subscribe(r => {
      
      if (r != null) {
        this.transactions = r.transactions;

        var annotations : PointAnnotationOptions[] = r.transactions
          .map(x => {
            
            return {
              type: 'point',
              xValue: x.date,
              yValue: x.price,
              backgroundColor: x.debit > 0 ? "#0000FF" : '#ff0000',
              radius: 5
            }
        })

        this.lineChartOptions.plugins.annotation.annotations = annotations
      }

      this.renderPrices(ticker)
      
    })
  }

}

