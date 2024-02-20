import { Component, EventEmitter, Input, Output } from '@angular/core';
import { BrokerageOptionPosition, StocksService } from 'src/app/services/stocks.service';
import { GetErrors } from 'src/app/services/utils';

@Component({
  selector: 'app-option-brokerage-positions',
  templateUrl: './option-brokerage-positions.component.html',
  styleUrls: ['./option-brokerage-positions.component.css']
})

export class OptionBrokeragePositionsComponent {
  errors: string[];
  constructor(
    private service : StocksService
  ) {}

  @Input()
  positions : BrokerageOptionPosition[]

  @Output()
  positionsUpdated = new EventEmitter()
    
    getTodaysDate() {
        return new Date()
    }

  turnIntoPosition(position:BrokerageOptionPosition, purchased:string){
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
      this.service.buyOption(opt).subscribe( r => {
        this.positionsUpdated.emit()
      }, err => {
        this.errors = GetErrors(err)
      })
    }
  
    recordSell(opt: object) {
      this.service.sellOption(opt).subscribe( r => {
        this.positionsUpdated.emit()
      }, err => {
        this.errors = GetErrors(err)
      })
    }

    protected readonly Date = Date;
}
