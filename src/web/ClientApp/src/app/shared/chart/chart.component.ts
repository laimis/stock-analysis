import { Component, Input, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Chart, ChartDataset, ChartOptions, ChartType, LogarithmicScale } from 'chart.js';
import annotationPlugin from 'chartjs-plugin-annotation';
import { BaseChartDirective } from 'ng2-charts';


@Component({
  selector: 'app-chart',
  templateUrl: './chart.component.html',
})
export class ChartComponent implements OnInit, OnDestroy {
  
  @ViewChild(BaseChartDirective) chart?: BaseChartDirective;
  
  public chartPlugins = [];

  @Input()
  public chartType : ChartType = 'line';

  @Input()
  public chartLegend : boolean = true;

  public chartData: ChartDataset[] = [];

  @Input()
  public chartLabels: string[] = [];

  @Input()
  public set yScaleType(value: 'linear' | 'logarithmic') {
    this.chartOptions.scales.y.type = value
  };

  public chartOptions: ChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
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
  

  @Input()
  set values(valueArray: number[]) {

    var data = [
      {
        data: valueArray
      }]

    this.chartData = data
  }
  
  ngOnInit() {
    console.log("ChartComponent.ngOnInit()")
    Chart.register(LogarithmicScale)
    Chart.register(annotationPlugin)
  }

  ngOnDestroy() {
    console.log("ChartComponent.ngOnDestroy()")
    Chart.unregister(LogarithmicScale)
    Chart.unregister(annotationPlugin)
  }

}

