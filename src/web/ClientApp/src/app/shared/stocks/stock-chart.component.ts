import { ChangeDetectionStrategy, Component, Input, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Chart, ChartDataset, ChartOptions, ChartType, LogarithmicScale } from 'chart.js';
import { Prices, PriceWithDate } from 'src/app/services/stocks.service';
import annotationPlugin, { AnnotationOptions } from 'chartjs-plugin-annotation';
import { BaseChartDirective } from 'ng2-charts';


@Component({
  selector: 'app-stock-chart',
  templateUrl: './stock-chart.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StockChartComponent implements OnInit, OnDestroy {
  
  @ViewChild(BaseChartDirective) chart?: BaseChartDirective;
  
  public chartPlugins = [];

  @Input()
  public chartType : ChartType = 'line';

  public lineChartLegend = true;

  public chartData: ChartDataset[] = [];

  public lineChartLabels: string[] = [];

  @Input()
  public yScaleType: 'linear' | 'logarithmic' = 'logarithmic';

  public lineChartOptions: ChartOptions = {
    responsive: true,
    scales: {
      y: {
        type: this.yScaleType,
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
  earliestDate: string;
  latestDate: string;
  
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
        pointRadius: 0,
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
        pointRadius: 0,
        backgroundColor: this._sma_colors[sma.interval],
        borderColor: this._sma_colors[sma.interval],
        pointStyle: 'line'
      }
    })

    this.chartData = data.concat(sma_data)

    this.lineChartLabels = prices.prices.map(x => x.dateStr).slice(-cutoff)

    var minPrice = Math.min.apply(null, closes)
    this.lineChartOptions.scales.y.min = minPrice - minPrice * 0.1

    if (this._maxPrice == undefined) {
      var maxPrice = Math.max.apply(null, closes)
      this.lineChartOptions.scales.y.max = maxPrice + maxPrice * 0.1
    }

    this.earliestDate = prices.prices[0].dateStr
    this.latestDate = prices.prices[prices.prices.length - 1].dateStr
  }

  toAnnotation(transaction: PriceWithDate, color: string) : AnnotationOptions {
    return {
      type: 'point',
      xValue: transaction.date,
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
    
    var filterFunc = (x: PriceWithDate) => x.date >= this.earliestDate && x.date <= this.latestDate
    
    var annotations : AnnotationOptions[] = 
      this.positivePricePoints
        .filter(filterFunc)
        .map(x => this.toAnnotation(x, this._color_positive))
        .concat(
          this.negativePricePoints
            .filter(filterFunc)
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
  }

  ngOnDestroy() {
    console.log("StockTradingChartComponent.ngOnDestroy()")
    Chart.unregister(LogarithmicScale)
    Chart.unregister(annotationPlugin)
  }

}

