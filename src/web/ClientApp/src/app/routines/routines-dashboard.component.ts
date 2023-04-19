import { Component, OnInit } from '@angular/core';
import { Routine, StocksService } from '../services/stocks.service';
import { GetErrors, toggleVisuallyHidden } from '../services/utils';

@Component({
  selector: 'app-routine',
  templateUrl: './routines-dashboard.component.html',
  styleUrls: ['./routines-dashboard.component.css']
})

export class RoutineDashboardComponent implements OnInit {
  

  routines:Routine[] = []

  activeRoutine = null
  currentStep = 0

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

  activate(routine) {
    this.activeRoutine = routine
    this.currentStep = 0
  }

  deactivate() {
    this.activeRoutine = null
  }

  nextStep() {
    this.currentStep++
  }

  prevStep() {
    this.currentStep--
  }

  reset() {
    this.currentStep = 0
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

  addStep(routine:Routine, label:string, url:string) {
    this.service.addRoutineStep(routine.name, label, url).subscribe(
      _ => {
        this.fetchRoutines();
      },
      error => {
        this.errors = GetErrors(error)
      }
    )
  }

  deleteStep(routine:Routine, stepIndex:number) {
    this.service.deleteRoutineStep(routine.name, stepIndex).subscribe(
      _ => {
        this.fetchRoutines();
      },
      error => {
        this.errors = GetErrors(error)
      }
    )
  }

  updateStep(routine:Routine, stepIndex:number, label:string, url:string) {
    this.service.updateRoutineStep(routine.name, stepIndex, label, url).subscribe(
      _ => {
        this.fetchRoutines();
      },
      error => {
        this.errors = GetErrors(error)
      }
    )
  }
}
