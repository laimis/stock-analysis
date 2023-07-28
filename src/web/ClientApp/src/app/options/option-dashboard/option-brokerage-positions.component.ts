import { Component, Input } from '@angular/core';
import { Router } from '@angular/router';
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
    private service : StocksService,
    private router : Router
  ) {}

  @Input()
  positions : BrokerageOptionPosition[]

  turnIntoPosition(position:BrokerageOptionPosition, purchased:string){
      var opt = {
        ticker: position.ticker,
        strikePrice: position.strikePrice,
        optionType: position.optionType,
        expirationDate: new Date(position.expirationDate),
        numberOfContracts: position.quantity,
        premium: position.averageCost * 100,
        filled: purchased,
        notes: null
      }
  
      if (position.quantity > 0) this.recordBuy(opt)
      if (position.quantity < 0) this.recordSell(opt)
    }
  
    recordBuy(opt: object) {
      this.service.buyOption(opt).subscribe( r => {
        this.navigateToOption(r.id)
      }, err => {
        this.errors = GetErrors(err)
      })
    }
  
    recordSell(opt: object) {
      this.service.sellOption(opt).subscribe( r => {
        this.navigateToOption(r.id)
      }, err => {
        this.errors = GetErrors(err)
      })
    }

    navigateToOption(id:string) {
      this.router.navigate(['/optiondetails', id])
    }
}