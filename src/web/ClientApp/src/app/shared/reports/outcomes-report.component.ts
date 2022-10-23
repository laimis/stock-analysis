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
    
    this.stocksService.reportTickersAnalysisDaily(tickers).subscribe((data) => {
      this.dailyAnalysis = data;

       // summary variable that hodls key/value pair of ticker and count
      
      var summary = new Map<string, number>();
      data.categories.forEach(category => {
        category.outcomes.forEach(outcome => {
          var increment = category.type === 'Positive' ? 1 : -1;
          
          if (summary[outcome.ticker] == undefined) {
            summary[outcome.ticker] = 0;
          }

          summary[outcome.ticker] += increment;

        });
      });
      
      this.summary = summary;
      console.log(this.summary);
    }
  );

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
