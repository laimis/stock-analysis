import { Component, HostListener, OnInit } from '@angular/core';
import { Routine, StocksService } from '../services/stocks.service';
import { GetErrors, toggleVisuallyHidden } from '../services/utils';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-routine',
  templateUrl: './routines-routine.component.html',
  styleUrls: ['./routines-routine.component.css']
})

export class RoutineComponent implements OnInit {
  

  activeRoutine = null
  activeStep = null
  currentStepIndex = 0

  errors:string[] = null
  private routineName: string;
  routine: Routine;
  mode: string;

  // accept route service where I can extract current routine name from :name parameter
  constructor(
    private route: ActivatedRoute,
    private service:StocksService) {
      this.routineName = this.route.snapshot.paramMap.get('name');
      this.mode = this.route.snapshot.paramMap.get('mode');
    }

  ngOnInit() {
    this.fetchRoutines();
  }

  private fetchRoutines() {
    this.service.getRoutines().subscribe(
      data => {
        this.routine = data.filter(routine => routine.name === this.routineName)[0];
        if (this.mode === 'activate') {
          this.activate(this.routine);
        }
      }
    );
  }

  activate(routine) {
    this.activeRoutine = routine
    this.updateActiveStep(0);
  }

  private updateActiveStep(increment) {
    var index = this.currentStepIndex
    if (increment !== 0) {
      index += increment
    } else {
      index = 0
    }

    if (index < 0) {
      return
    } else if (index >= this.activeRoutine.steps.length) {
      return
    }

    this.currentStepIndex = index
    this.activeStep = this.activeRoutine.steps[this.currentStepIndex];
  }

  deactivate() {
    this.activeRoutine = null
    this.activeStep = null
    this.currentStepIndex = 0
    this.mode = null;
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

  updateRoutine(routine:Routine, newName:string) {
    this.service.updateRoutine(routine.name, newName).subscribe(
      _ => {
        this.fetchRoutines();
      },
      error => {
        this.errors = GetErrors(error)
      }
    )
  }

  @HostListener('window:keydown', ['$event'])
  handleKeyboardEvent(event: KeyboardEvent) { 
    console.log(event.key)
    if (this.activeRoutine === null) {
      return
    }
    
    if (event.key === "ArrowRight") {
      this.nextStep();
      event.preventDefault();
    } else if (event.key === "ArrowLeft") {
      this.prevStep();
      event.preventDefault();
    } else if (event.key === "Escape") {
      this.deactivate();
      event.preventDefault();
    } else if (event.key === "Enter") {
      // open the url in the active step in a new tab
      window.open(this.activeStep.url, "_blank");
      event.preventDefault();
    }
  }
}
