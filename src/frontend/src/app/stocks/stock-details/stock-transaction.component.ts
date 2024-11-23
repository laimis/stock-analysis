import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {openpositioncommand, PositionInstance, stocktransactioncommand} from '../../services/stocks.service';
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
        // if position is set, then we are updating an existing position, otherwise we will be opening short/long position
        if (this.position) {
            let op = new stocktransactioncommand()
            op.positionId = this.position.positionId
            op.numberOfShares = this.numberOfShares
            op.price = this.pricePerShare
            op.date = this.filled

            if (this.positionType == 'buy') this.recordBuy(op)
            if (this.positionType == 'sell') this.recordSell(op)
        } else {
            let command = new openpositioncommand()
            command.ticker = this.ticker
            command.numberOfShares = this.numberOfShares
            command.price = this.pricePerShare
            command.date = this.filled
            command.notes = this.notes
            
            if (this.positionType == 'sell') {
                command.numberOfShares = -1 * command.numberOfShares
            }
            
            this.service.openPosition(command).subscribe(_ => {
                this.transactionRecorded.emit("open")
                this.clearFields()
            }, err => {
                this.transactionFailed.emit(GetErrors(err))
            })
        }
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
