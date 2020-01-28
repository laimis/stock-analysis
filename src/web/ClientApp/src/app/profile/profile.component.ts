import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {

  importSharesEnabled: boolean = true
  importSharesInProgress: boolean = false
  importSharesComplete: boolean = false

  constructor(private service:StocksService) { }

  ngOnInit() {}

  onFileSelected($event) {

    this.importSharesEnabled = false
    this.importSharesInProgress = true

    var file = $event.target.files[0]
    let formData: FormData = new FormData();
    formData.append("file", file, file.name);

    this.service.importShares(formData).subscribe(
      s => {
        console.log("success uploading " + s)
        this.importSharesInProgress = false
        this.importSharesComplete = true
      },
      e => console.log("failed: " + e))
  }

}
