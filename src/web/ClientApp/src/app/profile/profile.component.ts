import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {

  profile:object

  importProgress: string = ''

  showDelete: boolean = false

  deleteFeedback: string = ''

  constructor(private service:StocksService, private router:Router) { }

  ngOnInit() {
    this.service.getProfile().subscribe(p => {
      this.profile = p
    })
  }

  deleteInitial() {
    this.showDelete = true
  }

  undoDelete() {
    this.showDelete = false
  }

  deleteFinal() {
    this.service.deleteAccount({feedback:this.deleteFeedback})
      .subscribe(s => this.router.navigate(['/landing']))
  }

  markProgress(msg:string) {
    this.importProgress = msg
  }

  importShares($event) {

    this.markProgress('Importing shares')

    let formData: FormData = this.getFormData($event);

    this.service.importStocks(formData).subscribe(
      s => {
        console.log("success uploading " + s)
        this.markProgress('Shares imported successfully')
      },
      e => {
        console.log("failed: " + e);
        this.markProgress('Failed to import shares')
      })
  }

  importOptions($event) {

    this.markProgress('Importing options')

    let formData: FormData = this.getFormData($event);

    this.service.importOptions(formData).subscribe(
      s => {
        console.log("success uploading " + s)
        this.markProgress('Options imported')
      },
      e => {
        console.log("failed: " + e);
        this.markProgress('Options import failed')
      })
  }

  importNotes($event) {

    this.markProgress('Importing notes')

    let formData: FormData = this.getFormData($event);

    this.service.importNotes(formData).subscribe(
      s => {
        console.log("success uploading " + s)
        this.markProgress('Notes imported')
      },
      e => {
        console.log("failed: " + e);
        this.markProgress('Note import failed')
      })
  }

  private getFormData($event: any) {
    var file = $event.target.files[0];
    let formData: FormData = new FormData();
    formData.append("file", file, file.name);
    return formData;
  }
}
