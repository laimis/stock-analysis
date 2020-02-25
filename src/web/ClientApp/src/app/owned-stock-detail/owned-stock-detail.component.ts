import { Component, OnInit } from '@angular/core';
import { StocksService, OptionDefinition, GetErrors } from '../services/stocks.service';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe, Location } from '@angular/common';

@Component({
  selector: 'app-owned-stock-detail',
  templateUrl: './owned-stock-detail.component.html',
  styleUrls: ['./owned-stock-detail.component.css'],
  providers: [DatePipe]
})
export class OwnedStockDetailComponent implements OnInit {
  public stock: any;

  public errors: string[]

  numberOfShares: Number
	pricePerShare:  Number
	filled:         string
  positionType:   string
  notes:          string

  constructor(
    private service: StocksService,
    private route: ActivatedRoute,
    private router: Router,
    private location: Location,
    private datePipe: DatePipe
  ) { }

  ngOnInit() {
    this.filled = Date()
    this.filled = this.datePipe.transform(this.filled, 'yyyy-MM-dd');

    var id = this.route.snapshot.paramMap.get('id');

    this.getStock(id)
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
