using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.EnvironmentManager = this;
    }

    [SerializeField] private GameObject entrySpotLight;
    [SerializeField]private Transform entryEntityPlayerAnchor;
    private EntityPlayer entryEntityPlayer;
    [Space]
    [SerializeField] private Transform MainLight;
    [SerializeField] private Material skyboxMaterial;
    public void EntrySetSpotLightActive(bool active)
    {
        entrySpotLight.SetActive(active);
    }
    public void EntryShowCharacter(int index)
    {
        EntryHideCharacter();
        var obj=Instantiate(Tool.AssetsManager.ZombieCharacters[index].gameObject, entryEntityPlayerAnchor.position, entryEntityPlayerAnchor.rotation);
        entryEntityPlayer = obj.GetComponent<EntityPlayer>();
        if(entryEntityPlayer==null)entryEntityPlayer=obj.AddComponent<EntityPlayer>();
        entryEntityPlayer.RefreshAnimation(new EntityType(EntityCategory.Character,index), 0, false);
    }
    public void EntryHideCharacter()
    {
        if (entryEntityPlayer) Destroy(entryEntityPlayer.gameObject);
    }

    public void SetHour(float hour)
    {
        hour = hour % 24;
        MainLight.rotation=Quaternion.Euler(-90+hour*15,-90,0);
        skyboxMaterial.SetFloat("_Hour", hour);
    }
}
