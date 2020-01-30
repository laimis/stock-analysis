import { Component, OnInit } from '@angular/core';
import { StocksService, OptionDefinition, GetErrors } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';
import { DatePipe } from '@angular/common';
import { Location } from '@angular/common';

@Component({
  selector: 'app-sold-option-detail',
  templateUrl: './sold-option-detail.component.html',
  styleUrls: ['./sold-option-detail.component.css'],
  providers: [DatePipe]
})
export class SoldOptionDetailComponent implements OnInit {
  public option: OptionDefinition;
  public closed: boolean;
  public closePrice: number;
  public closeDate: string;
  public numberOfContracts: number;
  public errors: string[];

  constructor(
    private service: StocksService,
    private route: ActivatedRoute,
    private location: Location,
    private datePipe: DatePipe
  ) { }

  ngOnInit() {
    this.closeDate = Date()
    this.closeDate = this.datePipe.transform(this.closeDate, 'yyyy-MM-dd');

    var id = this.route.snapshot.paramMap.get('id');

    this.getOption(id)
  }

  getOption(id:string){
    this.service.getOption(id).subscribe( result => {
      this.option = result
    })
  }

  close() {

    this.errors = null;

    var obj = {
      id: this.option.id,
      closePrice: this.closePrice,
      closeDate: this.closeDate,
      numberOfContracts: this.numberOfContracts
    }

    this.service.closeOption(obj).subscribe( () => {
      this.closed = true
      this.closePrice = null
      this.numberOfContracts = null
      this.getOption(this.option.id)
    }, err => {
      this.errors = GetErrors(err)
    })
  }

  back() {
    this.location.back()
  }

}
