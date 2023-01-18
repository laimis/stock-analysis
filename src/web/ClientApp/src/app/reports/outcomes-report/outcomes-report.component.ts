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
  startDate: string = null;

  constructor (
    private stocksService: StocksService,
    private route: ActivatedRoute) {
  }
  
  ngOnInit(): void {
    var earningsParam = this.route.snapshot.queryParamMap.get("earnings");
    var earnings:string[] = null
    if (earningsParam) {
      earnings = earningsParam.split(",")
    }

    var startDateParam = this.route.snapshot.queryParamMap.get("startDate");
    if (startDateParam) {
      this.startDate = startDateParam;
    }

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
        this.loadSingleBarReport(tickers, earnings);
      }

    } else {
      this.error = "No tickers were provided";
    }

  }
  
  private loadSingleBarReport(tickers: string[], earnings: string[]) {
    return this.stocksService.reportOutcomesSingleBarDaily(tickers, "Earnings", earnings).subscribe(report => {
      this.singleBarReportDaily = report;
      this.loadAllBarsReport(tickers);
    }, error => {
      this.error = error;
    });
  }

  private loadAllBarsReport(tickers: string[]) {
    return this.stocksService.reportOutcomesAllBars(tickers, this.startDate).subscribe(report => {
      this.allBarsReport = report;
      this.gaps = report.gaps;
    }, error => {
      this.error = error;
    });
  }
}
