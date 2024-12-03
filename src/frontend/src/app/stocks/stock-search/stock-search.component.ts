import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {Observable, Subject} from 'rxjs';
import {debounceTime, switchMap} from 'rxjs/operators';
import {StockSearchResult, StocksService} from "../../services/stocks.service";
import {GetErrors} from "../../services/utils";
import {FormsModule} from "@angular/forms";

@Component({
    selector: 'app-stock-search',
    templateUrl: './stock-search.component.html',
    imports: [
        FormsModule
    ],
    styleUrls: ['./stock-search.component.css']
})
export class StockSearchComponent implements OnInit {

    @Input() label: string = "Search for securities using ticker or name"
    @Input() cssClass: string = "form-control"

    errors: string[] = [];
    @Input() placeholder: string
    @Input() justTickers: boolean = false
    @Output() tickerSelected = new EventEmitter<string>();
    selectedValue: string = null
    loading: boolean = false
    public searchResults: Observable<StockSearchResult[]>;
    searchResultsSubscribedArray: StockSearchResult[] = []
    highlightedIndex = -1;
    private searchTerms = new Subject<string>();

    constructor(
        private stocks: StocksService
    ) {
    }

    @Input()
    set ticker(value: string) {
        this.selectedValue = value
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
            switchMap((term: string) => {
                this.loading = true
                this.errors = []
                return this.stocks.search(term)
            }),
        );

        this.searchResults.subscribe(value => {
            this.loading = false
            console.log("searchResults.subscribe: " + value.length)
            this.searchResultsSubscribedArray = value
        }, error => {
            this.loading = false
            this.searchResultsSubscribedArray = []
            this.errors = GetErrors(error)
            console.log("searchResults.subscribe error: " + this.errors)
        })
    }

    loseFocus() {
        this.searchTerms.next('')
        this.loading = false
        this.errors = []
        if (!this.selectedValue) {
        }
    }

    clicked(ticker: string) {
        console.log("clicked: " + ticker)
        this.selectedValue = "" // once the user clicks, we want to clear the input
        this.tickerSelected.emit(ticker)
    }

    onModelChange($event: string) {
        console.log("onModelChange: " + $event)
        this.searchTerms.next($event)
    }

    onKeyDown($event: KeyboardEvent) {

        if (this.searchResultsSubscribedArray.length > 0) {
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
        } else {
            // no results, but perhaps the user wants to select what they just typed in
            if ($event.key === 'Enter') {
                this.loseFocus()
                this.clicked(this.selectedValue)
            }
        }
    }
}
