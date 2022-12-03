import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { OutcomesReport, StockGaps, StocksService, TickerOutcomes } from '../../services/stocks.service';

@Component({
  selector: 'app-outcomes-report',
  templateUrl: './outcomes-report.component.html',
  styleUrls: ['./outcomes-report.component.css']
})
export class OutcomesReportComponent implements OnInit {
  gaps: StockGaps[] = []
  error: string = null;
  allBarsReport: OutcomesReport;
  singleBarReportDaily: OutcomesReport;

  constructor (
    private stocksService: StocksService,
    private route: ActivatedRoute) {
  }
  
  ngOnInit(): void {
    var tickerParam = this.route.snapshot.queryParamMap.get("tickers");
    if (tickerParam) {
      var tickers = tickerParam
        .split(",")
        .map(t => {
          var parts = t.split(":");
          if (parts.length == 2) {
            return parts[1]
          }
          return t;
        });
      
      if (tickers.length > 0) {
        this.loadSingleBarReport(tickers);
      }

    } else {
      this.error = "No tickers were provided";
    }

  }
  
  private loadSingleBarReport(tickers: string[]) {
    return this.stocksService.reportOutcomesSingleBarDaily(tickers).subscribe(report => {
      this.singleBarReportDaily = report;
      this.loadAllBarsReport(tickers);
    }, error => {
      this.error = error;
    });
  }

  private loadAllBarsReport(tickers: string[]) {
    return this.stocksService.reportOutcomesAllBars(tickers).subscribe(report => {
      this.allBarsReport = report;
      this.gaps = report.gaps;
    }, error => {
      this.error = error;
    });
  }
}
