import { Component, HostListener, OnInit } from '@angular/core';
import { Routine, RoutineStep, StocksService } from '../services/stocks.service';
import { GetErrors, toggleVisuallyHidden } from '../services/utils';

@Component({
  selector: 'app-routines-dashboard',
  templateUrl: './routines-dashboard.component.html',
  styleUrls: ['./routines-dashboard.component.css']
})

export class RoutineDashboardComponent implements OnInit {
  

  routines:Routine[] = []

  errors:string[] = null

  constructor(private service:StocksService) { }

  ngOnInit() {
    this.fetchRoutines();
  }

  private fetchRoutines() {
    this.service.getRoutines().subscribe(
      data => {
        this.routines = data;
      }
    );
  }

  
  toggleVisibility(element) {
    toggleVisuallyHidden(element)
  }

  create(name:string, description:string) {
    this.service.createRoutine(name, description).subscribe(
      _ => {
        this.fetchRoutines();
      },
      error => {
        this.errors = GetErrors(error)
      }
    )
  }
}
