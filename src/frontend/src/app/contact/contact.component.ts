import { Component, inject } from '@angular/core';
import {StocksService} from '../services/stocks.service';
import {GetErrors} from '../services/utils';

@Component({
    selector: 'app-contact',
    templateUrl: './contact.component.html',
    styleUrls: ['./contact.component.css'],
    standalone: false
})
export class ContactComponent {
    private stockService = inject(StocksService);


    public saved: boolean = false
    public email: string
    public message: string
    public errors: string[]


    sendMessage() {
        var obj = {
            email: this.email,
            message: this.message
        }

        this.stockService.sendMessage(obj).subscribe(
            _ => this.saved = true, err => this.errors = GetErrors(err))
    }
}
