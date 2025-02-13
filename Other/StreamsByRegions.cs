using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace Smart
{
    public static class StreamsByRegions
    {
        static private List<Region> regions = [];
        static private List<DispatcherTimer> timers = [];

        // Загрузить все доступные участки
        public static void LoadRegions()
        {
            regions.Clear();
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
                    resRegion[8].ToString()!,
                    resRegion[9].ToString()!,
                    resRegion[10].ToString()!,
                    resRegion[11].ToString()!);
                if (region.IsDowntime)
                {
                    DispatcherTimer timer = new DispatcherTimer() { Interval = TimeSpan.FromMinutes(region.DowntimeRemaining), Tag = region.getId };
                    timer.Tick += Timer_Tick;
                    timers.Add(timer);
                }
                else
                    region.ActivateRegion();
                regions.Add(region);
            }
        }

        // Возобновление работы участка
        private static void Timer_Tick(object? sender, EventArgs e)
        {
            int NumTimer = timers.IndexOf((sender as DispatcherTimer)!);
            Region region = (Region)regions.Where(reg => reg.getId == (timers[NumTimer].Tag as int?));

            if (region.IsDowntime)
            {
                timers[NumTimer].Interval = TimeSpan.FromMinutes(region.DowntimeRemaining);
                timers[NumTimer].Start();
            }
            else
            {
                ActivateOnId(region.getId!.Value);
                timers[NumTimer].Stop();
                timers.RemoveAt(NumTimer);
            }
        }
        public static void RunAfter(int IdRegion, double Minutes)
        {
            // Проверка, что таймер ещё не установлен
            foreach (var t in timers)
                if (t.Tag as int? == IdRegion)
                    return;

            // Проверка, что данный регион существует в списке
            bool IsRegionExist = false;
            foreach (var region in regions)
                if (region.getId == IdRegion)
                {
                    IsRegionExist = true;
                    break;
                }
            if (!IsRegionExist)
                return;

            // Добавление таймера
            DispatcherTimer timer = new DispatcherTimer() { Interval = TimeSpan.FromMinutes(Minutes), Tag = IdRegion };
            timer.Tick += Timer_Tick;
            timers.Add(timer);
            timers[^1].Start();
            return;
        }

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

        // Для ускорения активации участка
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