import {Component, Input} from "@angular/core";
import {charts_getTradingViewLink} from "src/app/services/links.service";
import {NgOptimizedImage} from "@angular/common";


@Component({
    selector: 'app-trading-view-link',
    templateUrl: './trading-view-link.component.html',
    imports: [
        NgOptimizedImage
    ],
    standalone: true
})
export class TradingViewLinkComponent {

    @Input()
    public ticker: string;

    getTradingViewLink(): string {
        return charts_getTradingViewLink(this.ticker);
    }
}

