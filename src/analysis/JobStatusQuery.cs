namespace analysis
{
	public class JobStatusQuery
	{
		public JobStatusQuery(string jobId)
		{
			this.JobId = jobId;
		}

		public string JobId { get; }
	}
}