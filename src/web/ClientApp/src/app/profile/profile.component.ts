import { Component, OnInit } from '@angular/core';
import { StocksService, GetErrors } from '../services/stocks.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {

  profile:object

  importErrors: string[]
  importSuccess: string
  importProgress: string

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

  markError(msg) {
    this.importErrors = GetErrors(msg)
    this.importProgress = null
    this.importSuccess = null
  }

  markSuccess(msg) {
    this.importErrors = null
    this.importProgress = null
    this.importSuccess = msg
  }

  markProgress(msg) {
    this.importProgress = msg
    this.importErrors = null
    this.importSuccess = null
  }

  importShares($event) {

    this.markProgress('Importing shares')

    let formData: FormData = this.getFormData($event);

    this.service.importStocks(formData).subscribe(
      _ => this.markSuccess('Shares imported successfully'),
      e => this.markError(e)
    )
  }

  importOptions($event) {

    this.markProgress('Importing options')

    let formData: FormData = this.getFormData($event);

    this.service.importOptions(formData).subscribe(
      _ => this.markSuccess('Options imported'),
      e => this.markError(e)
    )
  }

  importNotes($event) {

    this.markProgress('Importing notes')

    let formData: FormData = this.getFormData($event);

    this.service.importNotes(formData).subscribe(
      _ => {this.markSuccess('Notes imported') },
      e => this.markError(e)
    )
  }

  private getFormData($event: any) {
    var file = $event.target.files[0];
    let formData: FormData = new FormData();
    formData.append("file", file, file.name);
    return formData;
  }
}
