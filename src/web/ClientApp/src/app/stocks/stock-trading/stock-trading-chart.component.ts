import { Component, Input, OnInit } from '@angular/core';
import { Chart, ChartDataset, ChartOptions, ChartType, LogarithmicScale } from 'chart.js';
import { PositionTransaction, StockHistoricalPrice } from 'src/app/services/stocks.service';
import annotationPlugin, { PointAnnotationOptions } from 'chartjs-plugin-annotation';

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
  _buys: PositionTransaction[];
  _sells: PositionTransaction[];

  private readonly _color_buy = "#0000FF"
  private readonly _color_sell = "#ff0000"
  
  @Input()
  set buys(transactions: PositionTransaction[]) {
    this._buys = transactions;
    this.updateAnnotations();
  }

  @Input()
  set sells(transactions: PositionTransaction[]) {
    this._sells = transactions;
    this.updateAnnotations();
  }

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

  toAnnotation(transaction: PositionTransaction, color: string) {
    return {
      type: 'point',
      xValue: transaction.when.split("T")[0],
      yValue: transaction.price,
      backgroundColor: color,
      radius: 3
    }
  }

  updateAnnotations() {
    var buys = this._buys != undefined ? this._buys : [];
    var sells = this._sells != undefined ? this._sells : [];

    var annotations : PointAnnotationOptions[] = 
      buys
        .map(x => this.toAnnotation(x, this._color_buy))
        .concat(
          sells
            .map(x => this.toAnnotation(x, this._color_sell))
        )

    this.lineChartOptions.plugins.annotation.annotations = annotations
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

