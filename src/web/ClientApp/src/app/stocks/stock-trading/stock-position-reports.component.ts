import { Component, Input } from '@angular/core';
import { StocksService, TickerOutcomes, PortfolioReport } from '../../services/stocks.service';

@Component({
  selector: 'app-position-reports',
  templateUrl: './stock-position-reports.component.html',
  styleUrls: ['./stock-position-reports.component.css']
})
export class StockPositionReportsComponent {

  historicalOutcomes: TickerOutcomes[]
  dailyOutcomes: TickerOutcomes[]
  portfolioDailyReport: PortfolioReport;
  portfolioWeeklyReport: PortfolioReport;

  loaded: boolean = false
  sortColumn: string
  sortDirection: number = -1
  

	constructor(private service : StocksService){}

  @Input()
  showDailyOutcomes: boolean = false

  @Input()
  showOverallOutcomes: boolean = false

  @Input()
  showDailyAnalysisReport : boolean = false

  @Input()
  showWeeklyAnalysisReport : boolean = false


	ngOnInit(): void {
    this.loadData()
  }

	loadData() {
    if (this.showDailyOutcomes) {
      this.service.reportPortfolioOutcomesDaily().subscribe(result => {
        this.dailyOutcomes = result;
      }, error => {
        console.error(error);
        this.loaded = true;
      });
    }
    
    if (this.showOverallOutcomes) {
      this.service.reportPortfolioOutcomesWeekly().subscribe(result => {
        this.historicalOutcomes = result;
      }, error => {
        console.error(error);
      });
    }

    if (this.showDailyAnalysisReport) {
      this.service.getPortfolioDailyReport().subscribe(result => {
        this.portfolioDailyReport = result;
      }, error => {
        console.error(error);
      });
    }

    if (this.showWeeklyAnalysisReport) {
      this.service.getPortfolioWeeklyReport().subscribe(result => {
        this.portfolioWeeklyReport = result;
      }, error => {
        console.error(error);
      });
    }
  } 
}
