import {Component, EventEmitter, HostListener, Input, Output} from '@angular/core';
import {Routine} from "../services/stocks.service";

@Component({
  selector: 'app-routines-active-routine',
  templateUrl: './routines-active-routine.component.html',
  styleUrls: ['./routines-active-routine.component.css']
})
export class RoutinesActiveRoutineComponent {

  activeRoutine:Routine = null
  activeStep = null
  currentStepIndex = 0

  @Input()
  set routine(value:Routine) {
    this.activeRoutine = value

    if (value) {
      this.updateActiveStep(0);
    }
    else {
      this.deactivate();
    }
  }

  @Output()
  routineDeactivated = new EventEmitter()

  deactivate() {
    this.routineDeactivated.emit()
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

  private updateActiveStep(increment:number) {
    let index = this.currentStepIndex;
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

  @HostListener('window:keydown', ['$event'])
  handleKeyboardEvent(event: KeyboardEvent) {

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
