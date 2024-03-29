import {Component, Input} from "@angular/core";
import {SECFiling} from "src/app/services/stocks.service";

@Component({
    selector: 'app-stock-secfilings',
    templateUrl: './stock-secfilings.component.html',
    styleUrls: ['./stock-secfilings.component.css']
})
export class StockSECFilingsComponent {

    @Input()
    filings: SECFiling[]
}
