import { Component, OnInit, inject } from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {
    StockPosition,
    StockTradingPerformanceCollection,
    TradingStrategyPerformance
} from 'src/app/services/stocks.service';
import {StockPositionsService} from "../../services/stockpositions.service";
import {GetErrors} from "../../services/utils";
import { LoadingComponent } from "src/app/shared/loading/loading.component";
import { NgClass } from '@angular/common';
import { StockTradingReviewComponent } from "./stock-trading-review.component";
import { StockTradingStrategiesComponent } from "src/app/shared/stocks/stock-trading-strategies.component";
import { StockTradingClosedPositionsComponent } from "./stock-trading-closed-positions.component";
import { StockTradingPerformanceComponent } from "./stock-trading-performance.component";

@Component({
    selector: 'app-stock-trading-review-dashboard',
    templateUrl: './stock-trading-review-dashboard.component.html',
    styleUrls: ['./stock-trading-review-dashboard.component.css'],
    standalone: true,
    imports: [LoadingComponent, NgClass, RouterLink, StockTradingReviewComponent, StockTradingStrategiesComponent, StockTradingClosedPositionsComponent, StockTradingPerformanceComponent]
})
export class StockTradingReviewDashboardComponent implements OnInit {
    private route = inject(ActivatedRoute);
    private stockService = inject(StockPositionsService);

    activeTab: string = 'positions';
    past: StockPosition[];
    performance: StockTradingPerformanceCollection;
    strategies: TradingStrategyPerformance[]
    errors: string[] = []
    loading = {
        positions: true,
        performance: true
    }

    ngOnInit() {
        this.route.params.subscribe(param => {
            this.activeTab = param['tab'] || 'positions'
        })
        this.loadEntries()
    }

    loadEntries() {
        this.stockService.getPastTradingEntries().subscribe(
            response => {
                this.past = response.past
                this.loading.positions = false
                this.loadPerformance()
            }, err => {
                this.loading.positions = false
                this.errors = GetErrors(err)
            }
        )
    }

    loadPerformance() {
        this.stockService.getPastTradingPerformance().subscribe(
            response => {
                this.performance = response.performance
                this.strategies = response.strategyPerformance
                this.loading.performance = false
            }, err => {
                this.loading.performance = false
                this.errors = GetErrors(err)
            }
        )
    }

    refresh() {
        this.loading.positions = true
        this.loading.performance = true
        this.errors = []
        this.loadEntries()
    }

    isActive(tabName: string) {
        return tabName == this.activeTab
    }

    activateTab(tabName: string) {
        this.activeTab = tabName
    }
}
