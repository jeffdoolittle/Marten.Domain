using Nancy;
using Nancy.ModelBinding;

namespace Ferret.Sample.Modules
{
    public class CommitsModule : NancyModule
    {
        public CommitsModule(IAggregateRepository repository)
            : base("/commits")
        {
            Get["/"] = _ => 
            {
                var request = this.Bind<CommitRequest>();
                var response = repository.Advanced.FetchCommits(request);
                return Response.AsJson(response);
            };
        }
    }
}
