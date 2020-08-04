import { Component, OnInit, Input } from '@angular/core';


@Component({
  selector: 'stock-past',
  templateUrl: './stock-past.component.html',
  styleUrls: ['./stock-past.component.css']
})
export class StockPastComponent implements OnInit {

	ngOnInit() {
  }

  @Input()
  past: any
}
