namespace Parcial2.Wolves
{
    public interface IWolfConditions
    {
        bool SeesHunter { get; }
        bool HasLastKnownPosition { get; }
    }
}
