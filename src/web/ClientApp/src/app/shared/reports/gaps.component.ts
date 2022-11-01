import { Component, Input, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { StockGap, StockGaps } from '../../services/stocks.service';

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
