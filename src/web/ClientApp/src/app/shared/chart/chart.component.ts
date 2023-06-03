import { Component, Input, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Chart, ChartDataset, ChartOptions, ChartType, LogarithmicScale } from 'chart.js';
import annotationPlugin from 'chartjs-plugin-annotation';
import { BaseChartDirective } from 'ng2-charts';
import ChartDataLabels from 'chartjs-plugin-datalabels';
import { ChartAnnotationLine } from 'src/app/services/stocks.service';

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

  @Input()
  public set chartAnnotationLine(value: ChartAnnotationLine) {

    let scaleID = value.chartAnnotationLineType == 'vertical' ? 'y' : 'x'

    this.chartOptions.plugins.annotation.annotations = [
      {
        type: 'line',
        scaleID: scaleID,
        value: value.value,
        borderColor: 'rgb(75, 192, 192)',
        borderWidth: 4,
        label: {
          enabled: true,
          content: value.label
        }
      }
    ]
  }

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
  set values(obj:{values: number[], label: string}) {

    var data = [
      {
        label: obj.label,
        data: obj.values
      }]

    this.chartData = data
  }
  
  ngOnInit() {
    console.log("ChartComponent.ngOnInit()")
    Chart.register(LogarithmicScale)
    Chart.register(annotationPlugin)
    // Chart.register(ChartDataLabels) // commented out, too noisy by default
  }

  ngOnDestroy() {
    console.log("ChartComponent.ngOnDestroy()")
    Chart.unregister(LogarithmicScale)
    Chart.unregister(annotationPlugin)
    // Chart.unregister(ChartDataLabels)
  }

}

