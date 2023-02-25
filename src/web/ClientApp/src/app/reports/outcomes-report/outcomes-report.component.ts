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
  earningsOutcomes: TickerOutcomes[];
  startDate: string = null;
  endDate: string = null;
  earnings: string[] = null;
  title: string;
  singleBarReportWeekly: OutcomesReport;
  tickers: string[] = [];


  activeTicker: string = null;

  constructor (
    private stocksService: StocksService,
    private route: ActivatedRoute) {
  }
  
  ngOnInit(): void {
    var titleParam = this.route.snapshot.queryParamMap.get("title");
    if (titleParam) {
      this.title = titleParam;
    }

    var earningsParam = this.route.snapshot.queryParamMap.get("earnings");
    if (earningsParam) {
      this.earnings = earningsParam.split(",")
    }

    var startDateParam = this.route.snapshot.queryParamMap.get("startDate");
    if (startDateParam) {
      this.startDate = startDateParam;
    }

    var endDateParam = this.route.snapshot.queryParamMap.get("endDate");
    if (endDateParam) {
      this.endDate = endDateParam;
    }

    var tickerParam = this.route.snapshot.queryParamMap.get("tickers");
    if (tickerParam) {
      this.tickers = tickerParam
        .split(",")
        .map(t => {
          var parts = t.split(":");
          if (parts.length == 2) {
            return parts[1]
          }
          return t;
        });
      
      if (this.tickers.length > 0) {
        this.loadSingleBarReport(this.tickers, this.earnings);
      }

    } else {
      this.error = "No tickers were provided";
    }

  }
  
  private loadSingleBarReport(tickers: string[], earnings: string[]) {
    return this.stocksService.reportOutcomesSingleBarDaily(tickers, "Earnings", earnings, this.endDate).subscribe(report => {
      this.singleBarReportDaily = report;
      if (this.earnings.length > 0) {
        this.earningsOutcomes = report.outcomes.filter(o => this.earnings.indexOf(o.ticker) >= 0);
      }
      this.loadSingleBarReportWeekly(tickers, earnings);
    }, error => {
      this.error = error;
      this.loadSingleBarReportWeekly(tickers, earnings);
    });
  }

  private loadSingleBarReportWeekly(tickers: string[], earnings: string[]) {
    return this.stocksService.reportOutcomesSingleBarWeekly(tickers, this.endDate).subscribe(report => {
      this.singleBarReportWeekly = report;
      this.loadAllBarsReport(tickers);
    }, error => {
      this.error = error;
      this.loadAllBarsReport(tickers);
    });
  }

  private loadAllBarsReport(tickers: string[]) {
    return this.stocksService.reportOutcomesAllBars(tickers, this.startDate, this.endDate).subscribe(report => {
      this.allBarsReport = report;
      this.gaps = report.gaps;
    }, error => {
      this.error = error;
    });
  }

  onTickerChange(activeTicker:string) {
    this.activeTicker = activeTicker;
  }
}
