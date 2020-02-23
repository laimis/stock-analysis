import { Component, OnInit } from '@angular/core';
import { StocksService, GetErrors } from '../services/stocks.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-landing',
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.css']
})
export class LandingComponent implements OnInit {

  success: Boolean = false
  errors: string[]

  constructor() { }

  ngOnInit() {
  }
}
