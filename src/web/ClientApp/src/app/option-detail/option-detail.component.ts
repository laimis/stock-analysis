import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-option-detail',
  templateUrl: './option-detail.component.html',
  styleUrls: ['./option-detail.component.css']
})
export class OptionDetailComponent implements OnInit {
  ticker: string;

  public options : object
  public loading : boolean = true

  constructor(
    private service: StocksService,
    private route: ActivatedRoute) { }

  ngOnInit() {
    var ticker = this.route.snapshot.paramMap.get('ticker');
    if (ticker) {
      this.ticker = ticker;
    }

    this.service.getOptions(this.ticker).subscribe( result => {
      this.options = result
      this.loading = false
    });
  }

}
