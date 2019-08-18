namespace analysis
{
	public class JobStatus
	{
		public int Analyzed;

		public int ToAnalyze { get; }

		public string[] Candidates;

		public AnalyzeStocks Request { get; }

		public JobStatus(int analyzed, int toAnalyze, string[] candidates, AnalyzeStocks request)
		{
			this.Analyzed = analyzed;
			this.ToAnalyze = toAnalyze;
			this.Candidates = candidates;
			this.Request = request;
		}
	}
}