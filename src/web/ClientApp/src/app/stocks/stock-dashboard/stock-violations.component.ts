import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'stock-violations',
  templateUrl: './stock-violations.component.html',
  styleUrls: ['./stock-violations.component.css']
})
export class StockViolationsComponent implements OnInit {

  @Input() violations:string[] = []
  
  constructor() { }

  ngOnInit(): void {
  }

}
