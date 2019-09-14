import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';
import {Location} from '@angular/common';

@Component({
  selector: 'app-job-list',
  templateUrl: './job-list.component.html',
  styleUrls: ['./job-list.component.css']
})
export class JobListComponent implements OnInit {

	public loaded: boolean = false;
	public loading: boolean = false;
	public analysis: object[];

	public minPrice : Number
	public maxPrice : Number

	sortByProperty:string = 'ticker';
	sortDirection:string = 'asc';

	constructor(
		private service : StocksService,
		private location : Location){}

	ngOnInit() {
		this.refresh()
	}

	start() {
		this.service.startAnalysis(this.minPrice, this.maxPrice);
	}

	sortBy(property:string){

		var changeInProperty = false;
		if (this.sortByProperty != property)
		{
			changeInProperty = true;
		}

		this.sortByProperty = property;
		if (!changeInProperty)
		{
			if (this.sortDirection == 'asc')
			{
				this.sortDirection = 'desc'
			}
			else
			{
				this.sortDirection = 'asc'
			}
		}

		this.refresh();
	}

	refresh(){
		this.loading = true;
		this.service.getAnalysis(this.sortByProperty, this.sortDirection).subscribe(result => {
			this.analysis = result;
			this.loaded = true;
			this.loading = false;
		}, error => {
			console.error(error);
			this.loaded = true;
			this.loading = false;
		});
	}

}
