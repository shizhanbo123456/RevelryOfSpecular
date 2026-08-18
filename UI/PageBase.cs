using UnityEngine;

public class PageBase:MonoBehaviour
{
    public virtual void Init()
    {

    }
    public virtual void Enter()
    {
        
    }
    public virtual void Exit()
    {

    }
    public void Show()
    {
        gameObject.SetActive(true);
        Enter();
    }
    public void Hide()
    {
        Exit();
        gameObject.SetActive(false);
    }
}