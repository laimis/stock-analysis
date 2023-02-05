import { Component } from '@angular/core';
import { StocksService } from '../services/stocks.service';
import { GetErrors } from '../services/utils';

@Component({
  selector: 'app-contact',
  templateUrl: './contact.component.html',
  styleUrls: ['./contact.component.css']
})
export class ContactComponent {

  public saved: boolean = false
  public email: string
  public message: string
  public errors: string[]

  constructor(
    private stockService:StocksService
  ) { }

  sendMessage() {
    var obj = {
      email: this.email,
      message: this.message
    }

    this.stockService.sendMessage(obj).subscribe(
      _ => this.saved = true, err => this.errors = GetErrors(err))
  }
}
