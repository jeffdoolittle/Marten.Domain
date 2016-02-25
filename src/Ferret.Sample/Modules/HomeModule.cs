using Nancy;

namespace Ferret.Sample.Modules
{
    public class HomeModule : NancyModule
    {
        public HomeModule()
        {
            Get["/"] = _ =>
            {
                return "Hello Ferret!";
            };
        }
    }
}
