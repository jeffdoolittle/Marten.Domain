using System;

namespace Ferret
{
    public class CommitRequest
    {
        public CommitRequest()
        {
            PageSize = 10;
            Page = 1;
        }

        public long MinimumCommitId { get; set; }
        public Guid? StreamId { get; set; }
        public int PageSize { get; set; }
        public int Page { get; set; }
    }
}
