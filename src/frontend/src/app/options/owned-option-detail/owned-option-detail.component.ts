import {Component, OnInit} from '@angular/core';
import {OwnedOption, StocksService} from '../../services/stocks.service';
import {ActivatedRoute, Router} from '@angular/router';
import {DatePipe} from '@angular/common';
import {Title} from '@angular/platform-browser';
import {GetErrors} from 'src/app/services/utils';

@Component({
    selector: 'app-owned-option-detail',
    templateUrl: './owned-option-detail.component.html',
    styleUrls: ['./owned-option-detail.component.css'],
    providers: [DatePipe],
    standalone: false
})
export class OwnedOptionComponent implements OnInit {
    public option: OwnedOption;

    public positionType: string
    public premium: number
    public filled: string
    public numberOfContracts: number

    public errors: string[]

    constructor(
        private service: StocksService,
        private route: ActivatedRoute,
        private router: Router,
        private datePipe: DatePipe,
        private title: Title
    ) {
    }

    ngOnInit() {
        this.filled = Date()
        this.filled = this.datePipe.transform(this.filled, 'yyyy-MM-dd');

        let id = this.route.snapshot.paramMap.get('id');

        this.getOption(id)
    }

    getOption(id: string) {
        this.service.getOption(id).subscribe(result => {
            this.option = result
            this.positionType = this.option.boughtOrSold == 'Bought' ? 'sell' : 'buy'
            this.numberOfContracts = this.option.numberOfContracts
            this.title.setTitle(this.option.ticker + " " + this.option.strikePrice + " " + this.option.optionType + " - Nightingale Trading")
        }, error => {
            this.errors = GetErrors(error)
        })
    }

    record() {

        this.errors = null;

        let opt = {
            ticker: this.option.ticker,
            strikePrice: this.option.strikePrice,
            optionType: this.option.optionType,
            expirationDate: this.option.expirationDate,
            numberOfContracts: this.numberOfContracts,
            premium: this.premium,
            filled: this.filled
        }

        if (this.positionType == 'buy') this.recordBuy(opt)
        if (this.positionType == 'sell') this.recordSell(opt)
    }

    delete() {

        if (confirm("are you sure you want to delete this option?")) {
            this.errors = null;

            this.service.deleteOption(this.option.id).subscribe(_ => {
                this.router.navigateByUrl('/dashboard')
            }, err => {
                this.errors = GetErrors(err)
            })
        }
    }

    recordBuy(opt: object) {
        this.service.buyOption(opt).subscribe(r => {
            this.getOption(r.id)
        }, err => {
            this.errors = GetErrors(err)
        })
    }

    recordSell(opt: object) {
        this.service.sellOption(opt).subscribe(r => {
            this.getOption(r.id)
        }, err => {
            this.errors = GetErrors(err)
        })
    }

    expire(assigned: boolean) {

        let service = this.service
        let func = assigned ? optId => service.assignOption(optId) : optId => service.expireOption(optId)

        if (assigned) {
            if (!confirm("Are you sure you want to mark this as assigned?")) {
                return;
            }
        } else {
            if (!confirm("Are you sure you want to mark this as expired?")) {
                return;
            }
        }

        func(this.option.id).subscribe(_ => {
            this.router.navigateByUrl('/options')
        }, err => {
            this.errors = GetErrors(err)
        })
    }

}
