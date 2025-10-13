import { Component, OnInit, inject } from '@angular/core';
import {StocksService} from 'src/app/services/stocks.service';


@Component({
    selector: 'app-admin-users',
    templateUrl: './admin-users.component.html',
    styleUrls: ['./admin-users.component.css'],
    standalone: false
})
export class AdminUsersComponent implements OnInit {
    private stocks = inject(StocksService);


    public users: any

    /** Inserted by Angular inject() migration for backwards compatibility */
    constructor(...args: unknown[]);

    constructor() {
    }

    ngOnInit() {

        this.stocks.getUsers().subscribe(result => {
            this.users = result;
        }, error => {
            console.log(error);
        })
    }
}
