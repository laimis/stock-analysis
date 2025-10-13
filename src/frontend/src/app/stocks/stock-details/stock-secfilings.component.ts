import {Component, Input} from "@angular/core";
import {SECFiling} from "../../services/stocks.service";

@Component({
    selector: 'app-stock-secfilings',
    templateUrl: './stock-secfilings.component.html',
    styleUrls: ['./stock-secfilings.component.css'],
    standalone: true
})
export class StockSECFilingsComponent {

    @Input()
    filings: SECFiling[] = [];
}
