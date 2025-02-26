import {Component, Input} from '@angular/core';
import {BrokerageOptionOrder, OptionPosition} from "../../services/option.service";
import {CurrencyPipe, PercentPipe} from "@angular/common";
import {ReactiveFormsModule} from "@angular/forms";
import {OptionPositionComponent} from "../option-position/option-position.component";

@Component({
    selector: 'app-option-closed',
    templateUrl: './option-closed.component.html',
    styleUrls: ['./option-closed.component.css'],
    imports: [
        CurrencyPipe,
        PercentPipe,
        ReactiveFormsModule,
        OptionPositionComponent
    ]
})

export class OptionClosedComponent {
    private _closedOptions: OptionPosition[];
    currentPosition: OptionPosition;
    currentPositionIndex: number;
    positionOrders: BrokerageOptionOrder[];

    @Input()
    set closedOptions(value: OptionPosition[]) {
        this._closedOptions = value;
        if (this._closedOptions) {
            this.setCurrentPosition(0);
        }
    }
    get closedOptions(): OptionPosition[] {
        return this._closedOptions;
    }
    
    @Input()
    orders : BrokerageOptionOrder[];
    
    setCurrentPosition(index: number) {
        this.currentPosition = this._closedOptions[index];
        this.positionOrders = this.orders.filter((order) => order.contracts[0].underlyingTicker == this.currentPosition.underlyingTicker);
        this.currentPositionIndex = index;
    }
    
    previous() {
        if (this.currentPositionIndex > 0) {
            this.setCurrentPosition(this.currentPositionIndex - 1);
        } else {
            this.setCurrentPosition(this._closedOptions.length - 1);
        }
    }
    
    next() {
        if (this.currentPositionIndex < this._closedOptions.length - 1) {
            this.setCurrentPosition(this.currentPositionIndex + 1);
        } else {
            this.setCurrentPosition(0);
        }
    }

    dropdownClick(et: EventTarget) {
        let target = et as HTMLSelectElement;
        let index = target.selectedIndex;
        this.setCurrentPosition(index);
    }
    
    abs(value: number) {
        return Math.abs(value);
    }
}
