namespace Parcial2.Wolves
{
    /// <summary>What a wolf knows about the Hunter; the brain turns it into transitions.</summary>
    public interface IWolfConditions
    {
        bool SeesHunter { get; }
        bool HasLastKnownPosition { get; }
    }
}
