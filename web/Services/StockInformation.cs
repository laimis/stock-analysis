namespace web.Services
{
	public class StockInformation
	{
		public string Symbol { get; set; }
		public string Name { get; set; }
		public float Price { get; set; }
		public int PriceBucket
		{
			get
			{
				if (this.Price <= 5) return 5;
				if (this.Price <= 25) return 25;
				if (this.Price <= 50) return 50;
				if (this.Price <= 100) return 100;
				if (this.Price <= 200) return 200;
				if (this.Price <= 500) return 500;
				if (this.Price <= 1000) return 1000;
				if (this.Price <= 3000) return 3000;
				if (this.Price <= 5000) return 5000;

				return 10000;
			}
		}
	}
}