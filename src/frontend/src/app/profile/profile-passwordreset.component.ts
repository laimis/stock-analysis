import { Component, OnInit, inject } from '@angular/core';
import {StocksService} from '../services/stocks.service';
import {ActivatedRoute, Router} from '@angular/router';
import {GetErrors} from '../services/utils';
import { ErrorDisplayComponent } from "../shared/error-display/error-display.component";
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'app-profile-passwordreset',
    templateUrl: './profile-passwordreset.component.html',
    styleUrls: ['./profile-passwordreset.component.css'],
    standalone: true,
    imports: [ErrorDisplayComponent, FormsModule]
})
export class ProfilePasswordResetComponent implements OnInit {
    private stockService = inject(StocksService);
    private route = inject(ActivatedRoute);
    private router = inject(Router);


    public errors: string[]
    public id: string
    public password: string

    ngOnInit() {
        this.id = this.route.snapshot.paramMap.get("id")
    }

    resetPassword() {

        this.errors = null

        var obj = {
            id: this.id,
            password: this.password
        }

        this.stockService.resetPassword(obj).subscribe(r => {
            this.router.navigate(['/dashboard'])
        }, err => {
            this.errors = GetErrors(err)
        })
    }
}
