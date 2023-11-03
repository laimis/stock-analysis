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


  errors:string[] = null
  private routineName: string;
  routine: Routine;
  activeRoutine: Routine = null;
  mode: string;

  // accept route service where I can extract current routine name from :name parameter
  constructor(
    private route: ActivatedRoute,
    private service:StocksService) {}

  ngOnInit() {
    this.routineName = this.route.snapshot.paramMap.get('name');
    this.mode = this.route.snapshot.paramMap.get('mode');
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

  activate(routine:Routine) {
    this.activeRoutine = routine
  }

  deactivate() {
    this.activeRoutine = null
    this.mode = null;
  }

  toggleVisibility(element:HTMLElement) {
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
    let direction = -1;
    this.moveStep(routine, stepIndex, direction);
  }

  moveDown(routine:Routine, stepIndex:number) {
    let direction = 1;
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

}
