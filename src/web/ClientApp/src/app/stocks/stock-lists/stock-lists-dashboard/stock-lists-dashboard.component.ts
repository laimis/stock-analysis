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
    if (confirm(`Are you sure you want to delete ${list.name}?`)) {
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

  getAnalysisLink(list:StockList) {
    var paramList = list.tickers.map(t => t.ticker).join(',')
    return `/reports/outcomes?tickers=${paramList}`
  }

}
