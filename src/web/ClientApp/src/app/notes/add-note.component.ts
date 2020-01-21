import { Component, OnInit } from '@angular/core';
import { StocksService, GetErrors } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-add-note',
  templateUrl: './add-note.component.html',
  styleUrls: ['./add-note.component.css']
})
export class AddNoteComponent implements OnInit {

  public ticker: string
  public note: string
  public saved:boolean
  public errors: string[];

  constructor(
    private stockService:StocksService,
    private route: ActivatedRoute) { }

  ngOnInit() {
    var ticker = this.route.snapshot.paramMap.get('ticker');
    if (ticker){
      this.ticker = ticker;
    }
  }

  addNote() {

    this.saved = false;
    this.errors = null;

    this.stockService.addNote({
      ticker: this.ticker,
      note: this.note
    }).subscribe(() => {
      this.saved = true
      this.note = null
    }, err => this.errors = GetErrors(err))
  }
}
