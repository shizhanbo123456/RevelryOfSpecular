using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[ExecuteInEditMode]
public class Tool:MonoBehaviour
{
    [SerializeField][Range(0,100)] private int delay = 20;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        Thread.Sleep(delay);
    }

    private static Transform t_calTransform;
    public static Transform GetTemporaryTransform()
    {
        if(t_calTransform == null)
        {
            t_calTransform = new GameObject("calTransform").transform;
            DontDestroyOnLoad(t_calTransform.gameObject);
        }
        return t_calTransform;
    }
    public static void ActiveFor<T>(List<T>list,int count)where T : Component
    {
        while (list.Count < count)
        {
            list.Add(Instantiate(list[0].gameObject, list[0].transform.parent).GetComponent<T>());
        }
        for(int i = 0; i < list.Count; i++)
        {
            list[i].gameObject.SetActive(i<count);
        }
    }
    public static void ActiveFor(List<GameObject> list, int count)
    {
        while (list.Count < count)
        {
            list.Add(Instantiate(list[0], list[0].transform.parent));
        }
        for (int i = 0; i < list.Count; i++)
        {
            list[i].gameObject.SetActive(i < count);
        }
    }
    public static void ExpandListTo<T>(List<T>list,int count,T value = default)
    {
        while(list.Count<count)
        {
            list.Add(value);
        }
    }
}
