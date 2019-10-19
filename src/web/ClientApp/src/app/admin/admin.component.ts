import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';

@Component({
  selector: 'app-admin',
  templateUrl: './admin.component.html',
  styleUrls: ['./admin.component.css']
})
export class AdminComponent implements OnInit {

  public users: object
  public loaded: boolean = false

  constructor(
    private stocks : StocksService,
  ) { }

  ngOnInit() {
    this.stocks.getAcounts().subscribe(result => {
      this.users = result;
      this.loaded = true;
		}, error => {
			console.error(error);
			this.loaded = true;
		});
  }

}
