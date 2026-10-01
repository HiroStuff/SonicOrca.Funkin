namespace SonicOrca.Funkin
{
    public interface IFunkinGameplay
    {
        Meta.Data.ChartFormat Chart { get; }
        GameObjects.UserInterface.Notes.StrumLine PlayerStrums { get; }
        GameObjects.UserInterface.Notes.StrumLine CpuStrums { get; }
    }

    public static class FunkinGameplay
    {
        public static IFunkinGameplay Current { get; set; }
    }
}