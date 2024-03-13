import {Component, Input} from '@angular/core';
import {ChartAnnotationLine, DataPoint, DataPointContainer} from "../../services/stocks.service";
import {blue} from "../candlestick-chart/candlestick-chart.component";
import {CanvasJSAngularChartsModule} from "@canvasjs/angular-charts";

function toChartJSPoint(p: DataPoint) {
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

function toAnnotationDataPoints(annotationLine:ChartAnnotationLine, dataPoints:any[]) {
  // it can be either horizontal or vertical
  // if it is horizontal, it's x is the x of the data points and y is a constant value

  if (annotationLine.chartAnnotationLineType === "Horizontal") {
    return dataPoints.map(p => {
      return {
        label: p.label,
        x: p.x,
        y: annotationLine.value,
        markerSize: 0
      }
    })
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

function emptyChart(containers:DataPointContainer[]) {
  return {
    title: {
      text: containers.length === 0 ?
        "No data" : (containers.map(c => c.label).join(", ") + " - No data")
    },
    axisX: {
      gridThickness: 0,
    },
    axisY: {
      gridThickness: 0,
    },
    data: []
  };
}

function toChartData(container:DataPointContainer) {
  let chartType = "line"
  if (container.chartType === "Column") {
    chartType = "column"
  } else if (container.chartType === "Line" ) {
    chartType = "line"
  }

  let chartJSDataPoints = container.data.map(p => toChartJSPoint(p))
  let data : any = [{
    type: chartType,
    markerSize: 1,
    color: blue,
    dataPoints: chartJSDataPoints
  }]

  if (container.annotationLine) {
    let annotationDataPoints = toAnnotationDataPoints(container.annotationLine, chartJSDataPoints)
    if (annotationDataPoints !== null) {
      data.push({
        type: "line",
        dataPoints: annotationDataPoints
      })
    }
  }

  return data
}

function toChart(containers:DataPointContainer[]) {

  let title = containers.map(c => c.label).join(", ")
  let isDate = containers[0].data[0].isDate
  let data = []
  containers.forEach(c => {
    toChartData(c).forEach(d => data.push(d))
  })

  return {
    exportEnabled: true,
    zoomEnabled: true,
    title: {
      text: title,
    },
    axisX: {
      valueFormatString: isDate ? "YYYY-MM-DD" : null,
      gridThickness: 0,
      // crosshair: {
      //   enabled: true,
      //   snapToDataPoint: true
      // }
    },
    axisY: {
      gridThickness: 0,
      // crosshair: {
      //   enabled: true
      // }
    },
    data: data
  };
}

@Component({
    selector: 'app-line-chart',
    templateUrl: './line-chart.component.html',
    standalone: true,
    imports: [
        CanvasJSAngularChartsModule
    ]
})
export class LineChartComponent {

  options: any // don't have type from canvasjs .... blerg

  @Input()
  set dataContainer(container: DataPointContainer) {
    if (container) {
      this.renderChart([container]);
    }
  }

  @Input()
  set dataContainers(containers: DataPointContainer[]) {
    if (containers) {
      this.renderChart(containers);
    }
  }

  private renderChart(containers: DataPointContainer[]) {
    this.options = containers.length === 0 || containers[0].data.length === 0 ?
      emptyChart(containers) : toChart(containers);
  }
}
