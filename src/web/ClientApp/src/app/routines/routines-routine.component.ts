import {Component, OnInit} from '@angular/core';
import {Routine, StocksService} from '../services/stocks.service';
import {GetErrors, toggleVisuallyHidden} from '../services/utils';
import {ActivatedRoute} from '@angular/router';

@Component({
    selector: 'app-routine',
    templateUrl: './routines-routine.component.html',
    styleUrls: ['./routines-routine.component.css']
})

export class RoutineComponent implements OnInit {


    errors: string[] = null
    routine: Routine;
    activeRoutine: Routine = null;
    mode: string;

    // accept route service where I can extract current routine name from :name parameter
    constructor(
        private route: ActivatedRoute,
        private service: StocksService) {
    }

    ngOnInit() {
        const id = this.route.snapshot.paramMap.get('id');
        this.mode = this.route.snapshot.paramMap.get('mode');
        this.fetchRoutines(id);
    }

    activate(routine: Routine) {
        this.activeRoutine = routine
    }

    deactivate() {
        this.activeRoutine = null
        this.mode = null;
    }

    toggleVisibility(element: HTMLElement) {
        toggleVisuallyHidden(element)
    }

    create(name: string, description: string) {
        this.service.createRoutine(name, description).subscribe(
            result => {
                this.fetchRoutines(result.id);
            },
            error => {
                this.errors = GetErrors(error)
            }
        )
    }

    moveUp(routine: Routine, stepIndex: number) {
        let direction = -1;
        this.moveStep(routine, stepIndex, direction);
    }

    moveDown(routine: Routine, stepIndex: number) {
        let direction = 1;
        this.moveStep(routine, stepIndex, direction);
    }

    addStep(routine: Routine, label: string, url: string) {
        this.service.addRoutineStep(routine.id, label, url).subscribe(
            _ => {
                this.fetchRoutines(routine.id);
            },
            error => {
                this.errors = GetErrors(error)
            }
        )
    }

    deleteStep(routine: Routine, stepIndex: number) {
        this.service.deleteRoutineStep(routine.id, stepIndex).subscribe(
            _ => {
                this.fetchRoutines(routine.id);
            },
            error => {
                this.errors = GetErrors(error)
            }
        )
    }

    updateStep(routine: Routine, stepIndex: number, label: string, url: string) {
        this.service.updateRoutineStep(routine.id, stepIndex, label, url).subscribe(
            _ => {
                this.fetchRoutines(routine.id);
            },
            error => {
                this.errors = GetErrors(error)
            }
        )
    }

    updateRoutine(routine: Routine, name: string) {
        this.service.updateRoutine(routine.id, name).subscribe(
            _ => {
                this.fetchRoutines(routine.id);
            },
            error => {
                this.errors = GetErrors(error)
            }
        )
    }

    private fetchRoutines(id:String) {
        this.service.getRoutines().subscribe(
            data => {
                this.routine = data.filter(routine => routine.id === id)[0];
                if (this.mode === 'activate') {
                    this.activate(this.routine);
                }
            }
        );
    }

    private moveStep(routine: Routine, stepIndex: number, direction: number) {
        this.service.moveRoutineStep(routine.id, stepIndex, direction).subscribe(
            _ => {
                this.fetchRoutines(routine.id);
            },
            error => {
                this.errors = GetErrors(error);
            }
        );
    }

}
