import { Component, OnInit } from '@angular/core';
import { StocksService, OptionDefinition, GetErrors } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-sold-option-detail',
  templateUrl: './sold-option-detail.component.html',
  styleUrls: ['./sold-option-detail.component.css'],
  providers: [DatePipe]
})
export class SoldOptionDetailComponent implements OnInit {
  public option: OptionDefinition;
  public loaded: boolean;
  public closed: boolean;
  public closePrice: number;
  public closeDate: string;
  public errors: string[];

  constructor(
    private service: StocksService,
    private route: ActivatedRoute,
    private datePipe: DatePipe
  ) { }

  ngOnInit() {
    this.closeDate = Date()
    this.closeDate = this.datePipe.transform(this.closeDate, 'yyyy-MM-dd');

    var id = this.route.snapshot.paramMap.get('id');

    this.service.getSoldOption(id).subscribe( result => {
      this.option = result
      this.loaded = true
    })
  }

  close() {

    this.errors = null;

    var obj = {
      id: this.option.id,
      closePrice: this.closePrice,
      closeDate: this.closeDate,
      amount: this.option.amount
    }

    this.service.closeSoldOption(obj).subscribe( () => {
      this.closed = true
    }, err => {
      this.errors = GetErrors(err)
    })
  }

}
