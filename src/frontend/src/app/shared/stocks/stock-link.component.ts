import {Component, Input, ChangeDetectionStrategy} from "@angular/core";
import {RouterLink} from "@angular/router";

@Component({
    selector: 'app-stock-link',
    templateUrl: './stock-link.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [
        RouterLink
    ]
})
export class StockLinkComponent {

    @Input()
    public ticker: string;

    @Input()
    public openInNewTab: boolean = false;
}

