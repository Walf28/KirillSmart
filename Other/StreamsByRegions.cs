using System.Windows.Threading;

namespace Smart
{
    public static class StreamsByRegions
    {
        static List<Region> regions = new List<Region>();
        /*static List<Route> routes = new List<Route>();

        public static void LoadRoutes()
        {
            var result = DB.SelectAll("Routes")!;
            foreach (var resRoute in result)
            {
                if (resRoute[2].ToString()!.Split(';').Length.ToString() == resRoute[7].ToString()!)
                    continue;
                Route route = new(
                    resRoute[0].ToString()!,
                    resRoute[1].ToString()!,
                    resRoute[2].ToString()!,
                    resRoute[3].ToString()!,
                    resRoute[4].ToString()!,
                    resRoute[5].ToString()!,
                    resRoute[6].ToString()!,
                    resRoute[7].ToString()!);
                route.ActivateRoute();
                routes.Add(route);
            }
        }

        public static void AddRoute(Route route)
        {
            route.ActivateRoute();
            routes.Add(route);
        }*/

        //static DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(10) };

        public static void LoadRegions()
        {
            var result = DB.SelectAll("Region")!;
            foreach (var resRegion in result)
            {
                Region region = new(
                    resRegion[0].ToString()!,
                    resRegion[1].ToString()!,
                    resRegion[2].ToString()!,
                    resRegion[3].ToString()!,
                    resRegion[4].ToString()!,
                    resRegion[5].ToString()!,
                    resRegion[6].ToString()!,
                    resRegion[7].ToString()!,
                    resRegion[8].ToString()!);
                region.ActivateRegion();
                regions.Add(region);
            }

            /*timer.Tick += Timer_Tick;
            timer.Start();*/
        }

        /*private static void Timer_Tick(object? sender, EventArgs e)
        {
            foreach (var region in regions)
                if (region.GetSummWorkload > 0 && !region.IsOn)
                    region.ActivateRegion();
        }*/

        // Добавить участок. А если он уже добавлен, то активировать
        public static void AddRegion(Region region)
        {
            foreach (var reg in regions)
                if (reg.getId == region.getId)
                {
                    reg.ActivateRegion();
                    break;
                }
            region.ActivateRegion();
            regions.Add(region);
        }

        // Для ускорения
        public static void ActivateOnId(int id)
        {
            foreach (var region in regions)
                if (region.getId == id)
                {
                    region.ActivateRegion();
                    return;
                }
        }
    }
}