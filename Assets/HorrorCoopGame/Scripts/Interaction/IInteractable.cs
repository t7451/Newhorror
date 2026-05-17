namespace HorrorCoopGame.Interaction
{
    public interface IInteractable
    {
        string GetInteractPrompt();
        void Interact(ulong playerNetworkId);
    }
}
