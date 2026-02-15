public interface IInteractable
{
    void Interact();
    string GetInteractionPrompt(); // e.g. "Press E to Talk" or "Press E to Open"
}