import {Component, OnInit} from '@angular/core';
import {CryptoDetails, CryptoOwnership, StocksService} from '../../services/stocks.service';
import {ActivatedRoute} from '@angular/router';
import {Title} from '@angular/platform-browser';

@Component({
    selector: 'app-crypto-details',
    templateUrl: './crypto-details.component.html',
    styleUrls: ['./crypto-details.component.css']
})
export class CryptoDetailsComponent implements OnInit {

    token: string
    loaded: boolean = false
    crypto: CryptoDetails
    ownership: CryptoOwnership

    constructor(
        private stocks: StocksService,
        private route: ActivatedRoute,
        private title: Title) {
    }

    ngOnInit(): void {
        this.route.params.subscribe(param => {
            const token = param['token']
            if (token) {
                this.token = token;
                this.fetchToken();
            }
        })
    }

    fetchToken() {
        this.stocks.getCryptoDetails(this.token).subscribe(result => {
            this.crypto = result;
            this.loaded = true;
            this.title.setTitle(this.crypto.token + " - Nightingale Trading")
        }, error => {
            console.error(error);
            this.loaded = true;
        });

        this.loadOwnership()
    }

    loadOwnership() {
        this.stocks.getCryptoOwnership(this.token).subscribe(result => {
            this.ownership = result
        })
    }

    deleteTransaction(transactionId: string) {
        if (confirm("are you sure you want to delete the transaction?")) {
            this.stocks.deleteCryptoTransaction(this.token, transactionId).subscribe(_ => {
                this.loadOwnership()
            })
        }
    }

    delete() {
        if (confirm("are you sure you want to delete this crypto?")) {
            alert("not implemented")
        }
    }
}
