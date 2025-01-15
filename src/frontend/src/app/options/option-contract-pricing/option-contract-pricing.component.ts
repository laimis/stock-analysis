import {Component, EventEmitter, Input, Output} from '@angular/core';
import {OptionContract, OptionPosition, OptionService} from "../../services/option.service";
import {convertToLocalTime, GetErrors} from "../../services/utils";
import {forkJoin} from 'rxjs';
import {ChartAnnotationLineType, ChartType, DataPointContainer} from "../../services/stocks.service";
import {LineChartComponent} from "../../shared/line-chart/line-chart.component";
import {NgForOf, NgIf} from "@angular/common";
import {LoadingComponent} from "../../shared/loading/loading.component";

@Component({
  selector: 'app-option-contract-pricing',
    imports: [
        LineChartComponent,
        NgForOf,
        LoadingComponent,
        NgIf
    ],
  templateUrl: './option-contract-pricing.component.html',
  styleUrl: './option-contract-pricing.component.css'
})
export class OptionContractPricingComponent {
    dataPointContainers: DataPointContainer[];
    private cost: number;
    loading: boolean = false;

    constructor(private optionService: OptionService) { }
    
    @Input() set position (value : OptionPosition) {
        if (value) {
            console.log('OptionContractPricingComponent: position set:', value);
            this.cost = value.cost;
            this.contracts = value.contracts;
        }
    }
    
    @Input() set contracts (value : OptionContract[]) {
        let dateWithoutMilliseconds = (date: Date) => {
            return new Date(date.getFullYear(), date.getMonth(), date.getDate(), date.getHours(), date.getMinutes(), date.getSeconds())
        }

        let dateStr = (date: Date) => {
            return date.toISOString()
        }
        
        this.loading = true;
        let observables = value.map((contract) => this.optionService.getOptionPricing(contract.details.symbol))

        forkJoin(observables).subscribe({
            next: (pricingResults) => {

                let cost = []
                for (let pricingIndex = 0; pricingIndex < pricingResults[0].length; pricingIndex++) {
                    let total = 0
                    for (let contractIndex = 0; contractIndex < pricingResults.length; contractIndex++) {
                        total += pricingResults[contractIndex][pricingIndex].mark * value[contractIndex].quantity
                    }
                    cost.push(total)
                }

                let underlyingPrice = pricingResults[0].map((op) => op.underlyingPrice)

                let costData = cost.map((c, idx) => {
                    let date = convertToLocalTime(
                        dateWithoutMilliseconds(
                            new Date(pricingResults[0][idx].timestamp)
                        )
                    );
                    return {label: dateStr(date), value: c, isDate: false}
                })

                let underlyingData = underlyingPrice.map((c, idx) => {
                    let date = convertToLocalTime(
                        dateWithoutMilliseconds(
                            new Date(pricingResults[0][idx].timestamp)
                        )
                    );
                    return {label: dateStr(date), value: c, isDate: false}
                })

                let costContainer : DataPointContainer = {
                    label: "Total Cost",
                    chartType: ChartType.Line,
                    data: costData,
                    annotationLine: {
                        chartAnnotationLineType: ChartAnnotationLineType.Horizontal,
                        label: 'Cost',
                        value: this.cost
                    },
                    includeZero: true
                }

                let underlyingContainer : DataPointContainer = {
                    label: "Underlying Price vs Time",
                    chartType: ChartType.Line,
                    data: underlyingData
                }

                this.dataPointContainers = [costContainer, underlyingContainer];
                this.loading = false
            },
            error: (error) => {
                console.error('Error fetching option pricing:', error);
                // Handle error appropriately
                this.loading = false;
                this.errorOccurred.emit(GetErrors(error));
            }
        });
    }

    @Output() errorOccurred = new EventEmitter<string[]>();
}
