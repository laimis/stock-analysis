import {Component, OnInit} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {ActivatedRoute, RouterLink} from "@angular/router";
import {OptionsContainer, OptionService} from "../../services/option.service";
import {LoadingComponent} from "../../shared/loading/loading.component";
import {ErrorDisplayComponent} from "../../shared/error-display/error-display.component";
import { NgClass } from "@angular/common";
import {OptionPositionsComponent} from "./option-positions.component";
import {OptionBrokeragePositionsComponent} from "./option-brokerage-positions.component";
import {OptionBrokerageOrdersComponent} from "./option-brokerage-orders.component";
import {OptionClosedComponent} from "./option-closed.component";
import {OptionPerformanceComponent} from "./option-performance.component";
import {OptionSpreadBuilderComponent} from "../option-spread-builder/option-spread-builder.component";

@Component({
    selector: 'app-options',
    templateUrl: './option-dashboard.component.html',
    imports: [
    LoadingComponent,
    ErrorDisplayComponent,
    NgClass,
    RouterLink,
    OptionPositionsComponent,
    OptionBrokeragePositionsComponent,
    OptionBrokerageOrdersComponent,
    OptionClosedComponent,
    OptionPerformanceComponent,
    OptionSpreadBuilderComponent
],
    styleUrls: ['./option-dashboard.component.css']
})
export class OptionsComponent implements OnInit {

    optionsContainer: OptionsContainer

    loading: boolean = false;
    loaded: boolean = false;

    activeTab: string = 'open'
    errors: string[] = null
    

    constructor(
        private service: OptionService,
        private title: Title,
        private route: ActivatedRoute
    ) {
    }

    ngOnInit() {
        this.title.setTitle("Options - Nightingale Trading")
        // get the active tab based on the url. The url is in the format /options/:tab
        // use route snapshot to get the current url
        this.route.paramMap.subscribe(params => {
            let tab = params.get('tab')
            if (tab) {
                this.activeTab = tab
            }
        })
        
        this.getOptions()
    }

    getOptions() {
        this.loading = true;
        this.service.getDashboard()
            .subscribe(
                result => {
                    this.optionsContainer = result
                    this.loaded = true
                    this.loading = false
                }, error => {
                    this.errors = error
                    this.loaded = true
                    this.loading = false
                })
    }

    isActive(tabName: string) {
        return tabName == this.activeTab
    }

    activateTab(tabName: string) {
        this.activeTab = tabName
    }

    refreshOptions() {
        this.getOptions()
    }
}
