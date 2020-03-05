import { Component, OnInit } from '@angular/core';
import { StocksService } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-admin-email',
  templateUrl: './admin-email.component.html',
  styleUrls: ['./admin-email.component.css']
})
export class AdminEmailComponent implements OnInit {

  to: string
  from: string
  fromName: string
  subject: string
  body: string

  constructor(private stockService : StocksService) { }

  ngOnInit() {
  }

  send() {
    var obj = {
      to: this.to,
      from: this.from,
      fromName: this.fromName,
      subject: this.subject,
      body: this.body
    }

    this.stockService.sendEmail(obj).subscribe(
      _ => console.log("sent!"),
      _ => console.error("failed!"))
  }

}
