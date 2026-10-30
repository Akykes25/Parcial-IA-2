namespace Parcial2.Hunter
{
    /// <summary>What the Hunter knows about itself and the world; the brain turns it into transitions.</summary>
    public interface IHunterConditions
    {
        bool IsCaught { get; }
        bool SeesWolf { get; }
        bool HasRequiredGathers { get; }
        bool SeesDeadBoid { get; }
        bool CanAttack { get; }
    }
}
