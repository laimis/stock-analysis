import {Component, OnInit} from '@angular/core';
import {AccountTransaction, StocksService} from "../services/stocks.service";
import {ParsedDatePipe} from "../services/parsedDate.filter";
import {CurrencyPipe, NgClass, NgForOf, NgIf} from "@angular/common";
import {GetErrors} from "../services/utils";
import {ErrorDisplayComponent} from "../shared/error-display/error-display.component";

@Component({
    selector: 'app-account-transactions',
    imports: [
        ParsedDatePipe,
        CurrencyPipe,
        NgForOf,
        NgIf,
        ErrorDisplayComponent,
        NgClass
    ],
    templateUrl: './account-transactions.component.html',
    styleUrl: './account-transactions.component.css'
})
export class AccountTransactionsComponent implements OnInit {
    transactions: AccountTransaction[] = [];
    sortColumn: keyof AccountTransaction = 'tradeDate';
    sortDirection: 'asc' | 'desc' = 'desc';
    errors = [];

    constructor(private stocksService: StocksService) {}
    
    ngOnInit(): void {
        this.loadTransactions();
    }

    loadTransactions(): void {
        this.stocksService.getAccountTransactions().subscribe(
            r => {
                this.transactions = r;
                this.sortTransactions();
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

    sortTransactions(): void {
        this.transactions.sort((a, b) => {
            const aValue = a[this.sortColumn];
            const bValue = b[this.sortColumn];

            if (aValue < bValue) return this.sortDirection === 'asc' ? -1 : 1;
            if (aValue > bValue) return this.sortDirection === 'asc' ? 1 : -1;
            return 0;
        });
    }

    changeSort(column: keyof AccountTransaction): void {
        if (this.sortColumn === column) {
            this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
        } else {
            this.sortColumn = column;
            this.sortDirection = 'asc';
        }
        this.sortTransactions();
    }
}
