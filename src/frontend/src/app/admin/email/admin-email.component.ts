import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {StocksService} from 'src/app/services/stocks.service';

@Component({
    selector: 'app-admin-email',
    templateUrl: './admin-email.component.html',
    styleUrls: ['./admin-email.component.css'],
    imports: [FormsModule],
    standalone: true
})
export class AdminEmailComponent {
    private stockService = inject(StocksService);


    to: string
    from: string
    fromName: string
    subject: string
    body: string

    send() {
        var obj = {
            to: this.to,
            from: this.from,
            fromName: this.fromName,
            subject: this.subject,
            htmlBody: this.body,  // Map body to htmlBody for backend
            plainBody: null
        }

        this.stockService.sendEmail(obj).subscribe(
            _ => console.log("sent!"),
            _ => console.error("failed!"))
    }

}
