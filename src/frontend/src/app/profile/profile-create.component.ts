import { Component, OnInit, inject } from '@angular/core';
import {StocksService} from '../services/stocks.service';
import {ActivatedRoute, Router} from '@angular/router';
import {Location, NgClass} from '@angular/common';
import {GetErrors} from '../services/utils';
import { FormsModule } from '@angular/forms';
import { ErrorDisplayComponent } from "../shared/error-display/error-display.component";

@Component({
    selector: 'app-profile-create',
    templateUrl: './profile-create.component.html',
    styleUrls: ['./profile-create.component.css'],
    imports: [FormsModule, NgClass, ErrorDisplayComponent],
    standalone: true
})
export class ProfileCreateComponent implements OnInit {
    private stockService = inject(StocksService);
    private router = inject(Router);
    private location = inject(Location);
    private route = inject(ActivatedRoute);


    firstname: string
    lastname: string
    email: string
    password: string
    terms: boolean

    errors: string[]

    disableButton: boolean = false
    isPremium: boolean = false
    payLabel: string = ''
    private FREE_PLAN: string = 'FREE'
    planName: string = this.FREE_PLAN
    private PLUS_PLAN: string = 'PLUS'
    private FULL_PLAN: string = 'FULL'

    ngOnInit() {
        var plan = this.route.snapshot.paramMap.get("plan")
        if (plan) {
            if (plan == 'plus') this.planName = this.PLUS_PLAN
            if (plan == 'full') this.planName = this.FULL_PLAN

            this.isPremium = true;
            this.payLabel = " & Pay"
        }
    }

    validate() {

        this.errors = null
        this.disableButton = true

        var obj = this.createUserData()

        this.stockService.validateAccount(obj).subscribe(r => {
            if (this.planName == this.PLUS_PLAN) {
                this.payPlus()
            } else if (this.planName == this.FULL_PLAN) {
                this.payFull()
            } else {
                this.createAccount(obj, null)
            }
        }, err => {
            this.disableButton = false
            this.errors = GetErrors(err)
        })
    }

    createAccount(userData, paymentToken) {
        var obj = {
            userInfo: userData,
            paymentInfo: paymentToken
        }

        this.stockService.createAccount(obj).subscribe(_ => {
            this.router.navigate(['/dashboard'])
            this.disableButton = false
        }, err => {
            this.errors = GetErrors(err)
            this.disableButton = false
        })
    }

    back() {
        this.location.back()
    }

    payFull() {
        this.pay("plan_GmXCy9dpWKIB4E", "Plan level: Full")
    }

    payPlus() {
        this.pay("plan_GmXCWQvmEWwhtr", "Plan level: Plus")
    }

    pay(planId: string, planName: string) {

        var capture = this;

        var handler = (<any>window).StripeCheckout.configure({
            key: 'pk_live_dls5NvmF6iwb4W19DlqsYvYR0006lBNU20',
            locale: 'auto',
            token: function (token: any) {
                console.log(token)

                var paymentData = {
                    token: token,
                    planId: planId
                }

                capture.createAccount(
                    capture.createUserData(),
                    paymentData)
            }
        });

        handler.open({
            name: 'Nightingale Trading',
            description: planName
        });

        this.disableButton = false
    }

    private createUserData() {
        return {
            firstname: this.firstname,
            lastname: this.lastname,
            email: this.email,
            password: this.password,
            terms: this.terms
        };
    }
}
