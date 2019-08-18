import { Component, OnInit } from '@angular/core';
import { StocksService, JobResponse } from '../services/stocks.service';
import {Location} from '@angular/common';

@Component({
  selector: 'app-job-list',
  templateUrl: './job-list.component.html',
  styleUrls: ['./job-list.component.css']
})
export class JobListComponent implements OnInit {

	public loaded: boolean = false;
	public loading: boolean = false;
	public jobs: object;
	public job: JobResponse

	constructor(
		private service : StocksService,
		private location : Location){}

	ngOnInit() {
		this.refresh()
	}

	start() {
		console.log("starting analysis")
		this.service.startAnalysis(5);
	}

	refresh(){
		this.loading = true;
		this.service.getJobs().subscribe(result => {
			this.jobs = result.jobs;
			this.loaded = true;
			this.loading = false;
		}, error => {
			console.error(error);
			this.loaded = true;
			this.loading = false;
		});
	}

	details(jobId:string){
		this.service.getJob(jobId).subscribe(result => {
			this.job = result;
		})
	}

	back(){
		this.location.back();
	}

}
