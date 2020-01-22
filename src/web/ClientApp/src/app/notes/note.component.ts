import { Component, OnInit } from '@angular/core';
import { StocksService, GetErrors } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-note',
  templateUrl: './note.component.html',
  styleUrls: ['./note.component.css']
})
export class NoteComponent implements OnInit {

  public note: any
  public saved:boolean
  public errors: string[];

  constructor(
    private stockService:StocksService,
    private route:ActivatedRoute) { }

  ngOnInit() {
    var id = this.route.snapshot.paramMap.get('id');
    if (id){
      this.loadNote(id)
    }
  }

  private loadNote(id:string) {
    this.stockService.getNote(id).subscribe(r => {
      this.note = r
    })
  }

  saveNote() {
    this.saved = false;
    this.errors = null;

    this.stockService.saveNote(this.note).subscribe(_ => {
      this.saved = true
    }, err => this.errors = GetErrors(err))
  }

  archiveNote() {
    this.errors = null;

    this.stockService.archiveNote(this.note).subscribe(_ => {
      this.loadNote(this.note.id)
    }, err => this.errors = GetErrors(err))
  }

  saveReminder() {
    this.errors = null;

    this.stockService.saveReminder(this.note).subscribe(_ => {
      this.loadNote(this.note.id)
    }, err => this.errors = GetErrors(err))
  }

  clearReminder() {
    this.errors = null;

    var obj = {
      id : this.note.id
    }

    this.stockService.saveReminder(obj).subscribe(_ => {
      this.loadNote(this.note.id)
    }, err => this.errors = GetErrors(err))
  }
}
