import { Component, OnInit } from '@angular/core';
import { StocksService, NoteList } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-notes',
  templateUrl: './notes.component.html',
  styleUrls: ['./notes.component.css']
})
export class NotesComponent implements OnInit {

  public notes: object[]
  public tickerFilter: string

  constructor(
    private stockService:StocksService,
    private route:ActivatedRoute) { }

  ngOnInit() {
    this.tickerFilter = this.route.snapshot.paramMap.get('ticker')
    this.loadNotes(this.tickerFilter)
  }

  private loadNotes(ticker:string) {
    this.stockService.getNotes(ticker).subscribe((r: NoteList) => {
      this.notes = r.notes
    })
  }
}
