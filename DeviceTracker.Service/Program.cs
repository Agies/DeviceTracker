using Topshelf;

namespace DeviceTracker.Service
{
    static class Program
    {
        static void Main()
        {
            HostFactory.Run(
                x =>
                {
                    //x.UseLog4Net();
                    x.Service<Tracker>(
                        s =>
                        {
                            s.ConstructUsing(name =>
                                             {
                                                 var t = new Tracker();
                                                 t.Initialize().Wait();
                                                 return t;
                                             });
                            s.WhenStarted(t => t.Start());
                            s.WhenStopped(t => t.Stop());
                            s.WhenShutdown(t => t.Shutdown());
                        });
                    //TODO: Make Run as configurable
                    x.RunAsNetworkService();
                    x.EnableShutdown();
                    x.SetDescription("Device GPS Tracking Service");
                    x.SetDisplayName("Device GPS Tracker");
                    x.SetServiceName("DGPST");

                    x.EnableServiceRecovery(
                        rc =>
                        {
                            rc.RestartService(1);
                        });
                });
        }
    }
}
