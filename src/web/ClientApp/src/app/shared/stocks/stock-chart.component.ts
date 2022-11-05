import { Component, Input, OnInit, ViewChild } from '@angular/core';
import { Chart, ChartDataset, ChartOptions, ChartType, LogarithmicScale } from 'chart.js';
import { PositionTransaction, Price, Prices, PriceWithDate } from 'src/app/services/stocks.service';
import annotationPlugin, { AnnotationOptions } from 'chartjs-plugin-annotation';
import {CrosshairPlugin} from 'chartjs-plugin-crosshair';
import { BaseChartDirective } from 'ng2-charts';


@Component({
  selector: 'app-stock-chart',
  templateUrl: './stock-chart.component.html',
})
export class StockChartComponent implements OnInit {
  
  @ViewChild(BaseChartDirective) chart?: BaseChartDirective;
  
  public lineChartPlugins = [];
  public lineChartType : ChartType = 'line';
  public lineChartLegend = true;
  public lineChartData: ChartDataset[] = [];
  public lineChartLabels: string[] = [];
  public lineChartOptions: ChartOptions = {
    responsive: true,
    scales: {
      y: {
        type: 'logarithmic',
        display: true,
      }
    },
    plugins: {
      annotation: {
        annotations: []
      }
    }
  };
  
  _buys: PriceWithDate[];
  _sells: PriceWithDate[];
  _horizontalLines: number[] = [];

  private readonly _color_buy = "#0000FF"
  private readonly _color_sell = "#ff0000"
  private readonly _color_close = 'rgb(75, 192, 192)'
  private readonly _color_horizontal_line = '#2962FF' // blue

  private readonly _sma_colors = {
    20: '#ff0000',
    50: '#32a84a',
    150: '#00BCD4',
    200: '#2962FF',
  }
  
  @Input()
  set buys(transactions: PriceWithDate[]) {
    this._buys = transactions;
    this.updateAnnotations();
  }

  @Input()
  set sells(transactions: PriceWithDate[]) {
    this._sells = transactions;
    this.updateAnnotations();
  }

  @Input()
  set horizontalLines(lines: number[]) {
    // take out null values
    lines = lines.filter(x => !isNaN(x))
    console.log('horizontalLines: ' + lines)
    this._horizontalLines = lines
    this.updateAnnotations();
  }

  _maxPrice: number
  @Input()
  set maxPrice(value: number) {
    this._maxPrice = value
    this.lineChartOptions.scales.y.max = value + value * 0.1
  }

  @Input()
  set prices(prices: Prices) {

    var closes = prices.prices.map(p => p.close)

    var cutoff = 300 // take the last 300 days, this will be changed later

    var data = [
      {
        data: closes.slice(-cutoff),
        label: "Close",
        fill: false,
        tension: 0.1,
        pointRadius: 1,
        borderWidth: 1,
        backgroundColor: this._color_close,
        borderColor: this._color_close,
        pointStyle: 'line'
      }]

    var smas = [prices.sma.sma20, prices.sma.sma50, prices.sma.sma150, prices.sma.sma200]

    var sma_data = smas.map( sma => {
      return {
        data: sma.values.slice(-cutoff),
        label: sma.interval + " SMA",
        fill: false,
        tension: 0.1,
        pointRadius: 1,
        borderWidth: 1,
        backgroundColor: this._sma_colors[sma.interval],
        borderColor: this._sma_colors[sma.interval],
        pointStyle: 'line'
      }
    })

    this.lineChartData = data.concat(sma_data)

    this.lineChartLabels = prices.prices.map(x => x.date).slice(-cutoff)

    var minPrice = Math.min.apply(null, closes)
    this.lineChartOptions.scales.y.min = minPrice - minPrice * 0.1

    if (this._maxPrice == undefined) {
      var maxPrice = Math.max.apply(null, closes)
      this.lineChartOptions.scales.y.max = maxPrice + maxPrice * 0.1
    }
    
  }

  toAnnotation(transaction: PriceWithDate, color: string) : AnnotationOptions {
    return {
      type: 'point',
      xValue: transaction.when.split("T")[0],
      yValue: transaction.price,
      backgroundColor: color,
      radius: 3
    }
  }

  toAnnotationLine(value: number, color: string) : AnnotationOptions {
    return {
      type: 'line',
      yMin: value,
      yMax: value,
      borderColor: color
    }
  }

  updateAnnotations() {
    var buys = this._buys != undefined ? this._buys : [];
    var sells = this._sells != undefined ? this._sells : [];
    var lines = this._horizontalLines != undefined ? this._horizontalLines : [];

    var annotations : AnnotationOptions[] = 
      buys
        .map(x => this.toAnnotation(x, this._color_buy))
        .concat(
          sells
            .map(x => this.toAnnotation(x, this._color_sell))
        )
        .concat(
          lines
            .map(x => this.toAnnotationLine(x, this._color_horizontal_line))
        )

    this.lineChartOptions.plugins.annotation.annotations = annotations
    
    if (this.chart) {
      this.chart.ngOnChanges({})
      // this.chart.update()
      console.log('chart updated')
    }
  }
  
  ngOnInit() {
    console.log("StockTradingChartComponent.ngOnInit()")
    Chart.register(LogarithmicScale)
    Chart.register(annotationPlugin)
    Chart.register(CrosshairPlugin)
  }

  ngOnDestroy() {
    console.log("StockTradingChartComponent.ngOnDestroy()")
    Chart.unregister(LogarithmicScale)
    Chart.unregister(annotationPlugin)
    Chart.unregister(CrosshairPlugin)
  }

}

