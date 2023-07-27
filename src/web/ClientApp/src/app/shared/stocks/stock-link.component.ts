import { Component, Input } from "@angular/core";


@Component({
  selector: 'app-stock-link',
  templateUrl: './stock-link.component.html'
})
export class StockLinkComponent {
  
  @Input()
  public ticker : string;

  @Input()
  public openInNewTab : boolean = false;
}

