import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, tap } from 'rxjs/operators';
import { StocksService } from '../services/stocks.service';

@Component({
  selector: 'app-symbol-search',
  templateUrl: './symbol-search.component.html',
  styleUrls: ['./symbol-search.component.css']
})
export class SymbolSearchComponent implements OnInit {

  @Input() label: string = "Search for securities using ticker or name"
  @Input() cssClass: string = "form-control"
  @Output() tickerSelected = new EventEmitter<string>();

  selectedValue: string = null

  public resultCount: number = 0
  public searchResults$: Observable<object[]>;
  private searchTerms = new Subject<string>();

  constructor(
    private stocks : StocksService
  ) { }

  ngOnInit() {
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

  loseFocus() {
    this.searchTerms.next('')
    if (!this.selectedValue)
    {

    }
  }

  clicked(ticker:string) {
    this.selectedValue = ticker
    this.tickerSelected.emit(ticker)
  }
}
