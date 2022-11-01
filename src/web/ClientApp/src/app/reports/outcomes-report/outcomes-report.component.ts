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

  summary: string[];
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
      this.calculateSummary(data);
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

  calculateSummary(data: OutcomesAnalysisReport) {
    // go over each category in data.categories and return an array of tickers
    // sorted by the number of times they appear in categories
    var summary = new Map<string, number>();
    data.categories.forEach((category) => {
      category.outcomes.forEach((outcome) => {
        var toAdd = category.type === 'Positive' ? 1 : -1;
        if (summary.has(outcome.ticker)) {
          summary.set(outcome.ticker, summary.get(outcome.ticker) + toAdd);
        } else {
          summary.set(outcome.ticker, toAdd);
        }
      });
    });

    // sort the map by the number of times the ticker appears
    var sorted = [...summary.entries()].sort((a, b) => b[1] - a[1]);
    
    // go over each sorted item and create string
    var arrOfValues = []
    sorted.forEach((item) => {
      arrOfValues.push(item[0] + " (" + item[1] + ")")
    });
    this.summary = arrOfValues;
    console.log(this.summary)
  }

  getSymbol(input:string) {
    return input.split(" ")[0];
  }
  
}
