import {Component, Input} from "@angular/core";


@Component({
    selector: 'app-stock-link-and-tradingview-link',
    templateUrl: './stock-link-and-tradingview-link.component.html'
})
export class StockLinkAndTradingviewLinkComponent {

    @Input()
    public ticker: string;

    @Input()
    public openInNewTab: boolean = false;
}

