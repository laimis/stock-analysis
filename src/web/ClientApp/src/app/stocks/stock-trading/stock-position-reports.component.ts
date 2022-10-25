import { Component, Input } from '@angular/core';
import { StocksService, TickerOutcomes, OutcomesAnalysisReport } from '../../services/stocks.service';

@Component({
  selector: 'app-position-reports',
  templateUrl: './stock-position-reports.component.html',
  styleUrls: ['./stock-position-reports.component.css']
})
export class StockPositionReportsComponent {

  historicalOutcomes: TickerOutcomes[]
  dailyOutcomes: TickerOutcomes[]
  portfolioDailyReport: OutcomesAnalysisReport;
  portfolioWeeklyReport: OutcomesAnalysisReport;

  sortColumn: string
  sortDirection: number = -1
  

	constructor(private service : StocksService){}

  @Input()
  dailyMode: boolean = false

  @Input()
  allTimeMode: boolean = false


	ngOnInit(): void {
    
    if (this.allTimeMode) {
      this.loadAllTimeData()
    }

    if (this.dailyMode) {
      this.loadDailyData()
    }
  }

  loadAllTimeData() {
    this.service.reportPortfolioOutcomesAllTime().subscribe(result => {
      this.historicalOutcomes = result;
    }, error => {
      console.error(error);
    });
  }

  loadDailyData() {
    this.loadDailyOutcomes()
  }

  loadDailyOutcomes(){
    this.service.reportPortfolioOutcomesDay().subscribe(result => {
      this.dailyOutcomes = result;
      this.loadDailyAnalaysisReport()
    }, error => {
      console.error(error);
    });
  }
  
  loadDailyAnalaysisReport() {
    this.service.reportPorfolioAnalysisDaily().subscribe(result => {
      this.portfolioDailyReport = result;
      this.loadWeeklyAnalaysisReport()
    }, error => {
      console.error(error);
    });
  }

  loadWeeklyAnalaysisReport() {
    this.service.reportPortfolioAnalysisWeekly().subscribe(result => {
      this.portfolioWeeklyReport = result;
    }, error => {
      console.error(error);
    });
  }
}
