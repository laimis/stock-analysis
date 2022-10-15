import { Component, Input } from '@angular/core';
import { StocksService, PositionAnalysisEntry, PortfolioReport } from '../../services/stocks.service';

@Component({
  selector: 'app-position-reports',
  templateUrl: './stock-position-reports.component.html',
  styleUrls: ['./stock-position-reports.component.css']
})
export class StockPositionReportsComponent {

  overallAnalysis: PositionAnalysisEntry[]
  dailyAnalysis: PositionAnalysisEntry[]
  portfolioDailyReport: PortfolioReport;
  portfolioWeeklyReport: PortfolioReport;

  loaded: boolean = false
  sortColumn: string
  sortDirection: number = -1
  

	constructor(private service : StocksService){}

  @Input()
  showDailyAnalysis: boolean = false

  @Input()
  showOverallAnalysis: boolean = false

  @Input()
  showDailyReport : boolean = false

  @Input()
  showWeeklyReport : boolean = false


	ngOnInit(): void {
    this.loadData()
  }

	loadData() {
    if (this.showDailyAnalysis) {
      this.service.getPortfolioDailyAnalysis().subscribe(result => {
        this.dailyAnalysis = result;
      }, error => {
        console.error(error);
        this.loaded = true;
      });
    }
    
    if (this.showOverallAnalysis) {
      this.service.getPortfolioAnalysis().subscribe(result => {
        this.overallAnalysis = result;
      }, error => {
        console.error(error);
      });
    }

    if (this.showDailyReport) {
      this.service.getPortfolioDailyReport().subscribe(result => {
        this.portfolioDailyReport = result;
      }, error => {
        console.error(error);
      });
    }

    if (this.showWeeklyReport) {
      this.service.getPortfolioWeeklyReport().subscribe(result => {
        this.portfolioWeeklyReport = result;
      }, error => {
        console.error(error);
      });
    }
  } 
}
