public enum EndureType
{
    None=0,
    Common=1,
    Super=2
}
public static class EndureExt
{
    public static EndureType GetEndure(this EntityAnim.AnimState state) => state switch
    {
        EntityAnim.AnimState.Spawn => EndureType.Super,
        EntityAnim.AnimState.Motion => EndureType.None,
        EntityAnim.AnimState.Attack => EndureType.Common,
        EntityAnim.AnimState.Hit => EndureType.None,
        EntityAnim.AnimState.Die => EndureType.Super,
        _=>EndureType.None,
    };
}