using System.Collections.Generic;

namespace Ferret
{
    public class CommitResponse
    {
        public CommitResponse(IEnumerable<Commit> commits, int totalCommits)
        {
            Commits = new List<Commit>(commits);
            TotalCommits = totalCommits;
        }

        public IList<Commit> Commits { get; private set; }
        public int TotalCommits { get; private set; }
    }
}
