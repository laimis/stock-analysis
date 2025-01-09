import {Component, EventEmitter, Input, Output} from '@angular/core';
import {BrokerageOptionContract, BrokerageOptionPosition} from "../../services/option.service";
import {ErrorDisplayComponent} from "../../shared/error-display/error-display.component";
import {TradingViewLinkComponent} from "../../shared/stocks/trading-view-link.component";
import {StockLinkComponent} from "../../shared/stocks/stock-link.component";
import {CurrencyPipe, DatePipe, DecimalPipe} from "@angular/common";
import {
    OptionPositionCreateModalComponent
} from "./option-position-create-modal/option-position-create-modal.component";

@Component({
    selector: 'app-option-brokerage-positions',
    templateUrl: './option-brokerage-positions.component.html',
    imports: [
        ErrorDisplayComponent,
        TradingViewLinkComponent,
        StockLinkComponent,
        CurrencyPipe,
        DecimalPipe,
        DatePipe,
        OptionPositionCreateModalComponent
    ],
    styleUrls: ['./option-brokerage-positions.component.css']
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
    
    openPositionDialog(position:BrokerageOptionPosition) {
        this.selectedOption = position
    }
}
