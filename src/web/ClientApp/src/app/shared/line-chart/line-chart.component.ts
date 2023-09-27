import {Component, Input} from '@angular/core';
import {ChartAnnotationLine, DataPoint, DataPointContainer} from "../../services/stocks.service";

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

  private createAnnotationDataPoints(annotationLine:ChartAnnotationLine, dataPoints:any[]) {

    // it can be either horizontal or vertical
    // if it is horizontal, it's x is the x of the data points and y is a constant value

    if (annotationLine.chartAnnotationLineType === "horizontal") {
      let data = dataPoints.map(p => {
        return {
          label: p.label,
          x: p.x,
          y: annotationLine.value,
          markerSize: 0
        }
      })

      return data
    } else {
      return null
      // let data = dataPoints.map(p => {
      //   return {
      //     label: p.label,
      //     x: annotationLine.value,
      //     y: p.y
      //   }
      // });
      //
      // return data
    }
  }

  private renderChart(container: DataPointContainer) {
    let dataPoints = container.data.map(p => this.toDataPoint(p))
    let chartType = container.chartType // they match 1:1 today
    let isDate = container.data[0].isDate

    let data = [{
      type: chartType,
      dataPoints: dataPoints
    }]

    if (container.annotationLine) {
      let annotationDataPoints = this.createAnnotationDataPoints(container.annotationLine, dataPoints)
      if (annotationDataPoints !== null) {
        data.push({
          type: "line",
          dataPoints: annotationDataPoints
        })
      }
    }

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
      data: data
    };
  }
}
