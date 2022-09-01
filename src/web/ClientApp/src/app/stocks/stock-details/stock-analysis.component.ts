import { Component, Input, OnInit } from '@angular/core';
import { StockAnalysis, StocksService } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-stock-analysis',
  templateUrl: './stock-analysis.component.html',
  styleUrls: ['./stock-analysis.component.css']
})
export class StockAnalysisComponent implements OnInit {
  analysis: StockAnalysis;

  constructor(
    private stockService : StocksService
  ) { }

  @Input()
  ticker: string;

  ngOnInit(): void {
    this.stockService.getStockAnalysis(this.ticker).subscribe(
      data => {
        this.analysis = data
      }
    );
  }

  priceDiffFromHigh(): number {
    return (this.analysis.high.close - this.analysis.price) / this.analysis.high.close;
  }

  priceDiffFromLow(): number {
    return (this.analysis.low.close - this.analysis.price) / this.analysis.price;
  }

  getLatestSma(smaIndex:number): number {
    return this.analysis.historicalPrices.sma[smaIndex].values.splice(-1)[0]
  }

  get200Sma(): number {
    return this.getLatestSma(3)
  }

  get150Sma(): number {
    return this.getLatestSma(2)
  }

  get50Sma(): number {
    return this.getLatestSma(1)
  }

  get20Sma(): number {
    return this.getLatestSma(0)
  }

  is20Healthy(): boolean {
    return this.get20Sma() < this.analysis.price
  }

  is50Healthy(): boolean {
    return this.get50Sma() < this.analysis.price
      && this.get50Sma() < this.get20Sma()
  }

  is150Healthy(): boolean {
    return this.get150Sma() < this.analysis.price
      && this.get150Sma() < this.get50Sma()
  }

  is200Healthy(): boolean {
    return this.get200Sma() < this.analysis.price
      && this.get200Sma() < this.get150Sma()
  }

}
