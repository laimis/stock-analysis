import { Component, EventEmitter, HostListener, Input, OnChanges, Output, inject } from '@angular/core';
import { BrokerageService, OptionOrderCommand, OptionOrderInstruction, OptionOrderType } from 'src/app/services/brokerage.service';
import { OptionPosition, OptionService } from 'src/app/services/option.service';
import { GetErrors } from 'src/app/services/utils';
import { ErrorDisplayComponent } from "../../../shared/error-display/error-display.component";
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-option-position-add-modal',
  imports: [ErrorDisplayComponent, CurrencyPipe, FormsModule],
  templateUrl: './option-position-add-modal.component.html',
  styleUrl: './option-position-add-modal.component.css'
})
export class OptionPositionAddModalComponent implements OnChanges {
  private optionService = inject(OptionService);
  private brokerageService = inject(BrokerageService);

  @Input() position: OptionPosition;
  @Input() isVisible: boolean = false;
  @Output() isVisibleChange = new EventEmitter<boolean>();
  @Output() positionOrderCreated = new EventEmitter();
  @Output() errorsOccurred = new EventEmitter<string[]>();

  positionNotes: string = '';
  price: number;
  quantity: number = 1;
  errors: string[] = [];
  spreadValues: { [key: string]: number } = {};

  @HostListener('document:keydown.escape')
  onEscape() {
      this.close();
  }

  ngOnChanges() {
    if (this.position) {
        console.log("setting price value", this.position.market)
        this.price = this.position.market;
        this.calculateSpreadValues();
    }
}

calculateSpreadValues() {
    const spread = this.position.spread;
    this.spreadValues = {
        '50%': spread * 0.5,
        '60%': spread * 0.6,
        '70%': spread * 0.7,
        '80%': spread * 0.8
    };
}

  onOverlayClick(event: MouseEvent) {
      if ((event.target as HTMLElement).classList.contains('modal-backdrop')) {
          this.close();
      }
  }

  close() {
      this.isVisible = false;
      this.isVisibleChange.emit(this.isVisible);
  }

  addToPosition() {
      this.createBrokerageOrder();
  }

  addNotes() {
    this.optionService.addNotes(this.position.positionId, this.positionNotes).subscribe({
      next: () => {
        this.positionNotes = '';
        this.close();
        this.positionOrderCreated.emit();
      },
      error: (err) => {
        console.log('Error adding notes', err);
        this.errors = GetErrors(err);
        this.errorsOccurred.emit(this.errors);
      }
    });
  }

  private createBrokerageOrder() {
      const orderType = this.price < 0 ? OptionOrderType.NET_DEBIT : OptionOrderType.NET_CREDIT;
      const session = "NORMAL";
      const duration = "GOOD_TILL_CANCEL";
      const orderStrategyType = "SINGLE";

      const order: OptionOrderCommand = {
          orderType,
          session,
          price: this.price,
          duration,
          orderStrategyType,
          orderLegCollection: this.position.contracts.map(c => ({
              instruction: c.quantity > 0 ?
                    OptionOrderInstruction.BUY_TO_OPEN :
                    OptionOrderInstruction.SELL_TO_OPEN,
                quantity: Math.abs(this.quantity),
                instrument: {
                    symbol: c.details.symbol,
                    assetType: "OPTION"
                }
          }))
      };

      this.brokerageService.issueOptionOrder(order).subscribe({
          next: (_) => {
              alert('Order created successfully, adding notes...');
              this.addNotes()
          },
          error: (err) => {
              console.log('Error creating order', err);
              this.errors = GetErrors(err);
              this.errorsOccurred.emit(this.errors);
          }
      });
  }
}
