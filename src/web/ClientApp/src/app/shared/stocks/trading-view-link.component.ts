import { Component, Input } from "@angular/core";
import { charts_getTradingViewLink } from "src/app/services/links.service";


@Component({
  selector: 'app-trading-view-link',
  templateUrl: './trading-view-link.component.html'
})
export class TradingViewLinkComponent {
  
  @Input()
  public ticker : string;

  getTradingViewLink() : string {
    return charts_getTradingViewLink(this.ticker);
  }
}

