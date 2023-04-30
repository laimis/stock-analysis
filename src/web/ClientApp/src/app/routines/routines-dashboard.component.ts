import { Component, OnInit } from '@angular/core';
import { Routine, RoutineStep, StocksService } from '../services/stocks.service';
import { GetErrors, toggleVisuallyHidden } from '../services/utils';

@Component({
  selector: 'app-routine',
  templateUrl: './routines-dashboard.component.html',
  styleUrls: ['./routines-dashboard.component.css']
})

export class RoutineDashboardComponent implements OnInit {
  

  routines:Routine[] = []

  activeRoutine = null
  activeStep = null
  currentStepIndex = 0

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
    this.updateActiveStep(0);
  }

  private updateActiveStep(increment) {
    if (increment !== 0) {
      this.currentStepIndex += increment
    } else {
      this.currentStepIndex = 0
    }

    this.activeStep = this.activeRoutine.steps[this.currentStepIndex];
  }

  deactivate() {
    this.activeRoutine = null
    this.activeStep = null
    this.currentStepIndex = 0
  }

  nextStep() {
    this.updateActiveStep(1)
  }

  prevStep() {
    this.updateActiveStep(-1)
  }

  reset() {
    this.updateActiveStep(0)
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

  moveUp(routine:Routine, stepIndex:number) {
    var direction = -1;
    this.moveStep(routine, stepIndex, direction);
  }

  moveDown(routine:Routine, stepIndex:number) {
    var direction = 1;
    this.moveStep(routine, stepIndex, direction);
  }

  private moveStep(routine: Routine, stepIndex: number, direction: number) {
    this.service.moveRoutineStep(routine.name, stepIndex, direction).subscribe(
      _ => {
        this.fetchRoutines();
      },
      error => {
        this.errors = GetErrors(error);
      }
    );
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
