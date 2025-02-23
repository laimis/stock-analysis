import {Component, OnInit} from '@angular/core';
import {AccountStatus, KeyValuePair, StocksService} from '../services/stocks.service';
import {Router} from '@angular/router';
import {GetErrors} from '../services/utils';
import {GlobalService} from "../services/global.service";
import {max} from "rxjs/operators";
import {OptionService} from "../services/option.service";

@Component({
    selector: 'app-profile',
    templateUrl: './profile.component.html',
    styleUrls: ['./profile.component.css'],
    standalone: false
})
export class ProfileComponent implements OnInit {

    profile: AccountStatus

    importErrors: string[]
    importSuccess: string
    importProgress: string

    showDelete: boolean = false

    deleteFeedback: string = ''

    errors: string[]
    protected readonly max = max;

    constructor(
        private global: GlobalService,
        private service: StocksService,
        private optionService: OptionService,
        private router: Router) {
    }

    ngOnInit() {
        this.global.accountStatusFeed.subscribe(s => {
            console.log("profile component")
            this.profile = s
        })
    }

    deleteInitial() {
        this.showDelete = true
    }

    clearData() {
        this.service.clearAccount()
            .subscribe(s => this.router.navigate(['/']))
    }

    undoDelete() {
        this.showDelete = false
    }

    updateMaxLoss(value: string) {
        this.errors = null
        let keyValue: KeyValuePair = {key: "maxLoss", value: value}
        this.service.updateAccountSettings(keyValue).subscribe(
            s => this.profile.maxLoss = s.maxLoss,
            error => this.errors = GetErrors(error)
        )
    }

    deleteFinal() {
        this.service.deleteAccount({feedback: this.deleteFeedback})
            .subscribe(s => this.router.navigate(['/landing']))
    }

    markError(msg) {
        this.importErrors = GetErrors(msg)
        this.importProgress = null
        this.importSuccess = null
    }

    markSuccess(msg) {
        this.importErrors = null
        this.importProgress = null
        this.importSuccess = msg
    }

    markProgress(msg) {
        this.importProgress = msg
        this.importErrors = null
        this.importSuccess = null
    }

    importShares($event) {

        this.markProgress('Importing shares')

        let formData: FormData = this.getFormData($event);

        this.service.importStocks(formData).subscribe(
            _ => this.markSuccess('Shares imported successfully'),
            e => this.markError(e)
        )
    }

    importOptions($event) {

        this.markProgress('Importing options')

        let formData: FormData = this.getFormData($event);

        this.optionService.importOptions(formData).subscribe(
            _ => this.markSuccess('Options imported'),
            e => this.markError(e)
        )
    }

    importTransactions($event) {

        this.markProgress('Importing transactions')

        let formData: FormData = this.getFormData($event);

        this.service.importTransactions(formData).subscribe(
            _ => {
                this.markSuccess('Transactions imported')
            },
            e => this.markError(e)
        )
    }

    importCrypto($event) {

        this.markProgress('Importing crypto')

        let formData: FormData = this.getFormData($event);

        this.service.importCrypto(formData).subscribe(
            _ => {
                this.markSuccess('Crypto imported')
            },
            e => this.markError(e)
        )
    }

    private getFormData($event: any) {
        var file = $event.target.files[0];
        let formData: FormData = new FormData();
        formData.append("file", file, file.name);
        return formData;
    }
}
