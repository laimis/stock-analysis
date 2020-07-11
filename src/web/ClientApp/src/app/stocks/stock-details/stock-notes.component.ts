import { Component, Input } from '@angular/core';
import { NoteList } from '../../services/stocks.service';

@Component({
  selector: 'stock-notes',
  templateUrl: './stock-notes.component.html',
  // styleUrls: ['./stock-notes.component.css']
})

export class StockNotesComponent {

  @Input() ticker: string

  private _notes : NoteList
  @Input()
  set notes(notes: NoteList) {
    this._notes = notes
  }
  get notes(): NoteList { return this._notes; }

	constructor(){}

	ngOnInit(): void {}
}
