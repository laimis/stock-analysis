import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';

@Component({
  selector: 'app-notes',
  templateUrl: './notes.component.html',
  styleUrls: ['./notes.component.css']
})
export class NotesComponent implements OnInit {

  public notes: object[]

  constructor(private stockService:StocksService) { }

  ngOnInit() {
    this.loadNotes()
  }

  private loadNotes() {
    this.stockService.getNotes().subscribe(r => {
      this.notes = r.notes
    })
  }
}
