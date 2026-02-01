import {Component, Input} from '@angular/core';
import {DailyPositionReport, DataPointContainer} from '../../services/stocks.service';
import {LineChartComponent} from "../line-chart/line-chart.component";
import {CanvasJSAngularChartsModule} from "@canvasjs/angular-charts";
import {NgClass} from '@angular/common';

import {parse} from "date-fns";
import {parseDate} from "../../services/utils";

function createData(container: DataPointContainer, useY2: boolean, color: string) {
    return {
        type: "line",
        dataPoints: container.data.map(d => {
            let parsedDate = parseDate(d.label)
            return {x: parsedDate, y: d.value}
        }),
        showInLegend: true,
        name: container.label,
        axisYType: useY2 ? "secondary" : "primary",
        visible: true,
        color: color,
        lineThickness: 2
    }
}

function createCombinedDailyChart(report:DailyPositionReport, showClose: boolean, showObv: boolean, showAd: boolean) {
    const data = []
    
    if (showClose) {
        data.push(createData(report.dailyClose, false, '#0d6efd')) // Bootstrap primary blue
    }
    if (showObv) {
        data.push(createData(report.dailyObv, true, '#198754')) // Bootstrap success green
    }
    if (showAd) {
        data.push(createData(report.dailyAd, true, '#0dcaf0')) // Bootstrap info cyan
    }

    return {
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Price / Volume Indicators",
        },
        axisX: {
            title: "Date",
            valueFormatString: "YYYY-MM-DD",
            gridThickness: 0.1,
        },
        axisY: {title: "Price", gridThickness: 0.1},
        axisY2: {title: "Volume Indicators", gridThickness: 0.1},
        data: data
    }
}

@Component({
    selector: 'app-daily-outcome-scores',
    templateUrl: './daily-outcome-scores.component.html',
    styleUrls: ['./daily-outcome-scores.component.css'],
    imports: [
    LineChartComponent,
    CanvasJSAngularChartsModule,
    NgClass
]
})
export class DailyOutcomeScoresComponent {

    gainContainer : DataPointContainer
    profitContainer: DataPointContainer
    
    obvAndCloseOptions: any
    private currentReport: DailyPositionReport
    chartKey: number = 0 // Force chart re-render
    
    showClose: boolean = true
    showObv: boolean = true
    showAd: boolean = true
    
    @Input()
    showProfit: boolean = false
    
    @Input()
    set report(value: DailyPositionReport) {
        if (!value) throw new Error('report is required')
        
        this.currentReport = value
        this.gainContainer = value.dailyGainPct
        this.profitContainer = value.dailyProfit
        
        this.updateChart()
    }
    
    updateChart() {
        // Increment key to force chart destruction and recreation
        this.chartKey++
        this.obvAndCloseOptions = createCombinedDailyChart(
            this.currentReport, 
            this.showClose, 
            this.showObv, 
            this.showAd
        )
    }
    
    toggleClose() {
        this.showClose = !this.showClose
        this.updateChart()
    }
    
    toggleObv() {
        this.showObv = !this.showObv
        this.updateChart()
    }
    
    toggleAd() {
        this.showAd = !this.showAd
        this.updateChart()
    }
}
