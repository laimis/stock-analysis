import {Component, OnInit} from '@angular/core';
import {Routine, StocksService} from '../services/stocks.service';
import {GetErrors, toggleVisuallyHidden} from '../services/utils';

@Component({
    selector: 'app-routines-dashboard',
    templateUrl: './routines-dashboard.component.html',
    styleUrls: ['./routines-dashboard.component.css'],
    standalone: false
})

export class RoutineDashboardComponent implements OnInit {


    routines: Routine[] = null
    activeRoutine: Routine = null
    errors: string[] = null

    constructor(private service: StocksService) {
    }

    ngOnInit() {
        this.fetchRoutines();
    }

    activate(routine: Routine) {
        this.activeRoutine = routine;
    }

    deactivate() {
        this.activeRoutine = null;
    }

    toggleVisibility(element: HTMLElement) {
        toggleVisuallyHidden(element)
    }

    create(name: string, description: string) {
        this.service.createRoutine(name, description).subscribe(
            _ => {
                this.fetchRoutines();
            },
            error => {
                this.errors = GetErrors(error)
            }
        )
    }

    private fetchRoutines() {
        this.service.getRoutines().subscribe(
            data => {
                this.routines = data;
            },
            err => {
                this.errors = GetErrors(err)
            }
        );
    }
}
