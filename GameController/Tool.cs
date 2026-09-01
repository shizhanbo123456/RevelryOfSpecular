using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// 全局工具/管理器引用（架构说明：通过 static 字段持有 GameController 中每一个 Mono 管理器的引用）。
/// 所有管理器在 Awake 中注册到对应 static 字段。
/// </summary>
[ExecuteInEditMode]
public class Tool : MonoBehaviour
{
    [SerializeField][Range(0,100)] private int delay = 20;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        Thread.Sleep(delay);
        GenericTimer.Update();
    }

    public static Tool Instance { get; private set; }

    #region 管理器引用（由各管理器 Awake 注册）
    public static NetworkManager NetworkManager;
    public static BattleManager BattleManager;
    public static InfoManager InfoManager;
    public static AssetsManager AssetsManager;
    public static InputManager InputManager;
    public static SaveManager SaveManager;
    public static EnvironmentManager EnvironmentManager;
    public static CameraController CameraController;
    public static ClientLogicManager ClientLogicManager;
    public static ClientDisplayManager ClientDisplayManager;
    public static AssetsObjectPool AssetsObjectPool;
    public static VfxManager VfxManager;
    public static TransitionManager TransitionManager;
    #endregion

    #region 通用工具
    private static Transform t_calTransform;
    public static Transform GetTemporaryTransform()
    {
        if (t_calTransform == null)
        {
            t_calTransform = new GameObject("calTransform").transform;
            DontDestroyOnLoad(t_calTransform.gameObject);
        }
        return t_calTransform;
    }
    public static void ActiveFor<T>(List<T> list, int count) where T : Component
    {
        while (list.Count < count)
        {
            list.Add(Instantiate(list[0].gameObject, list[0].transform.parent).GetComponent<T>());
        }
        for (int i = 0; i < list.Count; i++)
        {
            list[i].gameObject.SetActive(i < count);
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
    public static void ExpandListTo<T>(List<T> list, int count, T value = default)
    {
        while (list.Count < count)
        {
            list.Add(value);
        }
    }
    #endregion
}
