import { Component, Input } from '@angular/core';
import { StockGaps } from '../../services/stocks.service';

@Component({
  selector: 'app-gaps',
  templateUrl: './gaps.component.html',
  styleUrls: ['./gaps.component.css']
})
export class GapsComponent {
  
  error: string = null;

  @Input()
  gaps: StockGaps
  
}
