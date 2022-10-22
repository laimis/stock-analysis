import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AnalysisCategoryGrouping, StocksService, TickerOutcomes } from '../../services/stocks.service';

@Component({
  selector: 'app-outcomes-report',
  templateUrl: './outcomes-report.component.html',
  styleUrls: ['./outcomes-report.component.css']
})
export class OutcomesReportComponent implements OnInit {
  dayOutcomes: TickerOutcomes[];
  allTimeOutcomes: TickerOutcomes[];
  weekOutcomes: TickerOutcomes[];

  constructor (
    private stocksService: StocksService,
    private route: ActivatedRoute) {
  }
  
  ngOnInit(): void {
    var tickers = this.route.snapshot.queryParamMap.get("tickers").split(",");
    
    this.stocksService.reportTickersOutcomesDay(tickers).subscribe((data: TickerOutcomes[]) => {
        this.dayOutcomes = data;
      }
    );

    this.stocksService.reportTickersOutcomesWeek(tickers).subscribe((data: TickerOutcomes[]) => {
      this.weekOutcomes = data;
    }
  );

    this.stocksService.reportTickersOutcomesAllTime(tickers).subscribe((data: TickerOutcomes[]) => {
      this.allTimeOutcomes = data;
    }
  );
  }

  
}
