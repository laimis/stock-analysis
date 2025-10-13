import { CurrencyPipe, DatePipe, NgClass } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import {Chain, StocksService} from 'src/app/services/stocks.service';

@Component({
    selector: 'app-failuresuccesschain',
    templateUrl: './failuresuccesschain.component.html',
    styleUrls: ['./failuresuccesschain.component.css'],
    imports: [NgClass, CurrencyPipe, DatePipe]
})
export class FailuresuccesschainComponent implements OnInit {
    private service = inject(StocksService);

    chain: Chain;
    render: string = "all";

    toggle(identifier: string) {
        this.render = identifier
    }

    ngOnInit(): void {
        this.service.chainReport().subscribe(result => {
            this.chain = result
        }, error => {
            console.log("failed: " + error);
        })
    }

    showAllLinks() {
        this.render = "all"
    }

    showSuccessLinks() {
        this.render = "success"
    }

    showFailureLinks() {
        this.render = "failure"
    }

}
