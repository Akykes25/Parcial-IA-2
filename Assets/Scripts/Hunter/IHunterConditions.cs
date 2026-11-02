namespace Parcial2.Hunter
{
    public interface IHunterConditions
    {
        bool IsCaught { get; }
        bool SeesWolf { get; }
        bool HasRequiredGathers { get; }
        bool SeesDeadBoid { get; }
        bool CanAttack { get; }
    }
}
