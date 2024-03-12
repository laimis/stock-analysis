import {Component, OnInit} from '@angular/core';
import {StocksService, Trend, Trends, TrendType} from "../../services/stocks.service";
import {DatePipe, NgClass, NgIf, PercentPipe} from "@angular/common";
import {FormsModule} from "@angular/forms";
import {AppModule} from "../../app.module";
import {LoadingComponent} from "../../shared/loading/loading.component";

@Component({
  selector: 'app-trends-report',
  standalone: true,
    imports: [
        DatePipe,
        PercentPipe,
        NgClass,
        FormsModule,
        NgIf,
        LoadingComponent
    ],
  templateUrl: './trends-report.component.html',
  styleUrl: './trends-report.component.css'
})
export class TrendsReportComponent implements OnInit {
    trends: Trends;
    currentTrend: Trend;
    
    selectedTicker: string;
    selectedStartDate: string;
    selectedTrendType: TrendType;

    constructor(private stockService:StocksService) {
        // Set default values
        this.selectedTicker = "SPY";
        // selected date should be 10 years ago
        let date = new Date();
        date.setFullYear(date.getFullYear() - 10);
        this.selectedStartDate = date.toISOString().substring(0,10);
        this.selectedTrendType = TrendType.Ema20OverSma50
    }
    
    ngOnInit() {
        this.loadTrends()
    }
    
    loadTrends() {
        this.stockService.reportTrends(this.selectedTicker, this.selectedTrendType, this.selectedStartDate).subscribe(data => {
            this.trends = data
            this.currentTrend = data.currentTrend
        });
    }
    
    applyFilters() {
        this.currentTrend = null
        this.trends = null
        this.loadTrends()
    }
}
