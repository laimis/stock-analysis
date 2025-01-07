import {Component, EventEmitter, Input, Output} from '@angular/core';
import {GetErrors, GetOptionStrategies} from 'src/app/services/utils';
import {BrokerageOptionContract, OptionService} from "../../services/option.service";


export interface BrokerageOptionPosition {
    brokerageContracts: BrokerageOptionContract[]
    cost: number
    marketValue: number
    showPL: boolean
}

@Component({
    selector: 'app-option-brokerage-positions',
    templateUrl: './option-brokerage-positions.component.html',
    styleUrls: ['./option-brokerage-positions.component.css'],
    standalone: false
})
export class OptionBrokeragePositionsComponent {
    errors: string[];
    private _positions: BrokerageOptionContract[];
    totalMarketValue: number;
    totalCost: number;
    positionCollections: BrokerageOptionPosition[]
    showAllPL: boolean = false;
    
    @Input()
    set positions(val: BrokerageOptionContract[])
    {
    this._positions = val
        this.totalMarketValue = this._positions.reduce((acc, pos) => acc + pos.marketValue, 0)
        this.totalCost = this._positions.reduce((acc, pos) => acc + pos.averageCost * pos.quantity, 0) * 100
        
        this.positionCollections = []
        
        let grouped = this._positions.reduce((acc, pos) => {
            let key = pos.ticker
            if (!acc[key]) acc[key] = []
            acc[key].push(pos)
            return acc
        }, {} as {[key: string]: BrokerageOptionContract[]})
        
        for (let key in grouped) {
            let positions = grouped[key]
            let cost = positions.reduce((acc, pos) => acc + pos.averageCost * pos.quantity, 0) * 100
            let marketValue = positions.reduce((acc, pos) => acc + pos.marketValue, 0)
            this.positionCollections.push({brokerageContracts: positions, cost, marketValue, showPL: false})
        }
    }
    get positions() {
        return this._positions
    }
    
    @Output()
    positionsUpdated = new EventEmitter()
    protected readonly Date = Date;

    constructor(
        private optionService: OptionService
    ) {
        this.optionStrategies = GetOptionStrategies()
    }

    togglePL(option: BrokerageOptionPosition) {
        option.showPL = !option.showPL;
    }

    toggleAllPL() {
        this.showAllPL = !this.showAllPL;
        this.positionCollections.forEach(option => option.showPL = this.showAllPL);
    }

    getTodaysDate() {
        return new Date()
    }
    
    getExpirationInDays(option: BrokerageOptionPosition) {
        let numberOfDaysFromNow = (date: string) => {
            let millisPerDay = 1000 * 60 * 60 * 24
            return Math.floor((new Date(Date.parse(date)).getTime() - new Date().getTime()) / (millisPerDay))
        }
        
        // all the expiration dates in number of days, sorted and deduped
        let expirations =
            option.brokerageContracts.map(p => numberOfDaysFromNow(p.expirationDate))
                .sort((a, b) => a - b)
                .filter((val, idx, arr) => arr.indexOf(val) === idx)
        
        // concat them all together with a / separator
        return expirations.join(' / ')
    }
    
    selectedOption: BrokerageOptionPosition;
    positionNotes: string;
    positionStrategy: string;
    optionStrategies: { key: string, value: string }[] = []
    isPaperPosition: boolean;
    
    openPositionDialog(position:BrokerageOptionPosition) {
        this.selectedOption = position
    }
    
    createPosition(filledDate:string) {
        this.turnIntoPosition(this.selectedOption, filledDate)
    }

    turnIntoPosition(position: BrokerageOptionPosition, filledDate: string) {
        console.log('mapping', position, filledDate)
        
        // this will need to be completely rewritten
        let command = {
            underlyingTicker: position.brokerageContracts[0].ticker,
            filled: filledDate,
            notes: this.positionNotes,
            strategy: this.positionStrategy,
            isPaperPosition: this.isPaperPosition,
            contracts: position.brokerageContracts.map(l => ({
                quantity: l.quantity,
                strikePrice: l.strikePrice,
                expirationDate: l.expirationDate,
                optionType: l.optionType,
                cost: l.averageCost,
                filled: filledDate
            }))
        }
        
        this.optionService.open(command).subscribe({
                next: (position) => {
                    console.log('next', position)
                    this.positionsUpdated.emit()
                },
                error: (err) => {
                    console.log('error', err)
                    this.errors = GetErrors(err)
                },
                complete: () => {
                    console.log('complete')
                }
            }
        )
    }

    recordBuy(opt: object) {
        this.optionService.buyOption(opt).subscribe(r => {
            this.positionsUpdated.emit()
        }, err => {
            this.errors = GetErrors(err)
        })
    }

    recordSell(opt: object) {
        this.optionService.sellOption(opt).subscribe(r => {
            this.positionsUpdated.emit()
        }, err => {
            this.errors = GetErrors(err)
        })
    }
}
