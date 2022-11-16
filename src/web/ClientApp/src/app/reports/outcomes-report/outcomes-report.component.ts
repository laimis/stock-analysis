import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { OutcomesReport, StockGaps, StocksService, TickerOutcomes } from '../../services/stocks.service';

@Component({
  selector: 'app-outcomes-report',
  templateUrl: './outcomes-report.component.html',
  styleUrls: ['./outcomes-report.component.css']
})
export class OutcomesReportComponent implements OnInit {
  dayOutcomes: TickerOutcomes[];
  allTimeOutcomes: TickerOutcomes[];
  dailyReport: OutcomesReport;
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
      
    } else {
      this.error = "No tickers were provided";
    }

  }
  
}
