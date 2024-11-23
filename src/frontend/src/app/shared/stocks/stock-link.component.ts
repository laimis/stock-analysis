import {Component, Input} from "@angular/core";
import {RouterLink} from "@angular/router";

@Component({
    selector: 'app-stock-link',
    templateUrl: './stock-link.component.html',
    imports: [
        RouterLink
    ],
    standalone: true
})
export class StockLinkComponent {

    @Input()
    public ticker: string;

    @Input()
    public openInNewTab: boolean = false;
}

