import {Component, OnInit} from '@angular/core';
import {AccountTransaction, StocksService} from "../services/stocks.service";
import {ParsedDatePipe} from "../services/parsedDate.filter";
import {CurrencyPipe, NgForOf, NgIf} from "@angular/common";
import {GetErrors} from "../services/utils";
import {ErrorDisplayComponent} from "../shared/error-display/error-display.component";

@Component({
  selector: 'app-account-transactions',
  standalone: true,
    imports: [
        ParsedDatePipe,
        CurrencyPipe,
        NgForOf,
        NgIf,
        ErrorDisplayComponent
    ],
  templateUrl: './account-transactions.component.html',
  styleUrl: './account-transactions.component.css'
})
export class AccountTransactionsComponent implements OnInit {
    transactions: AccountTransaction[] = [];

    constructor(private stocksService: StocksService) {}
    errors = [];

    ngOnInit(): void {
        this.loadTransactions();
    }

    loadTransactions(): void {
        this.stocksService.getAccountTransactions().subscribe(
            r => {
                this.transactions = r;
            },
            error => {
                this.errors = GetErrors(error);
            }
        );
    }

    markAsApplied(transactionId: string): void {
        this.stocksService.markTransactionAsApplied(transactionId).subscribe(
            () => {
                this.loadTransactions(); // Reload the transactions after marking as applied
            },
            error => {
                console.error('Error marking transaction as applied:', error);
            }
        );
    }
}
