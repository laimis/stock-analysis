using System.Collections.Generic;

namespace analysis
{
	public class JobsStatus
	{
		public JobsStatus(List<KeyValuePair<string, string>> jobs)
		{
			this.Jobs = jobs;
		}

		public List<KeyValuePair<string, string>> Jobs { get; }
	}
}