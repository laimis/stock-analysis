import { Component, OnInit, inject } from '@angular/core';
import {StocksService} from '../services/stocks.service';
import {ActivatedRoute, Router} from '@angular/router';
import {GetErrors} from '../services/utils';

@Component({
    selector: 'app-profile-passwordreset',
    templateUrl: './profile-passwordreset.component.html',
    styleUrls: ['./profile-passwordreset.component.css'],
    standalone: false
})
export class ProfilePasswordResetComponent implements OnInit {
    private stockService = inject(StocksService);
    private route = inject(ActivatedRoute);
    private router = inject(Router);


    public errors: string[]
    public id: string
    public password: string

    /** Inserted by Angular inject() migration for backwards compatibility */
    constructor(...args: unknown[]);

    constructor() {
    }

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
