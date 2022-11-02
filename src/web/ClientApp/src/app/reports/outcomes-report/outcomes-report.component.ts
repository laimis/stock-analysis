import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { OutcomesAnalysisReport, OutcomesReport, StockGaps, StocksService, TickerOutcomes } from '../../services/stocks.service';

@Component({
  selector: 'app-outcomes-report',
  templateUrl: './outcomes-report.component.html',
  styleUrls: ['./outcomes-report.component.css']
})
export class OutcomesReportComponent implements OnInit {
  dayOutcomes: TickerOutcomes[];
  allTimeOutcomes: TickerOutcomes[];
  dailyAnalysis: OutcomesAnalysisReport;
  gaps: StockGaps[]

  error: string = null;

  constructor (
    private stocksService: StocksService,
    private route: ActivatedRoute) {
  }
  
  ngOnInit(): void {
    var tickerParam = this.route.snapshot.queryParamMap.get("tickers");
    if (tickerParam) {
      var tickers = tickerParam.split(",");
      this.runReportTickersAnalysisDaily(tickers);
    } else {
      this.error = "No tickers were provided";
    }

  }


  runReportTickersAnalysisDaily(tickers: string[]) {
    this.stocksService.reportTickersAnalysisDaily(tickers).subscribe((data) => {
      this.dailyAnalysis = data;
      this.generateOutcomesForDay(tickers)
    });
  }
  
  generateOutcomesForDay(tickers: string[]) {

    this.stocksService.reportTickersOutcomesDay(tickers, false).subscribe((data: OutcomesReport) => {
        this.dayOutcomes = data.outcomes;
        this.generateOutcomesForAllTime(tickers)
      }
    );
  }

  generateOutcomesForAllTime(tickers: string[]) {
    this.stocksService.reportTickersOutcomesAllTime(tickers, true).subscribe((data: OutcomesReport) => {
      this.allTimeOutcomes = data.outcomes;
      this.gaps = data.gaps;
      }
    );
  }
  
}
