import {Component, Input} from '@angular/core';
import {DataPoint, DataPointContainer} from "../../services/stocks.service";

@Component({
  selector: 'app-line-chart',
  templateUrl: './line-chart.component.html'
})
export class LineChartComponent {

  @Input()
  chartHeight: number = 400;
  options: any

  @Input()
  set dataContainer(container: DataPointContainer) {
    if (container) {
      this.renderChart(container);
    }
  }

  private toDataPoint(p: DataPoint) {

    if (p.isDate) {
      return {
        x: new Date(Date.parse(p.label)),
        y: p.value,
        format: "MMM DD, YYYY"
      }
    } else {
      return {
        label: p.label,
        y: p.value
      }
    }
  }

  private renderChart(container: DataPointContainer) {
    let dataPoints = container.data.map(p => this.toDataPoint(p))
    let chartType = container.chartType // they match 1:1 today
    let isDate = container.data[0].isDate

    this.options = {
      exportEnabled: true,
      zoomEnabled: true,
      title: {
        text: container.label,
      },
      axisX: {
        valueFormatString: isDate ? "YYYY-MM-DD" : null,
        // crosshair: {
        //   enabled: true,
        //   snapToDataPoint: true
        // }
      },
      axisY: {
        // crosshair: {
        //   enabled: true
        // }
      },
      data: [{
        type: chartType,
        dataPoints: dataPoints
      }]
    };
  }
}
