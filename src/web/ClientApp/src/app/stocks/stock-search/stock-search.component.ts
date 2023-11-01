import {Component, OnInit, Input, Output, EventEmitter} from '@angular/core';
import {Subject, Observable} from 'rxjs';
import {debounceTime, switchMap} from 'rxjs/operators';
import {StockSearchResult, StocksService} from "../../services/stocks.service";

@Component({
  selector: 'app-stock-search',
  templateUrl: './stock-search.component.html',
  styleUrls: ['./stock-search.component.css']
})
export class StockSearchComponent implements OnInit {

  @Input() label: string = "Search for securities using ticker or name"
  @Input() cssClass: string = "form-control"

  @Input()
  set ticker(value: string) {
    this.selectedValue = value
  }

  @Input() placeholder: string
  @Output() tickerSelected = new EventEmitter<string>();

  selectedValue: string = null

  public searchResults: Observable<StockSearchResult[]>;
  private searchTerms = new Subject<string>();
  searchResultsSubscribedArray: StockSearchResult[] = []
  highlightedIndex = -1;

  constructor(
    private stocks: StocksService
  ) {
  }

  ngOnInit() {

    this.searchResults = this.searchTerms.pipe(
      // wait 300ms after each keystroke before considering the term
      debounceTime(300),

      // ignore new term if same as previous term -- commented this one out because
      // if a user went back and used delete to go back to their previous query, this
      // filter would consider it duplicate and not pass it on
      // distinctUntilChanged(),

      // switch to new search observable each time the term changes
      switchMap((term: string) => this.stocks.search(term)),
    );

    this.searchResults.subscribe(value => {
      console.log("searchResults.subscribe: " + value.length)
      this.searchResultsSubscribedArray = value
    })
  }

  loseFocus() {
    this.searchTerms.next('')
    if (!this.selectedValue) {
    }
  }

  clicked(ticker: string) {
    console.log("clicked: " + ticker)
    this.selectedValue = ticker
    this.tickerSelected.emit(ticker)
  }

  onModelChange($event: string) {
    console.log("onModelChange: " + $event)
    this.searchTerms.next($event)
  }

  onKeyDown($event: KeyboardEvent) {

    if (this.searchResultsSubscribedArray.length == 0) {
      return
    }

    if ($event.key === 'ArrowUp' && this.highlightedIndex > 0) {
      this.highlightedIndex--;
    }

    if ($event.key === 'ArrowDown' && this.highlightedIndex < this.searchResultsSubscribedArray.length - 1) {
      this.highlightedIndex++;
    }

    if ($event.key === 'Enter' && this.highlightedIndex >= 0) {
      let ticker = this.searchResultsSubscribedArray[this.highlightedIndex].symbol
      this.loseFocus()
      this.clicked(ticker)
    }
  }
}
