namespace Smart
{
    public static class StreamsByRegions
    {
        static List<Region> regions = new List<Region>();

        // Загрузить все доступные участки
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
                    resRegion[8].ToString()!,
                    resRegion[9].ToString()!,
                    resRegion[10].ToString()!,
                    resRegion[11].ToString()!);
                region.ActivateRegion();
                regions.Add(region);
            }

            /*timer.Tick += Timer_Tick;
            timer.Start();*/
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