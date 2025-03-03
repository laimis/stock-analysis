import { Component, Input } from '@angular/core';
import { PositionChartInformation, Prices } from '../../services/stocks.service';
import { CandlestickChartComponent } from "../candlestick-chart/candlestick-chart.component";
import { NgClass, NgIf, PercentPipe } from '@angular/common';
import { calculateInflectionPoints, getCompleteTrendAnalysis, toChartMarker, TrendAnalysisResult, TrendChangeAlert } from 'src/app/services/prices.service';

@Component({
  selector: 'app-peak-valley-analysis',
  imports: [CandlestickChartComponent, NgIf, NgClass, PercentPipe],
  templateUrl: './peak-valley-analysis.component.html',
  styleUrl: './peak-valley-analysis.component.css'
})
export class PeakValleyAnalysisComponent {
  trendAnalysisResult: TrendAnalysisResult;
  trendChangeAlert: TrendChangeAlert;
  chartInfo: PositionChartInformation;

  @Input()
  set prices(result: Prices) {
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
