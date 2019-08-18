namespace analysis
{
	public class AnalysisFinished
	{
		internal string JobId { get; }
		internal JobStatus Status { get; }

		public AnalysisFinished(string jobId, JobStatus status)
		{
			this.JobId = jobId;
			this.Status = status;
		}
	}
}