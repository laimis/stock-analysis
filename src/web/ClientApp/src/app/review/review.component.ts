import { Component, OnInit } from '@angular/core';
import { StocksService, TransactionList, ReviewList } from '../services/stocks.service';

@Component({
  selector: 'app-review',
  templateUrl: './review.component.html',
  styleUrls: ['./review.component.css']
})
export class ReviewComponent implements OnInit {
  result: ReviewList
  loaded: boolean = false

  constructor(private stockService:StocksService) { }

  ngOnInit() {
    this.loadEntries()
  }

  private loadEntries() {
    this.stockService.getReviewEntires().subscribe((r: ReviewList) => {
      this.loaded = true
      this.result = r
    }, _ => { this.loaded = true})
  }
}

