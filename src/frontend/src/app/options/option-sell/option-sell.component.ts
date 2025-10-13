import { Component, OnInit, inject } from '@angular/core';
import {DatePipe, Location, PercentPipe} from '@angular/common';
import {ActivatedRoute, Router} from '@angular/router';
import {GetErrors} from 'src/app/services/utils';
import {tick} from "@angular/core/testing";
import { OptionService } from 'src/app/services/option.service';
import { FormsModule } from '@angular/forms';
import { StockSearchComponent } from "src/app/stocks/stock-search/stock-search.component";
import { ErrorDisplayComponent } from "src/app/shared/error-display/error-display.component";

@Component({
    selector: 'app-option-sell',
    templateUrl: './option-sell.component.html',
    providers: [DatePipe],
    imports: [FormsModule, StockSearchComponent, ErrorDisplayComponent],
    standalone: true
})
export class OptionSellComponent implements OnInit {
    private service = inject(OptionService);
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    private datePipe = inject(DatePipe);
    private location = inject(Location);


    errors: string[]

    success: boolean

    ticker: string
    strikePrice: number
    optionType: string
    expirationDate: string
    positionType: string
    numberOfContracts: number
    premium: number
    filled: string
    notes: string
    protected readonly tick = tick;


    ngOnInit() {
        var ticker = this.route.snapshot.paramMap.get('ticker');
        if (ticker) {
            this.ticker = ticker;
        }

        this.filled = Date()
        this.filled = this.datePipe.transform(this.filled, 'yyyy-MM-dd');
        this.positionType = 'buy'
    }

    record() {
        var opt = {
            ticker: this.ticker,
            strikePrice: this.strikePrice,
            optionType: this.optionType,
            expirationDate: this.expirationDate,
            numberOfContracts: this.numberOfContracts,
            premium: this.premium,
            filled: this.filled,
            notes: this.notes
        }

        if (this.positionType == 'buy') this.recordBuy(opt)
        if (this.positionType == 'sell') this.recordSell(opt)
    }

    recordBuy(opt: object) {
        this.service.buyOption(opt).subscribe(r => {
            this.navigateToOption(r.id)
        }, err => {
            this.errors = GetErrors(err)
        })
    }

    recordSell(opt: object) {
        this.service.sellOption(opt).subscribe(r => {
            this.navigateToOption(r.id)
        }, err => {
            this.errors = GetErrors(err)
        })
    }

    navigateToOption(id: string) {
        this.router.navigate(['/optiondetails', id])
    }

    back() {
        this.location.back()
    }

    onTickerSelected(ticker: string) {
        this.ticker = ticker;
    }
}
