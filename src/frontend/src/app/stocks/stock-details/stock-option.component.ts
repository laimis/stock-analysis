import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {DatePipe} from '@angular/common';
import {GetErrors} from 'src/app/services/utils';
import {OptionPosition, OptionService} from "../../services/option.service";

@Component({
    selector: 'app-stock-option',
    templateUrl: './stock-option.component.html',
    providers: [DatePipe],
    standalone: false
})
export class StockOptionComponent implements OnInit {

    @Input()
    options: OptionPosition[]

    @Input()
    ticker: string

    @Output()
    ownershipChanged = new EventEmitter();

    errors: string[]
    success: boolean

    strikePrice: number
    optionType: string
    expirationDate: string
    positionType: string
    numberOfContracts: number
    premium: number
    filled: string
    notes: string

    constructor(
        private service: OptionService,
        private datePipe: DatePipe) {
    }

    ngOnInit() {
        this.filled = Date()
        this.filled = this.datePipe.transform(this.filled, 'yyyy-MM-dd');
        this.positionType = 'buy'
    }

    clearFields() {
        this.strikePrice = null
        this.optionType = null
        this.positionType = null
        this.numberOfContracts = null
        this.expirationDate = null
        this.premium = null
        this.filled = null
        this.notes = null
    }

    record() {
        var opt = {
            ticker: this.ticker,
            strikePrice: this.strikePrice,
            optionType: this.optionType,
            expirationDate: this.expirationDate,
            numberOfContracts: this.numberOfContracts,
            premium: this.premium,
            filled: this.filled,
            notes: this.notes
        }

        if (this.positionType == 'buy') this.recordBuy(opt)
        if (this.positionType == 'sell') this.recordSell(opt)
    }

    recordBuy(opt: object) {
        this.service.buyOption(opt).subscribe(r => {
            this.ownershipChanged.emit("buy")
            this.clearFields()
            this.success = true
        }, err => {
            this.errors = GetErrors(err)
        })
    }

    recordSell(opt: object) {
        this.service.sellOption(opt).subscribe(r => {
            this.ownershipChanged.emit("sell")
            this.clearFields()
            this.success = true
        }, err => {
            this.errors = GetErrors(err)
        })
    }

    onTickerSelected(ticker: string) {
        this.ticker = ticker;
    }
}
