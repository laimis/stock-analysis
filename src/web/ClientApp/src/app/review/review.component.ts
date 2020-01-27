import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';

@Component({
  selector: 'app-review',
  templateUrl: './review.component.html',
  styleUrls: ['./review.component.css']
})
export class ReviewComponent implements OnInit {
  public entries: object[]

  constructor(private stockService:StocksService) { }

  ngOnInit() {
    this.loadEntries()
  }

  private loadEntries() {
    this.stockService.getReviewEntires().subscribe((r: object[]) => {
      this.entries = r
    })
  }
}

