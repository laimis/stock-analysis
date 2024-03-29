import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {PositionInstance, stocktransactioncommand} from '../../services/stocks.service';
import {DatePipe} from '@angular/common';
import {GetErrors} from 'src/app/services/utils';
import {StockPositionsService} from "../../services/stockpositions.service";

@Component({
    selector: 'app-stock-transaction',
    templateUrl: './stock-transaction.component.html',
    styleUrls: ['./stock-transaction.component.css'],
    providers: [DatePipe]
})
export class StockTransactionComponent implements OnInit {

    @Input()
    position: PositionInstance

    @Input()
    ticker: string

    @Input()
    numberOfShares: number

    @Input()
    pricePerShare: number

    @Output()
    transactionRecorded = new EventEmitter();

    @Output()
    transactionFailed = new EventEmitter<string[]>()

    filled: string
    positionType: string
    notes: string


    constructor(
        private service: StockPositionsService,
        private datePipe: DatePipe
    ) {
    }

    ngOnInit() {
        this.filled = Date()
        this.filled = this.datePipe.transform(this.filled, 'yyyy-MM-dd');
    }

    clearFields() {
        this.numberOfShares = null
        this.pricePerShare = null
        this.positionType = null
        this.notes = null
    }


    record() {
        let op = new stocktransactioncommand()
        op.positionId = this.position.positionId
        op.numberOfShares = this.numberOfShares
        op.price = this.pricePerShare
        op.date = this.filled

        if (this.positionType == 'buy') this.recordBuy(op)
        if (this.positionType == 'sell') this.recordSell(op)
    }

    recordBuy(stock: stocktransactioncommand) {
        this.service.purchase(stock).subscribe(_ => {
            this.transactionRecorded.emit("buy")
            this.clearFields()
        }, err => {
            this.transactionFailed.emit(GetErrors(err))
        })
    }

    recordSell(stock: stocktransactioncommand) {
        this.service.sell(stock).subscribe(_ => {
            this.transactionRecorded.emit("sell")
            this.clearFields()
        }, err => {
            this.transactionFailed.emit(GetErrors(err))
        })
    }

}
