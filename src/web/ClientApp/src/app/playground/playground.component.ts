import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {
  DataPointContainer,
  PositionChartInformation,
  PriceFrequency,
  Prices,
  StocksService
} from '../services/stocks.service';
import {GetErrors} from "../services/utils";
import {
  age,
  calculateInflectionPoints, histogramToDataPointContainer, humanFriendlyTime,
  InfectionPointType,
  InflectionPointLog, logToDataPointContainer,
  toChartMarker, toDailyBreakdownDataPointCointainer, toHistogram, toInflectionPointLog
} from "../services/prices.service";


const sixMonths = 365 / 2
const twoMonths = 365 / 6

@Component({
  selector: 'app-playground',
  templateUrl: './playground.component.html',
  styleUrls: ['./playground.component.css']
})
export class PlaygroundComponent implements OnInit {
  tickers: string[];
  options: any;
  prices: Prices;
  chartInfo: PositionChartInformation;
  priceFrequency: PriceFrequency = PriceFrequency.Daily;
  ageValueToUse = twoMonths

  lineContainers: DataPointContainer[];
  peaksAndValleys: DataPointContainer[];
  log: InflectionPointLog[];

  constructor(
    private stocks: StocksService,
    private route: ActivatedRoute) {
  }

  errors: string[];

  ngOnInit() {
    const tickerParam = this.route.snapshot.queryParamMap.get('tickers');
    this.tickers = tickerParam ? tickerParam.split(',') : ['AMD'];
    this.renderPrices(this.tickers)
  }

  renderPrices(tickers: string[]) {
    this.stocks.getStockPrices(tickers[0], 365, this.priceFrequency).subscribe(
      result => {
        this.prices = result

        const inflectionPoints = calculateInflectionPoints(result.prices);
        const peaks = inflectionPoints.filter(p => p.type === InfectionPointType.Peak)
        const valleys = inflectionPoints.filter(p => p.type === InfectionPointType.Valley)

        this.chartInfo = {
          ticker: this.tickers[0],
          prices: result,
          markers: inflectionPoints.map(toChartMarker),
          averageBuyPrice: null,
          stopPrice: null
        }

        const peaksContainer = toDailyBreakdownDataPointCointainer('peaks', peaks)
        const valleysContainer = toDailyBreakdownDataPointCointainer('valleys', valleys)
        const smoothedPeaks = toDailyBreakdownDataPointCointainer('smoothed peaks', peaks, (p: number) => Math.round(p))
        const smoothedValleys = toDailyBreakdownDataPointCointainer('smoothed valleys', valleys, (p: number) => Math.round(p))

        this.log = toInflectionPointLog(inflectionPoints).reverse()

        const humanFriendlyTimeDuration = humanFriendlyTime(this.ageValueToUse)
        const resistanceHistogram = toHistogram(peaks.filter(p => age(p) < this.ageValueToUse))
        const resistanceHistogramPointContainer = histogramToDataPointContainer(humanFriendlyTimeDuration + ' resistance histogram', resistanceHistogram)

        const supportHistogram = toHistogram(valleys.filter(p => age(p) < this.ageValueToUse))
        const supportHistogramPointContainer = histogramToDataPointContainer(humanFriendlyTimeDuration + ' support histogram', supportHistogram)

        const logChart = logToDataPointContainer("Log", this.log)

        this.lineContainers = [
          resistanceHistogramPointContainer, supportHistogramPointContainer, peaksContainer, smoothedPeaks, valleysContainer, smoothedValleys, logChart
        ]

        this.peaksAndValleys = [smoothedPeaks, smoothedValleys]
      },
      error => this.errors = GetErrors(error)
    );
  }

  priceFrequencyChanged() {
    this.chartInfo = null
    this.renderPrices(this.tickers);
  }

  ageValueChanged() {
    this.chartInfo = null
    this.renderPrices(this.tickers);
  }

  protected readonly twoMonths = twoMonths;
  protected readonly sixMonths = sixMonths;
}
