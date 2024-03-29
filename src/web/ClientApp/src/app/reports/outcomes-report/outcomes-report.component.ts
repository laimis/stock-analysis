import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import {GetErrors, toggleVisuallyHidden} from 'src/app/services/utils';
import { OutcomesReport, StockGaps, StocksService, TickerOutcomes } from '../../services/stocks.service';
import {tap} from "rxjs/operators";
import {concat, forkJoin} from "rxjs";

@Component({
  selector: 'app-outcomes-report',
  templateUrl: './outcomes-report.component.html',
  styleUrls: ['./outcomes-report.component.css']
})
export class OutcomesReportComponent implements OnInit {
  errors: string[] = null;
  startDate: string = null;
  endDate: string = null;
  earnings: string[] = [];
  title: string;
  tickers: string[] = [];
  excludedTickers: string[] = [];

  activeTicker: string = null;

  allBarsReport: OutcomesReport;
  singleBarReportDaily: OutcomesReport;
  singleBarReportWeekly: OutcomesReport;
  earningsOutcomes: TickerOutcomes[];

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

    const tickerParam = this.route.snapshot.queryParamMap.get("tickers");
    if (tickerParam) {
      this.tickers = tickerParam
        .split(",")
        .map(t => {
          const parts = t.split(":");
          if (parts.length == 2) {
            return parts[1]
          }
          return t;
        });

      if (this.tickers.length > 0) {
        this.loadData()
      }

    } else {
      this.errors = ["No tickers were provided"];
    }
  }

  runReports(start, end) {
    if (this.tickers.length === 0) {
      return
    }

    this.startDate = start
    this.endDate = end
    this.loadData()
  }

  loadData() {
    if (this.tickers.length === 0) {
      return
    }

    this.allBarsReport = null
    this.singleBarReportDaily = null
    this.singleBarReportWeekly = null
    this.earningsOutcomes = null

    this.reset()
      
      let singleBarDailyReport = this.stocksService.reportOutcomesSingleBarDaily(this.tickers, "Earnings", this.earnings, this.endDate)
          .pipe(
              tap(
                  report => {
                      this.singleBarReportDaily = report;
                      if (this.earnings.length > 0) {
                          this.earningsOutcomes = report.outcomes.filter(o => this.earnings.indexOf(o.ticker) >= 0);
                      }
                  },
                  error => {
                      this.errors = GetErrors(error)
                  }
          ))

      let singleBarWeekly = this.stocksService.reportOutcomesSingleBarWeekly(this.tickers, this.endDate)
          .pipe(
              tap(
                  report => {
                      this.singleBarReportWeekly = report;
                  },
                  error => {
                      this.errors = GetErrors(error)
                  }
          ))
      
        let allBarsReport = this.stocksService.reportOutcomesAllBars(this.tickers, this.startDate, this.endDate)
            .pipe(
                tap(
                report => {
                    this.allBarsReport = report;
                },
                error => {
                    this.errors = GetErrors(error)
                }
            ))

      forkJoin([singleBarDailyReport, singleBarWeekly, allBarsReport]).subscribe(
          (_) => {
              console.log("All reports done")
          },
          (error) => {
              console.error("Error fork join: " + error)
              this.errors = GetErrors(error)
          }
      )
  }

  private reset() {
    this.errors = null;
    this.allBarsReport = null;
    this.singleBarReportDaily = null;
    this.singleBarReportWeekly = null;
    this.earningsOutcomes = [];
  }

  onTickerChange(activeTicker:string) {
    this.activeTicker = activeTicker;
  }

  excludeEarningsTitle:string = "Exclude Earnings";
  toggleExcludeEarnings() {
    if (this.excludedTickers.length > 0) {
      this.excludedTickers = [];
      this.excludeEarningsTitle = "Exclude Earnings";
    } else {
      this.excludedTickers = this.earnings;
      this.excludeEarningsTitle = "Include Earnings";
    }
  }

  toggleVisibility(form:HTMLElement, button:HTMLElement) {
    toggleVisuallyHidden(form);
    toggleVisuallyHidden(button);
  }
}
