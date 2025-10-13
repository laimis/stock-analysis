import { Component, inject } from '@angular/core';
import {StocksService} from '../services/stocks.service';
import {GetErrors} from '../services/utils';
import { ErrorDisplayComponent } from "../shared/error-display/error-display.component";
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'app-contact',
    templateUrl: './contact.component.html',
    styleUrls: ['./contact.component.css'],
    imports: [ErrorDisplayComponent, FormsModule]
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
