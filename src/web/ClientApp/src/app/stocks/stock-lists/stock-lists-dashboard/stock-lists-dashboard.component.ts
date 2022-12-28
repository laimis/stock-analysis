import { Component, OnInit } from '@angular/core';
import { StockList, StockListTicker, StocksService } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-stock-lists-dashboard',
  templateUrl: './stock-lists-dashboard.component.html',
  styleUrls: ['./stock-lists-dashboard.component.css']
})
export class StockListsDashboardComponent implements OnInit {
  constructor(
    private stockService:StocksService
  ){}

  newName:string
  newDescription:string

  lists:StockList[]

  ngOnInit(): void {
    this.loadLists();
  }

  private loadLists() {
    this.stockService.getStockLists().subscribe(
      s => {
        this.lists = s;
      },
      e => {
        console.error(e);
      }
    );
  }

  createList() {
    var obj = {
      name: this.newName,
      description: this.newDescription
    }

    this.stockService.createStockList(obj).subscribe(
      _ => {
        this.newName = null
        this.newDescription = null
        this.loadLists()
      },
      e => {
        console.error(e)
      }
    )
  }

  deleteList(list:StockList) {
    this.stockService.deleteStockList(list.name).subscribe(
      _ => {
        this.loadLists()
      },
      e => {
        console.error(e)
      }
    )
  }

}
