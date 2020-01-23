import { Component, OnInit } from '@angular/core';
import { StocksService, GetErrors } from '../services/stocks.service';
import { ActivatedRoute, Router } from '@angular/router';

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
    private route: ActivatedRoute,
    private router: Router) { }

  ngOnInit() {
    var ticker = this.route.snapshot.paramMap.get('ticker');
    if (ticker){
      this.ticker = ticker;
    }
  }

  addNote() {

    this.saved = false;
    this.errors = null;

    var obj = {
      ticker: this.ticker,
      note: this.note
    }

    this.stockService.addNote(obj).subscribe(
      r => this.router.navigate(['/notes', r.id]),
      err => this.errors = GetErrors(err)
    )
  }
}
