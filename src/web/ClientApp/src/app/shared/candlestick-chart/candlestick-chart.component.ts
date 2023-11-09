import { Component, Input, OnDestroy } from '@angular/core';
import {ChartMarker, PositionChartInformation, PriceBar, SMA} from 'src/app/services/stocks.service';
import {IChartApi, PriceLineOptions, createChart, SeriesMarker, Time} from 'lightweight-charts';

const numberOfVisibleBars = 60

export const blue = '#2196f3'
export const green = '#26a69a'
export const red = '#ef5350'
export const white = '#ffffff'
export const lightblue = '#add8e6'

function createLineData(sma:SMA, interval:number, priceBars) {
  return sma.values
    .filter(v => v != null)
    .map( (p, index) => ({ value: p, time: priceBars[index + interval].time }));
}

function toPriceBar (p:PriceBar) {
  return { time: p.dateStr, open: p.open, high: p.high, low: p.low, close: p.close }
}

function toVolumeBar (p:PriceBar) {
  let color = p.close > p.open ? green : red
  return { time: p.dateStr, value: p.volume, color: color }
}

function addLineSeries (chart:IChartApi, sma:SMA, color:string, interval:number, priceBars) {
  const smaSeries = chart.addLineSeries({ color: color, lineWidth: 1, crosshairMarkerVisible: false });
  let smaLineData = createLineData(sma, interval, priceBars)
  smaSeries.setData(smaLineData);
}

function createPriceLine(title:string, price:number, color:string) {
  let priceLine : PriceLineOptions = {
    'title': title,
    price: price,
    color: color,
    lineWidth: 1,
    lineVisible: true,
    axisLabelColor: color,
    axisLabelTextColor: white,
    lineStyle: 0,
    axisLabelVisible: true
  };

  return priceLine
}

export function toSeriesMarker(marker:ChartMarker) : SeriesMarker<Time> {
  return {
    time: marker.date,
    position: marker.shape == 'arrowUp' ? 'belowBar' : 'aboveBar',
    color: marker.color,
    shape: marker.shape,
    text: marker.label
  }
}

@Component({
  selector: 'app-candlestick-chart',
  templateUrl: './candlestick-chart.component.html'
})
export class CandlestickChartComponent implements OnDestroy {
  chart: IChartApi;

  @Input()
  chartHeight: number = 400;

  @Input()
  set chartInformation(value: PositionChartInformation) {
    if (value) {
      this.renderChart(value);
    }
  }

  ngOnDestroy(): void {
    this.removeChart();
  }

  private removeChart() {
    if (this.chart) {
      this.chart.remove();
    }
  }

  renderChart(info:PositionChartInformation) {

    this.removeChart();

    this.chart = createChart(
      document.getElementById("chart"),
      { height: this.chartHeight }
    );

    const barSeries = this.chart.addCandlestickSeries();
    let priceBars = info.prices.prices.map(toPriceBar)
    barSeries.setData(priceBars);

    if (info.averageBuyPrice) {
      barSeries.createPriceLine(
        createPriceLine('avg cost', info.averageBuyPrice, blue)
      );
    }

    if (info.stopPrice) {
      barSeries.createPriceLine(
        createPriceLine('stop', info.stopPrice, red)
      );
    }

    addLineSeries(this.chart, info.prices.sma.sma20, red, info.prices.sma.sma20.interval, priceBars);
    addLineSeries(this.chart, info.prices.sma.sma50, green, info.prices.sma.sma50.interval, priceBars);
    addLineSeries(this.chart, info.prices.sma.sma150, lightblue, info.prices.sma.sma150.interval, priceBars);
    addLineSeries(this.chart, info.prices.sma.sma200, blue, info.prices.sma.sma200.interval, priceBars);

    if (info.markers) {
      let markers = info.markers.map(toSeriesMarker)
      console.log(markers)
      markers.sort((a, b) => a.time.toLocaleString().localeCompare(b.time.toLocaleString()))
      barSeries.setMarkers(markers)
    }

    barSeries.priceScale().applyOptions({
      scaleMargins: {
        top: 0.1, // highest point of the series will be 10% away from the top
        bottom: 0.4, // lowest point will be 40% away from the bottom
      },
    });

    // volume
    const volumeSeries = this.chart.addHistogramSeries({
      priceFormat: {
        type: 'volume',
      },

      priceScaleId: '', // set as an overlay by setting a blank priceScaleId
    });
    volumeSeries.priceScale().applyOptions({
      // set the positioning of the volume series
      scaleMargins: {
        top: 0.7, // highest point of the series will be 70% away from the top
        bottom: 0,
      },
    });
    let volumeData = info.prices.prices.map(toVolumeBar)
    volumeSeries.setData(volumeData);

    this.chart.timeScale().setVisibleRange({
      from: priceBars[priceBars.length - numberOfVisibleBars].time,
      to: priceBars[priceBars.length - 1].time
    });


  }
}
