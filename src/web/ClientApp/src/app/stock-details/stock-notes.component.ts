import { Component, Input } from '@angular/core';

@Component({
  selector: 'stock-notes',
  templateUrl: './stock-notes.component.html',
  // styleUrls: ['./stock-notes.component.css']
})

export class StockNotesComponent {

  @Input() ticker: string

  private _notes : object[]
  @Input()
  set notes(notes: object[]) {
    this._notes = notes
  }
  get notes(): object[] { return this._notes; }

	constructor(){}

	ngOnInit(): void {}
}
