using UnityEngine;

public abstract class Interactable : MonoBehaviour
{

    private void Awake()
    {
        // Bu GameObject'in katmanýný "Player" katmanýna ayarla
        this.gameObject.layer = 7;
    }



    public abstract void OnInteract();

    public abstract void OnFocus();

    public abstract void OnLoseFocus();
}
