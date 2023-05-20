import { Component, Input } from '@angular/core';
import { ChartOptions, ChartType } from 'chart.js';
import { DataPointContainer, StockTradingPerformanceCollection } from 'src/app/services/stocks.service';


@Component({
  selector: 'app-stock-trading-performance',
  templateUrl: './stock-trading-performance.component.html',
  styleUrls: ['./stock-trading-performance.component.css']
})
export class StockTradingPerformanceComponent {
  
  private _performance:StockTradingPerformanceCollection

  @Input()
  set performance(value:StockTradingPerformanceCollection) {
    this._performance = value
    this.selectTrendsToRenderBasedOnTimeFilter()
  }
  get performance() {
    return this._performance
  }
  
  timeLimit = "ytd"
  trends:DataPointContainer[]

  selectTrendsToRenderBasedOnTimeFilter() {
    if (this.timeLimit == "1y") {
      this.trends = this.performance.trendsOneYear
    } else if (this.timeLimit == "ytd") {
      this.trends = this.performance.trendsYTD
    } else if (this.timeLimit == "2m") {
      this.trends = this.performance.trendsTwoMonths
    } else if (this.timeLimit == "all") {
      this.trends = this.performance.trendsAll
    }
  }

  timeLimitChanged(value:string) {
    if (value != this.timeLimit) {
      this.timeLimit = value
      this.selectTrendsToRenderBasedOnTimeFilter()
    }
  }

  getData(c:DataPointContainer) {
    var data = [
      {
        data: c.data.map(x => x.value),
        label: c.label,
        fill: false,
        tension: 0.1,
        pointRadius: 0,
        // borderWidth: 1,
        pointStyle: 'line'
      }]

    return data
  }

  getLabels(c:DataPointContainer) {
    return c.data.map(x => x.label)
  }

  getChartType(c:DataPointContainer) {
    if (c.chartType == 'bar') {
      return 'bar'
    }
    else if (c.chartType == 'line') {
      return 'line'
    }
    else {
      return 'line'
    }
    
  }

  public lineChartPlugins = [];
  public lineChartType : ChartType = 'line';
  public lineChartLegend = true;
  getOptions(c:DataPointContainer) : ChartOptions {
    let mappedAnnotationArray = []
    if (c.annotationLine) {
      let scaleID = c.annotationLine.chartAnnotationLineType == 'horizontal' ? 'y' : 'x'
      
      mappedAnnotationArray = [
        {
          type: 'line',
          scaleID: scaleID,
          value: c.annotationLine.value,
          borderColor: 'rgb(75, 192, 192)',
          borderWidth: 4,
          label: {
            enabled: true,
            content: c.annotationLine.label
          }
        }
      ]
    }

    var options = {
      responsive: true,
      scales: {
        y: {
          display: true,
        }
      },
      plugins: {
        annotation: {
          annotations: mappedAnnotationArray
        }
      }
    }

    return options
  }

}
