import { Component, OnInit } from '@angular/core';
import { StocksService, NoteList, Note } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-notes',
  templateUrl: './notes.component.html',
  styleUrls: ['./notes.component.css']
})
export class NotesComponent implements OnInit {

  notes: Note[]
  tickers: string[]
  symbolFilter: string = ''
  loading: boolean = false

  constructor(
    private stockService:StocksService,
    private route:ActivatedRoute) { }

  ngOnInit() {
    var filter = this.route.snapshot.paramMap.get('ticker')
    if (filter)
    {
      this.symbolFilter = filter
    }
    this.loadData()
  }

  loadData() {
    this.loading = true

    console.log("loading")

    this.stockService.getNotes(this.symbolFilter).subscribe((r: NoteList) => {
      this.loading = false;
      this.tickers = r.tickers
      this.notes = r.notes
      console.log("loaded")
    }, _ => {
      console.log("failed to load")
      this.loading = false;
    })
  }
}
