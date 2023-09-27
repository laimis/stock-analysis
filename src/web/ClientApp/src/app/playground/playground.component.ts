import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {DataPointContainer, Prices, StocksService} from '../services/stocks.service';


@Component({
  selector: 'app-playground',
  templateUrl: './playground.component.html',
  styleUrls: ['./playground.component.css']
})

export class PlaygroundComponent implements OnInit {
  tickers: string[];
  prices: Prices;
  options: any;
  manualOptions: any;
  container: DataPointContainer;
  constructor(
    private stocks:StocksService,
    private route:ActivatedRoute) { }

  startDate:string;

  ngOnInit() {
    const tickerParam = this.route.snapshot.queryParamMap.get('tickers');
    if (tickerParam) {
      this.tickers = tickerParam.split(',');
      this.stocks.getStockPrices(this.tickers[0], 365).subscribe(result => {
        this.prices = result
        this.renderNewChart(result)
      });
    }

    this.container = {
      data: [
        {label: "A", value: 2, isDate: false},
        {label: "B", value: 14, isDate: false},
        {label: "C", value: 2, isDate: false}
      ],
      label: "Sample data",
      chartType: 'column',
      annotationLine: null
    };

    this.manualOptions = {
      title:{
        text: "Angular Column Chart"
      },
      animationEnabled: true,
      data: [{
        type: "column",
        dataPoints: [
          { label: "A", y: 71 },
          { label: "B", y: 55 },
          { label: "C", y: 50 }
        ]
      }]
    }
  }

  renderNewChart(prices:Prices) {
    let dataPoints = prices.prices.map(p => {
      let date = new Date(Date.parse(p.dateStr))
      return {
        x: date,
        y: [p.open, p.high, p.low, p.close]
      }
    });

    this.options = {
      logarithmic: true,
      exportEnabled: true,
      zoomEnabled: true,
      title: {
        text: this.tickers[0] + " Price",
      },
      axisX: {
        // valueFormatString: "MMM",
        crosshair: {
          enabled: true,
          valueFormatString: "MMM YYYY",
          snapToDataPoint: true
        }
      },
      axisY: {
        title: "Price in USD",
        prefix: "$",
        crosshair: {
          enabled: true
        }
      },
      data: [{
        type: "candlestick",
        risingColor: "green",
        fallingColor: "red",
        yValueFormatString: "$##.##",
        xValueFormatString: "MMM YYYY",
        dataPoints: dataPoints
      }]
    };
  }

  chart:any
  getChartInstance(chart: object) {
    this.chart = chart;
    let sma = this.prices.sma.sma20.values.map( (p, i) => {
      let date = new Date(Date.parse(this.prices.prices[i].dateStr))
      return {
        x: date,
        y: p
      }
    });

    this.chart.addTo("data", {
      type: "line",
      showInLegend: true,
      markerSize: 0,
      yValueFormatString: "$#,###.00",
      name: "20 sma",
      dataPoints: sma
    });
  }
}

