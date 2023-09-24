import {Component, Input} from '@angular/core';
import {createChart, IChartApi} from "lightweight-charts";
import {DailyScore, Prices} from "../../services/stocks.service";

@Component({
  selector: 'app-line-chart',
  templateUrl: './line-chart.component.html',
  styleUrls: ['./line-chart.component.css']
})
export class LineChartComponent {
  chart: IChartApi;

  @Input()
  chartHeight: number = 400;
  private _values: DailyScore[];

  @Input()
  set lineData(values: DailyScore[]) {
    if (values) {
      this._values = values
      this.renderChart();
    }
  }

  private removeChart() {
    if (this.chart) {
      this.chart.remove();
    }
  }

  private renderChart() {
    this.removeChart();

    this.chart = createChart(
      document.getElementById('chart'),
      {
        height: this.chartHeight,
        handleScale: {
          axisPressedMouseMove: false
        },
        handleScroll: {
          mouseWheel: false,
          pressedMouseMove: false,
          vertTouchDrag: false
        }
      }
    );

    let lineSeries = this.chart.addLineSeries();

    lineSeries.setData(this._values.map(v => {
      return {time: v.date.split('T')[0], value: v.score}
    }));

    this.chart.timeScale().fitContent();
  }
}
