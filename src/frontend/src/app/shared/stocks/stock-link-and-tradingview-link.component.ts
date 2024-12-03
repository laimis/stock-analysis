import {Component, Input} from "@angular/core";
import {TradingViewLinkComponent} from "./trading-view-link.component";
import {StockLinkComponent} from "./stock-link.component";

@Component({
    selector: 'app-stock-link-and-tradingview-link',
    templateUrl: './stock-link-and-tradingview-link.component.html',
    imports: [
        TradingViewLinkComponent,
        StockLinkComponent
    ]
})
export class StockLinkAndTradingviewLinkComponent {

    @Input()
    public ticker: string;

    @Input()
    public openInNewTab: boolean = false;
}

