import {Component, Input} from '@angular/core';
import {DailyPositionReport, DataPointContainer} from '../../services/stocks.service';
import {LineChartComponent} from "../line-chart/line-chart.component";
import {CanvasJSAngularChartsModule} from "@canvasjs/angular-charts";
import {NgIf} from "@angular/common";
import {parse} from "date-fns";
import {parseDate} from "../../services/utils";

function createData(container: DataPointContainer, useY2: boolean) {
    return {
        type: "line",
        dataPoints: container.data.map(d => {
            let parsedDate = parseDate(d.label)
            return {x: parsedDate, y: d.value}
        }),
        showInLegend: true,
        name: container.label,
        axisYType: useY2 ? "secondary" : "primary"
    }
}

function createCombinedDailyChart(report:DailyPositionReport) {
    console.log(report)
    const data = [
        createData(report.dailyClose, false),
        createData(report.dailyObv, true)
    ]

    return {
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Daily Close / OBV",
        },
        axisX: {
            title: "Date",
            valueFormatString: "YYYY-MM-DD",
            gridThickness: 0.1,
        },
        axisY: {title: "Close", gridThickness: 0.1},
        axisY2: {title: "OBV", gridThickness: 0.1},
        data: data
    }
}

@Component({
    selector: 'app-daily-outcome-scores',
    templateUrl: './daily-outcome-scores.component.html',
    styleUrls: ['./daily-outcome-scores.component.css'],
    imports: [
        LineChartComponent,
        NgIf,
        CanvasJSAngularChartsModule
    ]
})
export class DailyOutcomeScoresComponent {

    gainContainer : DataPointContainer
    profitContainer: DataPointContainer
    
    obvAndCloseOptions: any
    
    @Input()
    showProfit: boolean = false
    
    @Input()
    set report(value: DailyPositionReport) {
        if (!value) throw new Error('report is required')
        
        this.gainContainer = value.dailyGainPct
        this.profitContainer = value.dailyProfit
        
        // obv and close will be rendered on the same chart with
        // different axis
        this.obvAndCloseOptions = createCombinedDailyChart(value)
    }
}
