import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {

  importShareStatus: string = 'ready'
  importOptionStatus: string = 'ready'
  importNoteStatus: string = 'ready'

  constructor(private service:StocksService) { }

  ngOnInit() {}

  importShares($event) {

    this.importShareStatus = 'inprogress'

    let formData: FormData = this.getFormData($event);

    this.service.importStocks(formData).subscribe(
      s => {
        console.log("success uploading " + s)
        this.importShareStatus = 'success'
      },
      e => {
        console.log("failed: " + e);
        this.importShareStatus = 'failed'
      })
  }

  importOptions($event) {

    this.importOptionStatus = 'inprogress'

    let formData: FormData = this.getFormData($event);

    this.service.importOptions(formData).subscribe(
      s => {
        console.log("success uploading " + s)
        this.importOptionStatus = 'success'
      },
      e => {
        console.log("failed: " + e);
        this.importOptionStatus = 'failed'
      })
  }

  importNotes($event) {

    this.importNoteStatus = 'inprogress'

    let formData: FormData = this.getFormData($event);

    this.service.importNotes(formData).subscribe(
      s => {
        console.log("success uploading " + s)
        this.importNoteStatus = 'success'
      },
      e => {
        console.log("failed: " + e);
        this.importNoteStatus = 'failed'
      })
  }

  private getFormData($event: any) {
    var file = $event.target.files[0];
    let formData: FormData = new FormData();
    formData.append("file", file, file.name);
    return formData;
  }
}
