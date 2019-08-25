using System;

namespace web
{
	public class PurchaseModel
	{
		public string Ticker { get; set; }
		public int Amount { get; set; }
		public double Price { get; set; }
		public DateTime Date { get; set; }
	}
}