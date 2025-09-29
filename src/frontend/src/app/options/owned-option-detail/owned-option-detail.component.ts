import {Component, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import { DatePipe } from '@angular/common';
import {Title} from '@angular/platform-browser';
import {GetErrors} from 'src/app/services/utils';
import {BrokerageOptionOrder, OptionPosition, OptionService} from "../../services/option.service";
import {ErrorDisplayComponent} from "../../shared/error-display/error-display.component";
import {OptionPositionComponent} from "../option-position/option-position.component";
import {BrokerageService} from "../../services/brokerage.service";
import {concat} from "rxjs";

@Component({
    selector: 'app-owned-option-detail',
    templateUrl: './owned-option-detail.component.html',
    styleUrls: ['./owned-option-detail.component.css'],
    imports: [
    ErrorDisplayComponent,
    OptionPositionComponent
],
    providers: [DatePipe]
})
export class OwnedOptionComponent implements OnInit {
    public position: OptionPosition;
    public orders: BrokerageOptionOrder[];

    public errors: string[]
    public filled: string

    constructor(
        private optionService: OptionService,
        private brokerageService: BrokerageService,
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
        this.optionService.get(id).subscribe(result => {
            this.position = result
            this.title.setTitle(this.position.underlyingTicker + " option position - Nightingale Trading")
        }, error => {
            this.errors = GetErrors(error)
        });
        
        this.brokerageService.brokerageAccount().subscribe(result => {
            this.orders = result.optionOrders
        }, error => {
            this.errors = GetErrors(error)
        })
    }

    delete() {

        if (confirm("are you sure you want to delete this option?")) {
            this.errors = null;

            this.optionService.delete(this.position.positionId).subscribe(_ => {
                this.router.navigateByUrl('/dashboard')
            }, err => {
                this.errors = GetErrors(err)
            })
        }
    }
}
