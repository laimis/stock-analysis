import {Component, EventEmitter, Input, Output} from '@angular/core';
import {OptionContract, OptionPosition, OptionService} from "../../services/option.service";
import {convertToLocalTime, GetErrors} from "../../services/utils";
import {forkJoin} from 'rxjs';
import {ChartType, DataPointContainer} from "../../services/stocks.service";
import {LineChartComponent} from "../../shared/line-chart/line-chart.component";
import {CurrencyPipe, NgForOf, NgIf} from "@angular/common";
import {LoadingComponent} from "../../shared/loading/loading.component";

@Component({
  selector: 'app-option-contract-pricing',
    imports: [
        LineChartComponent,
        NgForOf,
        LoadingComponent,
        NgIf,
        CurrencyPipe
    ],
  templateUrl: './option-contract-pricing.component.html',
  styleUrl: './option-contract-pricing.component.css'
})
export class OptionContractPricingComponent {
    dataPointContainers: DataPointContainer[];
    private cost: number;
    minPrice: number;
    maxPrice: number;
    loading: boolean = false;
    hasPrice: boolean = false;

    constructor(private optionService: OptionService) { }
    
    @Input() set position (value : OptionPosition) {
        if (value) {
            this.cost = value.cost;
            this.contracts = value.contracts;
        }
    }
    
    @Input() set contracts (contracts : OptionContract[]) {
        let dateWithoutMilliseconds = (date: Date) => {
            return new Date(date.getFullYear(), date.getMonth(), date.getDate(), date.getHours(), date.getMinutes(), date.getSeconds())
        }

        let dateStr = (date: Date) => {
            return date.toISOString()
        }
        
        this.loading = true;
        let observables = contracts.map((contract) => this.optionService.getOptionPricing(contract.brokerageSymbol))

        forkJoin(observables).subscribe({
            next: (pricingResults) => {

                let cost : number[] = []
                let minPrice = Number.MAX_VALUE
                let maxPrice = -Number.MAX_VALUE
                for (let pricingIndex = 0; pricingIndex < pricingResults[0].length; pricingIndex++) {
                    let total = 0
                    for (let contractIndex = 0; contractIndex < pricingResults.length; contractIndex++) {
                        let multiplier = contracts[contractIndex].isShort ? -1 : 1
                        total += pricingResults[contractIndex][pricingIndex].mark * multiplier
                    }
                    // always use absolute value for total as we might be doing credit spreads
                    total = Math.abs(total)
                    if (total < minPrice) {
                        minPrice = total
                    }
                    if (total > maxPrice) {
                        maxPrice = total
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
                    includeZero: true
                }

                let underlyingContainer : DataPointContainer = {
                    label: "Underlying Price vs Time",
                    chartType: ChartType.Line,
                    data: underlyingData
                }

                let individualContractOI = pricingResults.map((pricing) => {
                    return pricing.map((op) => op.openInterest);
                });
                let individualContractOIContainers = individualContractOI.map((mark, idx) => {
                    return {
                        label: "$" + contracts[idx].strikePrice + " Open Interest",
                        chartType: ChartType.Line,
                        data: mark.map((m, i) => {
                            let date = convertToLocalTime(
                                dateWithoutMilliseconds(
                                    new Date(pricingResults[0][i].timestamp)
                                )
                            );
                            return {label: dateStr(date), value: m, isDate: false}
                        })
                    }
                });

                let individualCost = pricingResults.map((pricing) => {
                    return pricing.map((op) => op.mark);
                });
                let individualContractMarkContainers = individualCost.map((mark, idx) => {
                    return {
                        label: "$" + contracts[idx].strikePrice + " Mark",
                        chartType: ChartType.Line,
                        data: mark.map((m, i) => {
                            let date = convertToLocalTime(
                                dateWithoutMilliseconds(
                                    new Date(pricingResults[0][i].timestamp)
                                )
                            );
                            return {label: dateStr(date), value: m, isDate: false}
                        })
                    }
                });

                let individualIV = pricingResults.map((pricing) => {
                    return pricing.map((op) => op.volatility);
                });
                let individualIVContainers = individualIV.map((iv, idx) => {
                    return {
                        label: "$" + contracts[idx].strikePrice + " IV",
                        chartType: ChartType.Line,
                        data: iv.map((m, i) => {
                            let date = convertToLocalTime(
                                dateWithoutMilliseconds(
                                    new Date(pricingResults[0][i].timestamp)
                                )
                            );
                            // check if IV is 999, usually indicates bad data, set that to 0
                            if (m === -999) {
                                m = 0;
                            }
                            return {label: dateStr(date), value: m, isDate: false}
                        })
                    }
                });

                this.dataPointContainers = [costContainer, underlyingContainer, ...individualContractMarkContainers, ...individualContractOIContainers, ...individualIVContainers];
                this.minPrice = minPrice;
                this.maxPrice = maxPrice;
                this.hasPrice = costContainer.data.length > 0;
                this.loading = false;
            },
            error: (error) => {
                console.error('Error fetching option pricing:', error);
                // Handle error appropriately
                this.loading = false;
                this.errorOccurred.emit(GetErrors(error));
            },
            complete: () => {
                this.loading = false;
            }
        });
    }

    @Output() errorOccurred = new EventEmitter<string[]>();
}
