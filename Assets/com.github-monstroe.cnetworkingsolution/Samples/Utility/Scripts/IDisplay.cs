using UnityEngine.Events;

public interface IDisplay
{
    void Initialize();
    void Display(bool display, UnityAction callback = null);
    void Reset();
}