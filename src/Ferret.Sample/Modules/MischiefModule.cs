using Ferret.Sample.Domain;
using Nancy;
using Nancy.ModelBinding;

namespace Ferret.Sample.Modules
{
    public class MischiefModule : NancyModule
    {
        public MischiefModule(Mischief.Manager manager)
            : base("/mischief")
        {
            Get["/{mischiefId}"] = _ =>
            {
                return "Mischief status";
            };

            Post["/map-discovery"] = _ =>
            {
                var command = this.Bind<OpenMaraudersMap>();
                manager.When(command);
                return HttpStatusCode.OK;
            };

            Post["/room-of-requirement-visit"] = _ =>
            {
                var command = this.Bind<GotoRoomOfRequirement>();
                manager.When(command);
                return HttpStatusCode.OK;
            };
        }
    }
}
