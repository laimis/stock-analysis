import { Component, Input } from '@angular/core';
import { DataPointContainer, PositionChartInformation, Prices } from '../../services/stocks.service';
import { PriceChartComponent } from "../price-chart/price-chart.component";
import { NgClass, NgIf, PercentPipe } from '@angular/common';
import { calculateInflectionPoints, getCompleteTrendAnalysis, toChartMarker, TrendAnalysisResult, TrendChangeAlert } from 'src/app/services/prices.service';

@Component({
  selector: 'app-peak-valley-analysis',
  imports: [PriceChartComponent, NgIf, NgClass, PercentPipe],
  templateUrl: './peak-valley-analysis.component.html',
  styleUrl: './peak-valley-analysis.component.css'
})
export class PeakValleyAnalysisComponent {
  trendAnalysisResult: TrendAnalysisResult;
  trendChangeAlert: TrendChangeAlert;
  chartInfo: PositionChartInformation;

  @Input()
  set prices(result: Prices) {
    if (!result) {
      return;
    }
    this.runAnalysisAndSetChartInfo(result);
  }

  @Input()
  set obv(result: DataPointContainer) {
    if (!result) {
      return;
    }
    // ranges for normalization
    const values = result.data.map(item => item.value);
    const min = Math.min(...values);
    const max = Math.max(...values);
    const range = max - min;

    const prices: Prices = {
        prices: result.data.map(r => ({
            open: r.value,
            close: 100 * (r.value - min) / range,
            high: r.value,
            low: r.value,
            volume: 0,
            dateStr: r.label
        })),
        movingAverages: {
            ema20: {interval: 20, values: [],exponential: true},
            sma20: {interval: 20, values: [],exponential: false},
            sma50: {interval: 50, values: [],exponential: false},
            sma150: {interval: 150, values: [],exponential: false},
            sma200: {interval: 200, values: [],exponential: false},
        },
        atr: null,
        atrPercent: null,
        percentChanges: null
    };
    this.runAnalysisAndSetChartInfo(prices);
  }

  runAnalysisAndSetChartInfo(result: Prices) {
    const inflectionPoints = calculateInflectionPoints(result.prices);
    const completeAnalysisResults = getCompleteTrendAnalysis(inflectionPoints, result.prices[result.prices.length - 1])
    this.trendAnalysisResult = completeAnalysisResults.establishedTrend
    this.trendChangeAlert = completeAnalysisResults.potentialChange
    
    this.chartInfo = {
        ticker: "ticker",
        prices: result,
        markers: inflectionPoints.map(toChartMarker),
        transactions: [],
        averageBuyPrice: null,
        stopPrice: null,
        buyOrders: [],
        sellOrders: [],
        renderMovingAverages: false
    }
  }
}
