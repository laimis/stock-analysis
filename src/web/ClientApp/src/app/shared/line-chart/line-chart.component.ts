import {Component, Input} from '@angular/core';
import {DataPoint, DataPointContainer} from "../../services/stocks.service";

@Component({
  selector: 'app-line-chart',
  templateUrl: './line-chart.component.html'
})
export class LineChartComponent {

  @Input()
  chartHeight: number = 400;
  options: {
    data: { dataPoints: { x: Date | string; y: number }[]; type: string }[];
    zoomEnabled: boolean;
    axisY: { crosshair: { enabled: boolean } };
    axisX: { crosshair: { snapToDataPoint: boolean; enabled: boolean },valueFormatString: string };
    exportEnabled: boolean;
    logarithmic: boolean;
    title: { text: string }
  };

  @Input()
  set dataContainer(container: DataPointContainer) {
    if (container) {
      this.renderChart(container);
    }
  }

  private getLineChartData(container: DataPointContainer) {
    let data = container.data.map(p => {
      let x = p.isDate ? new Date(Date.parse(p.label)) : p.label;

      return {
        x: x,
        y: p.value,
        format: p.isDate ? "MMM DD, YYYY" : null
      }
    });

    let isDate = container.data[0].isDate;

    return [{
      type: "line",
      xValueFormatString: isDate ? "MMM DD, YYYY" : null,
      dataPoints: data
    }];
  }

  private getColumnChartData(container: DataPointContainer) {
    let data = container.data.map(p => {
      let x = p.isDate ? new Date(Date.parse(p.label)) : p.label;

      return {
        x: x,
        y: p.value,
        format: p.isDate ? "MMM DD, YYYY" : null
      }
    });

    return [{
      type: "column",
      dataPoints: data
    }];
  }

  private renderChart(container: DataPointContainer) {
    let data = container.chartType === 'line' ? this.getLineChartData(container) : this.getColumnChartData(container);
    let isDate = container.data[0].isDate;
    this.options = {
      logarithmic: true,
      exportEnabled: true,
      zoomEnabled: true,
      title: {
        text: container.label,
      },
      axisX: {
        valueFormatString: isDate ? "YYYY-MM-DD" : null,
        crosshair: {
          enabled: true,
          snapToDataPoint: true
        }
      },
      axisY: {
        // title: <-- should start passing this back perhaps
        crosshair: {
          enabled: true
        }
      },
      data: data
    };
  }
}
