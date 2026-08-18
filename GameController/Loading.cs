using System;
using System.Threading.Tasks;
using UnityEngine;

public class Loading : MonoBehaviour
{
    private int count;
    private int Count
    {
        get
        {
            return count;
        }
        set
        {
            gameObject.SetActive(value>0);
            count= value;
        }
    }

    [SerializeField] private Transform loadingIcon;
    [SerializeField] private float loadingIconRotateSpeed = 180f;
    private float loadingIconAngle;
    public void Init()
    {
        gameObject.SetActive(false);
        loadingIconAngle = 0f;
        if (loadingIcon != null)
        {
            loadingIcon.localRotation = Quaternion.identity;
        }
    }
    private void Update()
    {
        if (loadingIcon == null) return;

        loadingIconAngle = Mathf.Repeat(loadingIconAngle - loadingIconRotateSpeed * Time.unscaledDeltaTime, 360f);
        loadingIcon.localRotation = Quaternion.Euler(0, 0, -loadingIconAngle);
    }
    public async Task Execute(Func<Task> task,Action result=null)
    {
        Count++;
        try
        {
            var a = task?.Invoke();
            await a;
            result?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            Count--;
        }
    }
}
