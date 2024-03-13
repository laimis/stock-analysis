import {Component, OnInit} from '@angular/core';
import {
    ChartType,
    DataPoint,
    DataPointContainer,
    StocksService,
    Trend,
    TrendDirection,
    Trends,
    TrendType,
    ValueWithFrequency
} from "../../services/stocks.service";
import {DatePipe, NgClass, NgIf, PercentPipe} from "@angular/common";
import {FormsModule} from "@angular/forms";
import {LoadingComponent} from "../../shared/loading/loading.component";
import {StockSearchComponent} from "../../stocks/stock-search/stock-search.component";
import {LineChartComponent} from "../../shared/line-chart/line-chart.component";

@Component({
  selector: 'app-trends-report',
  standalone: true,
    imports: [
        DatePipe,
        PercentPipe,
        NgClass,
        FormsModule,
        NgIf,
        LoadingComponent,
        StockSearchComponent,
        LineChartComponent
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
    
    dataPointContainers: DataPointContainer[] = []
    
    constructor(private stockService:StocksService) {
        // Set default values
        this.selectedTicker = "SPY";
        this.selectedStartDate = "10";
        this.selectedTrendType = TrendType.Ema20OverSma50
    }
    
    ngOnInit() {
        this.loadTrends()
    }
    
    loadTrends() {
        
        const date = new Date();
        date.setFullYear(date.getFullYear() - parseInt(this.selectedStartDate));
        const selectedStartDate = date.toISOString().substring(0,10);
        
        this.stockService.reportTrends(this.selectedTicker, this.selectedTrendType, selectedStartDate).subscribe(data => {
            this.trends = data
            this.currentTrend = data.currentTrend

            // chronological
            let barsChronological: DataPoint[] = data.trends.map((t:Trend) => {
                return {label:t.startDateStr, isDate: true, value: t.direction === TrendDirection.Up ? t.numberOfBars : -t.numberOfBars}
            })
            let gainsChronological: DataPoint[] = data.trends.map((t:Trend) => {return {label:t.startDateStr, isDate: true, value: t.gainPercent}})

            this.dataPointContainers = [
                this.createContainer(data.upBarStatistics.buckets, "Up Bar Distribution"),
                this.createContainer(data.downBarStatistics.buckets, "Down Bar Distribution"),
                this.createContainer(data.upGainStatistics.buckets, "Up Gain Distribution"),
                this.createContainer(data.downGainStatistics.buckets, "Down Gain Distribution"),
                
                // up bars sorted
                this.sortAndCreateContainer(data.upTrends.map((t:Trend) => t.numberOfBars), "Up Bars Sorted"),
                this.sortAndCreateContainer(data.downTrends.map((t:Trend) => t.numberOfBars), "Down Bars Sorted"),
                this.sortAndCreateContainer(data.upTrends.map((t:Trend) => t.gainPercent), "Up Gain Sorted"),
                this.sortAndCreateContainer(data.downTrends.map((t:Trend) => t.gainPercent), "Down Gain Sorted"),
                
                {label: "Bars Chronological", chartType: ChartType.Column, data: barsChronological},
                {label: "Gains Chronological", chartType: ChartType.Column, data: gainsChronological}
            ]
        });
    }
    
    tickerSelected(ticker: string) {
        this.selectedTicker = ticker
        this.applyFilters()
    }
    
    trendTypeSelected() {
        this.applyFilters()
    }
    
    startDateSelected() {
        this.applyFilters()
    }
    
    applyFilters() {
        this.currentTrend = null
        this.trends = null
        this.dataPointContainers = null
        this.loadTrends()
    }
    
    sortAndCreateContainer(numbers:number[], title:string) {
        let sorted: DataPoint[] = numbers.sort((a,b) => a-b).map((value:number, index:number) => {
            return {label: (index + 1).toString(), isDate: false, value: value}
        })
        return {
            label: title,
            chartType: ChartType.Column,
            data: sorted
        }
    }
    
    createContainer(data:ValueWithFrequency[], title:string) {
        let dataPoints: DataPoint[] = []
        data.forEach((b:ValueWithFrequency) => {
            dataPoints.push({
                label: b.value.toString(),
                isDate: false,
                value: b.frequency
            })
        })
        
        return {
            label: title,
            chartType: ChartType.Column,
            data: dataPoints
        }
    }

    protected readonly TrendDirection = TrendDirection;
}
