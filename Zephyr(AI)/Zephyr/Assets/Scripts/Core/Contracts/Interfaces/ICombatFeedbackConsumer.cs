namespace Zephyr.Contracts.Interfaces
{
    public interface ICombatFeedbackConsumer
    {
        void Configure(ICombatFeedbackGateway combatFeedback);
    }
}
