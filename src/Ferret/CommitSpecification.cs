using System;
using System.Linq.Expressions;

namespace Ferret
{
    public class CommitSpecification : Specification<Commit>
    {
        private readonly CommitRequest _request;

        public CommitSpecification(CommitRequest request)
        {
            _request = request;
        }

        public override Expression<Func<Commit, bool>> IsSatisfiedBy()
        {
            var expr = ExpressionBuilder.Apply<Commit>(() => true, null, x => x.Id >= _request.MinimumCommitId);
            expr = ExpressionBuilder.Apply(() => _request.StreamId.HasValue, expr, x => x.StreamId == _request.StreamId.Value);
            return expr;
        }
    }
}
