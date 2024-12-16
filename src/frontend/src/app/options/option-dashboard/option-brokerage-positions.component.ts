import {Component, EventEmitter, Input, Output} from '@angular/core';
import {BrokerageOptionPosition, StocksService} from 'src/app/services/stocks.service';
import {GetErrors} from 'src/app/services/utils';


export interface BrokerageOptionPositionCollection {
    positions: BrokerageOptionPosition[]
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
    private _positions: BrokerageOptionPosition[];
    totalMarketValue: number;
    totalCost: number;
    positionCollections: BrokerageOptionPositionCollection[]
    showAllPL: boolean = false;
    
    @Input()
    set positions(val: BrokerageOptionPosition[])
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
        }, {} as {[key: string]: BrokerageOptionPosition[]})
        
        for (let key in grouped) {
            let positions = grouped[key]
            let cost = positions.reduce((acc, pos) => acc + pos.averageCost * pos.quantity, 0) * 100
            let marketValue = positions.reduce((acc, pos) => acc + pos.marketValue, 0)
            this.positionCollections.push({positions, cost, marketValue, showPL: false})
        }
    }
    get positions() {
        return this._positions
    }
    
    @Output()
    positionsUpdated = new EventEmitter()
    protected readonly Date = Date;

    constructor(
        private service: StocksService
    ) {
    }

    togglePL(option: BrokerageOptionPositionCollection) {
        option.showPL = !option.showPL;
    }

    toggleAllPL() {
        this.showAllPL = !this.showAllPL;
        this.positionCollections.forEach(option => option.showPL = this.showAllPL);
    }

    getTodaysDate() {
        return new Date()
    }
    
    getExpirationInDays(option: BrokerageOptionPositionCollection) {
        let numberOfDaysFromNow = (date: number) => {
            let millisPerDay = 1000 * 60 * 60 * 24
            return Math.floor((new Date(date).getTime() - new Date().getTime()) / (millisPerDay))
        }
        
        // all the expiration dates in number of days, sorted and deduped
        let expirations =
            option.positions.map(p => numberOfDaysFromNow(p.expirationDate))
                .sort((a, b) => a - b)
                .filter((val, idx, arr) => arr.indexOf(val) === idx)
        
        // concat them all together with a / separator
        return expirations.join(' / ')
    }

    turnIntoPosition(position: BrokerageOptionPosition, purchased: string) {
        let opt = {
            ticker: position.ticker,
            strikePrice: position.strikePrice,
            optionType: position.optionType,
            expirationDate: new Date(position.expirationDate),
            numberOfContracts: Math.abs(position.quantity),
            premium: position.averageCost * 100,
            filled: purchased,
            notes: null
        }

        if (position.quantity > 0) this.recordBuy(opt)
        if (position.quantity < 0) this.recordSell(opt)
    }

    recordBuy(opt: object) {
        this.service.buyOption(opt).subscribe(r => {
            this.positionsUpdated.emit()
        }, err => {
            this.errors = GetErrors(err)
        })
    }

    recordSell(opt: object) {
        this.service.sellOption(opt).subscribe(r => {
            this.positionsUpdated.emit()
        }, err => {
            this.errors = GetErrors(err)
        })
    }
}
