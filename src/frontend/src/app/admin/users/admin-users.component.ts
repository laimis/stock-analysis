import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import {StocksService} from 'src/app/services/stocks.service';


@Component({
    selector: 'app-admin-users',
    templateUrl: './admin-users.component.html',
    styleUrls: ['./admin-users.component.css'],
    imports: [RouterLink, DatePipe],
    standalone: true
})
export class AdminUsersComponent implements OnInit {
    private stocks = inject(StocksService);


    public users: any

    ngOnInit() {

        this.stocks.getUsers().subscribe(result => {
            this.users = result;
        }, error => {
            console.log(error);
        })
    }
}
