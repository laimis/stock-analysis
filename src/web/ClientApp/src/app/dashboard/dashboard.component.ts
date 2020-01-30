import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { StocksService } from '../services/stocks.service';

import { Observable, Subject } from 'rxjs';

import {
   debounceTime, distinctUntilChanged, switchMap, tap
 } from 'rxjs/operators';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {

	public owned : object[];
  public openOptions : object[];
  public loaded : boolean = false;
  public resultCount: number = 0

  public searchResults$: Observable<object[]>;
  private searchTerms = new Subject<string>();

	constructor(
		private stocks : StocksService,
		private router : Router)
	{ }

	ngOnInit() {

		this.stocks.getPortfolio().subscribe(result => {
      this.owned = result.owned;
      this.openOptions = result.openOptions;
			this.loaded = true;
		}, error => {
			console.log(error);
			this.loaded = false;
    })

    this.searchResults$ = this.searchTerms.pipe(
      // wait 300ms after each keystroke before considering the term
      debounceTime(300),

      // ignore new term if same as previous term
      distinctUntilChanged(),

      // switch to new search observable each time the term changes
      switchMap((term: string) => this.stocks.search(term)),

      tap(r => this.reportResults(r))
    );
	}

  reportResults(arr:object[]) {
    this.resultCount = arr.length
  }

  search(term:string) {
    console.log("search: " + term + ", prev result count " + this.resultCount)
    this.searchTerms.next(term);
  }

  clicked(ticker:string) {
    this.router.navigateByUrl('/stocks/' + ticker)
  }

  loseFocus() {
    this.searchTerms.next('')
  }

}
