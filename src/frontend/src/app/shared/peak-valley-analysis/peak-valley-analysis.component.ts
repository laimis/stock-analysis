import { Component, Input } from '@angular/core';
import { ChartMarker, InfectionPointType, InflectionPoint, InflectionPointsReportDetails, LatestTrendAnalysis, PositionChartInformation, PriceBar, TrendAnalysisResult } from '../../services/stocks.service';
import { PriceChartComponent } from "../price-chart/price-chart.component";
import { NgClass, PercentPipe } from '@angular/common';
import { green, red } from 'src/app/services/charts.service';


function toChartMarker(inflectionPoint: InflectionPoint): ChartMarker {
    const bar = inflectionPoint.gradient.dataPoint
    return {
        label: inflectionPoint.priceValue.toFixed(2),
        date: bar.dateStr,
        color: inflectionPoint.type === InfectionPointType.Valley ? green : red,
        shape: inflectionPoint.type === InfectionPointType.Valley ? 'arrowUp' : 'arrowDown'
    }
}

@Component({
  selector: 'app-peak-valley-analysis',
  imports: [PriceChartComponent, NgClass, PercentPipe],
  templateUrl: './peak-valley-analysis.component.html',
  styleUrl: './peak-valley-analysis.component.css'
})
export class PeakValleyAnalysisComponent {
  
  trendAnalysisResult: TrendAnalysisResult;
  trendChangeAlert: LatestTrendAnalysis;
  inflectionPoints: InflectionPoint[];
  chartInfo: PositionChartInformation;

  @Input()
  set completeDataSet(result: InflectionPointsReportDetails) {
    if (!result) {
      return;
    }

    this.inflectionPoints = result.inflectionPoints;
    this.trendAnalysisResult = result.establishedTrend;
    this.trendChangeAlert = result.latestTrend;

    this.chartInfo = {
        ticker: "ticker",
        prices: result.data,
        markers: this.inflectionPoints.map(toChartMarker),
        transactions: [],
        averageBuyPrice: null,
        stopPrice: null,
        buyOrders: [],
        sellOrders: [],
        movingAverages: null
    }
  }
}
