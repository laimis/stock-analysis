import {Component, OnInit} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {OptionsContainer, StocksService} from '../../services/stocks.service';
import {ActivatedRoute, Route} from "@angular/router";

@Component({
    selector: 'app-options',
    templateUrl: './option-dashboard.component.html',
    styleUrls: ['./option-dashboard.component.css'],
    standalone: false
})
export class OptionsComponent implements OnInit {

    optionsContainer: OptionsContainer

    loaded: boolean = false;

    activeTab: string = 'open'
    errors: string[] = null

    constructor(
        private service: StocksService,
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
        this.service.getOptions().subscribe(result => {
            this.optionsContainer = result
            this.loaded = true
        }, error => {
            this.errors = error
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
