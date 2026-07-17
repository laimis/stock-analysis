import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import {StocksService} from '../services/stocks.service';
import {ActivatedRoute, Router} from '@angular/router';
import {GetErrors} from '../services/utils';
import { ErrorDisplayComponent } from "../shared/error-display/error-display.component";

@Component({
    selector: 'app-profile-verify',
    templateUrl: './profile-verify.component.html',
    styleUrls: ['./profile-verify.component.css'],
    standalone: true,
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [ErrorDisplayComponent],
    
})
export class ProfileVerifyComponent implements OnInit {
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

        const obj = {
            id: this.id,
            password: this.password
        }

        this.stockService.resetPassword(obj).subscribe(() => {
            this.router.navigate(['/dashboard'])
        }, err => {
            this.errors = GetErrors(err)
        })
    }
}
