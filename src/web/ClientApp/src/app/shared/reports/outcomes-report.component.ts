import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { OutcomesAnalysisReport, StocksService, TickerOutcomes } from '../../services/stocks.service';

@Component({
  selector: 'app-outcomes-report',
  templateUrl: './outcomes-report.component.html',
  styleUrls: ['./outcomes-report.component.css']
})
export class OutcomesReportComponent implements OnInit {
  dayOutcomes: TickerOutcomes[];
  allTimeOutcomes: TickerOutcomes[];
  weekOutcomes: TickerOutcomes[];
  dailyAnalysis: OutcomesAnalysisReport;

  summary: Map<string, number>;

  constructor (
    private stocksService: StocksService,
    private route: ActivatedRoute) {
  }
  
  ngOnInit(): void {
    var tickers = this.route.snapshot.queryParamMap.get("tickers").split(",");
    this.runReportTickersAnalysisDaily(tickers);
  }

  runReportTickersAnalysisDaily(tickers: string[]) {
    this.stocksService.reportTickersAnalysisDaily(tickers).subscribe((data) => {
      this.dailyAnalysis = data;
      this.generateOutcomesForDay(tickers)
    });
  }

  generateOutcomesForDay(tickers: string[]) {

    this.stocksService.reportTickersOutcomesDay(tickers).subscribe((data: TickerOutcomes[]) => {
        this.dayOutcomes = data;
        this.generateOutcomesForWeek(tickers)
      }
    );
  }

  generateOutcomesForWeek(tickers: string[]) {

    this.stocksService.reportTickersOutcomesWeek(tickers).subscribe((data: TickerOutcomes[]) => {
        this.weekOutcomes = data;
        this.generateOutcomesForAllTime(tickers);
      }
    );
  }

  generateOutcomesForAllTime(tickers: string[]) {
    this.stocksService.reportTickersOutcomesAllTime(tickers).subscribe((data: TickerOutcomes[]) => {
      this.allTimeOutcomes = data;
      }
    );
  }
  
}
