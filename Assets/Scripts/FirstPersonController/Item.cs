public class Item : Interactable
{
    public override void OnFocus()
    {
        print("OnFocus");
    }

    public override void OnInteract()
    {
        print("INTERACT");
    }

    public override void OnLoseFocus()
    {
        print("LOSE FOCUS");
    }
}
