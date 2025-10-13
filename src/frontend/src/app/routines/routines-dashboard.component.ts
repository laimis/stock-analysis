import { Component, OnInit, inject } from '@angular/core';
import {Routine, StocksService} from '../services/stocks.service';
import {GetErrors, toggleVisuallyHidden} from '../services/utils';
import { ErrorDisplayComponent } from "../shared/error-display/error-display.component";
import { LoadingComponent } from "../shared/loading/loading.component";
import { RoutinesActiveRoutineComponent } from "./routines-active-routine.component";
import { RouterLink } from '@angular/router';

@Component({
    selector: 'app-routines-dashboard',
    templateUrl: './routines-dashboard.component.html',
    styleUrls: ['./routines-dashboard.component.css'],
    imports: [ErrorDisplayComponent, LoadingComponent, RoutinesActiveRoutineComponent, RouterLink],
    standalone: true
})

export class RoutineDashboardComponent implements OnInit {
    private service = inject(StocksService);



    routines: Routine[] | null = null
    activeRoutine: Routine | null = null
    errors: string[] | null = null


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
