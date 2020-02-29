import { Component, OnInit, Input } from '@angular/core';
import { StocksService, GetErrors } from '../services/stocks.service';
import { Router } from '@angular/router';
import { DatePipe, Location } from '@angular/common';

@Component({
  selector: 'stock-ownership',
  templateUrl: './stock-ownership.component.html',
  providers: [DatePipe]
})
export class StockOwnershipComponent implements OnInit {

  @Input()
  public stock: any;

  @Input()
  public ticker: any;

  public errors: string[]

  numberOfShares: Number
	pricePerShare:  Number
	filled:         string
  positionType:   string
  notes:          string

  constructor(
    private service: StocksService,
    private router: Router,
    private location: Location,
    private datePipe: DatePipe
  ) { }

  ngOnInit() {
    this.filled = Date()
    this.filled = this.datePipe.transform(this.filled, 'yyyy-MM-dd');
  }

  getStock(id:string){
    this.service.getStock(id).subscribe( result => {
      this.stock = result
    })
  }

  delete() {

    if (confirm("are you sure you want to delete this stock?"))
    {
      this.errors = null

      this.service.deleteStocks(this.stock.id).subscribe(r => {
        this.router.navigateByUrl('/dashboard')
      })
    }
  }

  record() {

    this.errors = null;

    var op = {
      ticker: this.stock.ticker,
      numberOfShares: this.numberOfShares,
      price: this.pricePerShare,
      date: this.filled
    }

    if (this.positionType == 'buy') this.recordBuy(op)
    if (this.positionType == 'sell') this.recordSell(op)
  }

  back() {
    this.location.back()
  }

  recordBuy(stock: object) {
    this.service.purchase(stock).subscribe( r => {
      this.getStock(this.stock.id)
    }, err => {
      this.errors = GetErrors(err)
    })
  }

  recordSell(stock: object) {
    this.service.sell(stock).subscribe( r => {
      this.getStock(this.stock.id)
    }, err => {
      this.errors = GetErrors(err)
    })
  }

}
