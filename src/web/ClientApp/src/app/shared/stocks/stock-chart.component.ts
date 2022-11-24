import { Component, Input, OnInit, ViewChild } from '@angular/core';
import { Chart, ChartDataset, ChartOptions, ChartType, LogarithmicScale } from 'chart.js';
import { Prices, PriceWithDate } from 'src/app/services/stocks.service';
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
  
  _positivePricePoints: PriceWithDate[] = [];
  _negativePricePoints: PriceWithDate[] = [];
  
  private readonly _color_positive = "#0000FF"
  private readonly _color_negative = "#ff0000"
  private readonly _color_close = 'rgb(75, 192, 192)'

  private readonly _sma_colors = {
    20: '#ff0000',
    50: '#32a84a',
    150: '#00BCD4',
    200: '#2962FF',
  }
  
  @Input()
  set positivePricePoints(transactions: PriceWithDate[]) {
    this._positivePricePoints = transactions;
    this.updateAnnotations();
  }
  get positivePricePoints() {
    return this._positivePricePoints
  }

  @Input()
  set negativePricePoints(transactions: PriceWithDate[]) {
    this._negativePricePoints = transactions;
    this.updateAnnotations();
  }
  get negativePricePoints() {
    return this._negativePricePoints
  }

  private _positiveLines: number[] = [];
  @Input()
  set positiveLines(lines: number[]) {
    this._positiveLines = lines
    this.updateAnnotations();
  }
  get positiveLines() {
    return this._positiveLines
  }

  private _negativeLines: number[] = [];
  @Input()
  set negativeLines(lines: number[]) {
    this._negativeLines = lines
    this.updateAnnotations();
  }
  get negativeLines() {
    return this._negativeLines
  }

  _maxPrice: number
  @Input()
  set maxPrice(value: number) {
    this._maxPrice = value
    this.lineChartOptions.scales.y.max = value + value * 0.1
  }

  @Input()
  set prices(prices: Prices) {

    if (!prices) {
      return
    }
    
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

    this.lineChartLabels = prices.prices.map(x => x.dateStr).slice(-cutoff)

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
    
    var annotations : AnnotationOptions[] = 
      this.positivePricePoints
        .map(x => this.toAnnotation(x, this._color_positive))
        .concat(
          this.negativePricePoints
            .map(x => this.toAnnotation(x, this._color_negative))
        )
        .concat(
          this.positiveLines
            .map(x => this.toAnnotationLine(x, this._color_positive))
        )
        .concat(
          this.negativeLines
            .map(x => this.toAnnotationLine(x, this._color_negative))
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

