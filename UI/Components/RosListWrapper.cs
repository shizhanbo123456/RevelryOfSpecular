using System;
using System.Collections.Generic;
using UnityEngine;

//设置数量、itemRenderer渲染元素、GetItem获取指定子元素
public class RosList:MonoBehaviour
{
    public GameObject template;
    public Action<GameObject, int> itemRenderer;
    private List<GameObject> items = new();
    public int Count { get; private set; }
    public void SetItemCount(int count)
    {
        if (template == null) throw new System.Exception();
        if (items.Count == 0) items.Add(template);
        Tool.ActiveFor(items, count);
        Count = count;
        if (itemRenderer != null)
        {
            for (int i = 0; i < count; i++)
            {
                itemRenderer.Invoke(items[i], i);
            }
        }
    }
    public GameObject GetItem(int index)
    {
        if (index < 0 || index >= Count) throw new System.Exception();
        return items[index];
    }
}
public class RosListWrapper<T>where T:Component
{
    private RosList list;
    public Action<T, int> itemRenderer;
    public List<T> items = new();
    public int Count { get; private set; }
    public RosListWrapper(RosList list)
    {
        this.list = list;
    }
    public void SetItemCount(int count)
    {
        list.SetItemCount(count);
        items.Clear();
        for (int i = 0; i < list.Count; i++)
        {
            var obj = list.GetItem(i);
            items.Add(obj.GetComponent<T>());
        }
        Count = count;
        if (itemRenderer != null)
        {
            for (int i = 0; i < count; i++)
            {
                itemRenderer.Invoke(items[i], i);
            }
        }
    }
    public T GetItem(int index)
    {
        if (index < 0 || index >= Count) throw new System.Exception();
        return items[index];
    }
}